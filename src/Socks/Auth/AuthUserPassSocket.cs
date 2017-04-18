using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CodeCave.NetworkAgilityPack.Auth;

namespace CodeCave.NetworkAgilityPack.Socks.Auth
{
	/// <summary>
	/// This class implements the 'username/password authentication' scheme for SOCKS.
	/// </summary>
    internal sealed class AuthUserPassSocket : AuthNoneSocket, IAuthProtocol
	{
        /// <summary>Holds the address of the method to call when the proxy has authenticated the client.</summary>
        private HandShakeComplete _callBack;

	    /// <summary>
	    /// Initializes a new AuthUserPass instance.
	    /// </summary>
	    /// <param name="server">The socket connection with the proxy server.</param>
	    /// <param name="credential"></param>
	    /// <exception cref="ArgumentNullException"><c>credential</c> is null.</exception>
	    public AuthUserPassSocket(Socket server, NetworkCredential credential)
	        : base(server)
	    {
	        this.credential = credential;
	    }

		/// <summary>
		/// Creates an array of bytes that has to be sent if the user wants to authenticate with the username/password authentication scheme.
		/// </summary>
		/// <returns>An array of bytes that has to be sent if the user wants to authenticate with the username/password authentication scheme.</returns>
		private byte[] GetAuthenticationBytes()
        {
			var buffer = new byte[3 + Username.Length + Password.Length];
			buffer[0] = 1;
			buffer[1] = (byte)Username.Length;
			
            Array.Copy(Encoding.ASCII.GetBytes(Username), 0, buffer, 2, Username.Length);
			buffer[Username.Length + 2] = (byte)Password.Length;
			
            Array.Copy(Encoding.ASCII.GetBytes(Password), 0, buffer, Username.Length + 3, Password.Length);
			return buffer;
		}

		/// <summary>
		/// Starts the authentication process.
		/// </summary>
		public override void Authenticate()
        {
			Endpoint.Send(GetAuthenticationBytes());
			var buffer = new byte[2];
			var received = 0;
            while (received != 2)
				received += Endpoint.Receive(buffer, received, 2 - received, SocketFlags.None);

		    if (buffer[1] == 0) return;
            Endpoint.Close();
		    throw new SocksProxyException("Username/password combination rejected.");
        }

        /// <summary>
		/// Starts the asynchronous authentication process.
		/// </summary>
		/// <param name="callback">The method to call when the authentication is complete.</param>
		public void BeginAuthenticate(HandShakeComplete callback) {
			_callBack = callback;
			Endpoint.BeginSend(GetAuthenticationBytes(), 0, 3 + Username.Length + Password.Length, SocketFlags.None, OnSent, Endpoint);
		}

		/// <summary>
		/// Called when the authentication bytes have been sent.
		/// </summary>
		/// <param name="ar">Stores state information for this asynchronous operation as well as any user-defined data.</param>
		private void OnSent(IAsyncResult ar)
        {
			try {
				Endpoint.EndSend(ar);
				Buffer = new byte[2];
				Endpoint.BeginReceive(Buffer, 0, 2, SocketFlags.None, OnReceive, Endpoint);
			} catch (Exception e) {
				_callBack(e);
			}
		}

		/// <summary>
		/// Called when the socket received an authentication reply.
		/// </summary>
		/// <param name="ar">Stores state information for this asynchronous operation as well as any user-defined data.</param>
		private void OnReceive(IAsyncResult ar)
        {
			try {
				Received += Endpoint.EndReceive(ar);
                if (Received == Buffer.Length)
                {
                    if (Buffer[1] == 0)
                        _callBack(null);
                    else
                        throw new SocksProxyException("Username/password combination not accepted.");
                }
                else
                    Endpoint.BeginReceive(Buffer, Received, Buffer.Length - Received, SocketFlags.None, OnReceive, Endpoint);

			} catch (Exception e) {
				_callBack(e);
			}
		}

        /// <summary>
        /// Gets or sets a byte array that can be used to store data.
        /// </summary>
        /// <value>
        /// A byte array to store data.
        /// </value>
        public byte[] Buffer
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the number of bytes that have been received from the remote proxy server.
        /// </summary>
        /// <value>
        /// An integer that holds the number of bytes that have been received from the remote proxy server.
        /// </value>
        public int Received
        {
            get;
            set;
        }
	}
}