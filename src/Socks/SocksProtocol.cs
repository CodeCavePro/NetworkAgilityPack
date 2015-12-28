using System;
using System.Net;
using System.Net.Sockets;

namespace CodeCave.NetworkAgilityPack.Socks
{
	/// <summary>
	/// Implements a specific version of the SOCKS protocol. This is an abstract class; it must be inherited.
	/// </summary>
	internal abstract class SocksProtocol : IDisposable
    {
        #region Variables

        /// <summary>Holds the value of the Server property.</summary>
        protected Socket server;

        /// <summary>Holds the address of the method to call when the SOCKS protocol has been completed.</summary>
        protected HandShakeComplete handShakeComplete;

        #endregion Variables

        #region Constructors

        /// <summary>
		/// Initializes a new instance of the SocksHandler class.
		/// </summary>
		/// <param name="server">The socket connection with the proxy server.</param>
		/// <param name="user">The username to use when authenticating with the server.</param>
		/// <exception cref="ArgumentNullException"><c>server</c> -or- <c>user</c> is null.</exception>
		protected SocksProtocol(Socket server, string user)
        {
			Server = server;
			Username = user;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SocksProtocol"/> class.
        /// </summary>
        ~SocksProtocol()
	    {
            Dispose(false);
	    }

	    #endregion Constructors

        #region Abstract members

        /// <summary>
        /// Starts negotiating with a SOCKS proxy server.
        /// </summary>
        /// <param name="host">The remote server to connect to.</param>
        /// <param name="port">The remote port to connect to.</param>
        public abstract void Negotiate(string host, int port);

        /// <summary>
        /// Starts negotiating with a SOCKS proxy server.
        /// </summary>
        /// <param name="remoteEndPoint">The remote endpoint to connect to.</param>
        public abstract void Negotiate(IPEndPoint remoteEndPoint);

        /// <summary>
        /// Starts negotiating asynchronously with a SOCKS proxy server.
        /// </summary>
        /// <param name="remoteEndPoint">An IPEndPoint that represents the remote device. </param>
        /// <param name="callback">The method to call when the connection has been established.</param>
        /// <param name="proxyEndPoint">The IPEndPoint of the SOCKS proxy server.</param>
        /// <returns>An IAsyncProxyResult that references the asynchronous connection.</returns>
        public abstract AsyncSocksResult BeginNegotiate(IPEndPoint remoteEndPoint, HandShakeComplete callback, IPEndPoint proxyEndPoint);

        /// <summary>
        /// Starts negotiating asynchronously with a SOCKS proxy server.
        /// </summary>
        /// <param name="host">The remote server to connect to.</param>
        /// <param name="port">The remote port to connect to.</param>
        /// <param name="callback">The method to call when the connection has been established.</param>
        /// <param name="proxyEndPoint">The IPEndPoint of the SOCKS proxy server.</param>
        /// <returns>An IAsyncProxyResult that references the asynchronous connection.</returns>
        public abstract AsyncSocksResult BeginNegotiate(string host, int port, HandShakeComplete callback, IPEndPoint proxyEndPoint);

        #endregion Abstract members

        #region Methods

        /// <summary>
		/// Converts a port number to an array of bytes.
		/// </summary>
		/// <param name="port">The port to convert.</param>
		/// <returns>An array of two bytes that represents the specified port.</returns>
		protected byte[] PortToBytes(int port)
        {
			byte [] ret = new byte[2];
			ret[0] = (byte)(port / 256);
			ret[1] = (byte)(port % 256);
			return ret;
		}

		/// <summary>
		/// Converts an IP address to an array of bytes.
		/// </summary>
		/// <param name="address">The IP address to convert.</param>
		/// <returns>An array of four bytes that represents the specified IP address.</returns>
		protected byte[] AddressToBytes(long address)
        {
			byte [] ret = new byte[4];
			ret[0] = (byte)(address % 256);
			ret[1] = (byte)((address / 256) % 256);
			ret[2] = (byte)((address / 65536) % 256);
			ret[3] = (byte)(address / 16777216);
			return ret;
		}

		/// <summary>
		/// Reads a specified number of bytes from the Server socket.
		/// </summary>
		/// <param name="count">The number of bytes to return.</param>
		/// <returns>An array of bytes.</returns>
		/// <exception cref="ArgumentException">The number of bytes to read is invalid.</exception>
		/// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
		/// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        protected byte[] ReadBytes(int count)
        {
            if (count <= 0) throw new ArgumentException(); 

            const int MAX_TIMES_0_RECEIVED_BYTES = 32;

            var buffer = new byte[count];
            var received = 0;
            var counter = MAX_TIMES_0_RECEIVED_BYTES;
            while (received != count && counter != 0) 
            {
                counter = (received == 0) ? counter - 1 : MAX_TIMES_0_RECEIVED_BYTES; // TODO add a real timeout
                received += Server.Receive(buffer, received, count - received, SocketFlags.None);
            }

            return buffer;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        public void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            server?.Disconnect(false);
            server?.Close();
            server?.Dispose();
        }

        #endregion Methods

        #region Properties

        /// <summary>
		/// Gets or sets the socket connection with the proxy server.
		/// </summary>
		/// <value>A Socket object that represents the connection with the proxy server.</value>
		/// <exception cref="ArgumentNullException">The specified value is null.</exception>
		protected Socket Server
        {
			get { return server; }
			set
            {
                if (value == null) throw new ArgumentNullException();
                server = value;
			}
		}

        /// <summary>
        /// Gets the username.
        /// </summary>
        /// <value>
        /// The username.
        /// </value>
        public string Username { get; private set; }

        /// <summary>
        /// Gets or sets a byte buffer.
        /// </summary>
        /// <value>An array of bytes.</value>
        protected byte[] Buffer { get; set; }

	    /// <summary>
	    /// Gets or sets the number of bytes that have been received from the remote proxy server.
	    /// </summary>
	    /// <value>An integer that holds the number of bytes that have been received from the remote proxy server.</value>
	    protected int Received { get; set; }

        /// <summary>
        /// Gets or sets the return value of the BeginConnect call.
        /// </summary>
        /// <value>An IAsyncProxyResult object that is the return value of the BeginConnect call.</value>
        protected AsyncSocksResult AsyncResult { get; set; }

        #endregion Properties
    }

    /// <summary>
    /// References the callback method to be called when the protocol negotiation is completed.
    /// </summary>
    public delegate void HandShakeComplete(Exception error);
}