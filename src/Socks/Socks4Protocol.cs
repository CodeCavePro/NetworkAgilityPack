using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CodeCave.NetworkAgilityPack.Socks
{
	internal sealed class Socks4Protocol : SocksProtocol
    {
        #region Constructors

		/// <summary>
		/// Initilizes a new instance of the SocksHandler class.
		/// </summary>
		/// <param name="server">The socket connection with the proxy server.</param>
		/// <param name="user">The username to use when authenticating with the server.</param>
		/// <exception cref="ArgumentNullException"><c>server</c> -or- <c>user</c> is null.</exception>
		public Socks4Protocol(Socket server, string user) : base(server, user) {}

        #endregion Constructors

        #region Methods

        /// <summary>
		/// Creates an array of bytes that has to be sent when the user wants to connect to a specific host/port combination.
		/// </summary>
		/// <param name="host">The host to connect to.</param>
		/// <param name="port">The port to connect to.</param>
		/// <returns>An array of bytes that has to be sent when the user wants to connect to a specific host/port combination.</returns>
		/// <remarks>Resolving the host name will be done at server side. Do note that some SOCKS4 servers do not implement this functionality.</remarks>
		/// <exception cref="ArgumentNullException"><c>host</c> is null.</exception>
		/// <exception cref="ArgumentException"><c>port</c> is invalid.</exception>
		private byte[] GetHostPortBytes(string host, int port)
        {
			if (host == null) throw new ArgumentNullException();
            if (port <= 0 || port > 65535) throw new ArgumentException(); 

			var connect = new byte[10 + Username.Length + host.Length];
			connect[0] = 4;
			connect[1] = 1;

			Array.Copy(PortToBytes(port), 0, connect, 2, 2);
			connect[4] = connect[5] = connect[6] = 0;
			connect[7] = 1;

			Array.Copy(Encoding.ASCII.GetBytes(Username), 0, connect, 8, Username.Length);
			connect[8 + Username.Length] = 0;

			Array.Copy(Encoding.ASCII.GetBytes(host), 0, connect, 9 + Username.Length, host.Length);
			connect[9 + Username.Length + host.Length] = 0;

			return connect;
		}

		/// <summary>
		/// Creates an array of bytes that has to be sent when the user wants to connect to a specific IPEndPoint.
		/// </summary>
		/// <param name="remoteEndPoint">The IPEndPoint to connect to.</param>
		/// <returns>An array of bytes that has to be sent when the user wants to connect to a specific IPEndPoint.</returns>
		/// <exception cref="ArgumentNullException"><c>remoteEndPoint</c> is null.</exception>
		private byte[] GetEndPointBytes(IPEndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null) throw new ArgumentNullException();

			var connect = new byte[9 + Username.Length];
			connect[0] = 4;
			connect[1] = 1;

			Array.Copy(PortToBytes(remoteEndPoint.Port), 0, connect, 2, 2);
			Array.Copy(AddressToBytes(remoteEndPoint.Address.Address), 0, connect, 4, 4);
			Array.Copy(Encoding.ASCII.GetBytes(Username), 0, connect, 8, Username.Length);
			connect[8 + Username.Length] = 0;

			return connect;
		}

		/// <summary>
		/// Starts negotiating with the SOCKS server.
		/// </summary>
		/// <param name="host">The host to connect to.</param>
		/// <param name="port">The port to connect to.</param>
		/// <exception cref="ArgumentNullException"><c>host</c> is null.</exception>
		/// <exception cref="ArgumentException"><c>port</c> is invalid.</exception>
		/// <exception cref="SocksProxyException">The proxy rejected the request.</exception>
		/// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
		/// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
		public override void Negotiate(string host, int port)
        {
			Negotiate(GetHostPortBytes(host, port));
		}

		/// <summary>
		/// Starts negotiating with the SOCKS server.
		/// </summary>
		/// <param name="remoteEndPoint">The IPEndPoint to connect to.</param>
		/// <exception cref="ArgumentNullException"><c>remoteEndPoint</c> is null.</exception>
		/// <exception cref="SocksProxyException">The proxy rejected the request.</exception>
		/// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
		/// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
		public override void Negotiate(IPEndPoint remoteEndPoint)
        {
			Negotiate(GetEndPointBytes(remoteEndPoint));
		}

		/// <summary>
		/// Starts negotiating with the SOCKS server.
		/// </summary>
		/// <param name="connect">The bytes to send when trying to authenticate.</param>
		/// <exception cref="ArgumentNullException"><c>connect</c> is null.</exception>
		/// <exception cref="ArgumentException"><c>connect</c> is too small.</exception>
		/// <exception cref="SocksProxyException">The proxy rejected the request.</exception>
		/// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
		/// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
		private void Negotiate(byte [] connect)
        {
            if (connect == null) throw new ArgumentNullException();
            if (connect.Length < 2) throw new ArgumentException();

			Server.Send(connect);

			var buffer = ReadBytes(8);
		    if (buffer[1] == 90)
                return;

            Server.Close();
            throw SocksProxyException.FromSocks4Error(buffer[1]);
        }

		/// <summary>
		/// Starts negotiating asynchronously with a SOCKS proxy server.
		/// </summary>
		/// <param name="host">The remote server to connect to.</param>
		/// <param name="port">The remote port to connect to.</param>
		/// <param name="callback">The method to call when the connection has been established.</param>
		/// <param name="proxyEndPoint">The IPEndPoint of the SOCKS proxy server.</param>
		/// <returns>An IAsyncProxyResult that references the asynchronous connection.</returns>
		public override AsyncSocksResult BeginNegotiate(string host, int port, HandShakeComplete callback, IPEndPoint proxyEndPoint)
        {
            handShakeComplete = callback;
			Buffer = GetHostPortBytes(host, port);
			Server.BeginConnect(proxyEndPoint, OnConnect, Server);
			AsyncResult = new AsyncSocksResult();
			return AsyncResult;
		}

		/// <summary>
		/// Starts negotiating asynchronously with a SOCKS proxy server.
		/// </summary>
		/// <param name="remoteEndPoint">An IPEndPoint that represents the remote device.</param>
		/// <param name="callback">The method to call when the connection has been established.</param>
		/// <param name="proxyEndPoint">The IPEndPoint of the SOCKS proxy server.</param>
		/// <returns>An IAsyncProxyResult that references the asynchronous connection.</returns>
		public override AsyncSocksResult BeginNegotiate(IPEndPoint remoteEndPoint, HandShakeComplete callback, IPEndPoint proxyEndPoint)
        {
            handShakeComplete = callback;
			Buffer = GetEndPointBytes(remoteEndPoint);
			Server.BeginConnect(proxyEndPoint, OnConnect, Server);
			AsyncResult = new AsyncSocksResult();
			return AsyncResult;
		}

		/// <summary>
		/// Called when the Socket is connected to the remote proxy server.
		/// </summary>
		/// <param name="ar">Stores state information for this asynchronous operation as well as any user-defined data.</param>
		private void OnConnect(IAsyncResult ar)
        {
			try
            {
				Server.EndConnect(ar);
			}
            catch (Exception e)
            {
                handShakeComplete(e);
				return;
			}

			try
            {
				Server.BeginSend(Buffer, 0, Buffer.Length, SocketFlags.None, OnSent, Server);
			}
            catch (Exception e)
            {
                handShakeComplete(e);
			}
		}

		/// <summary>
		/// Called when the Socket has sent the handshake data.
		/// </summary>
		/// <param name="ar">Stores state information for this asynchronous operation as well as any user-defined data.</param>
		private void OnSent(IAsyncResult ar) {
			try
            {
				if (Server.EndSend(ar) < Buffer.Length)
                {
                    handShakeComplete(new SocketException());
					return;
				}
			}
            catch (Exception e)
            {
                handShakeComplete(e);
				return;
			}

			try
            {
				Buffer = new byte[8];
				Received = 0;
				Server.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, OnReceive, Server);
			}
            catch (Exception e)
            {
                handShakeComplete(e);
			}
		}

		/// <summary>
		/// Called when the Socket has received a reply from the remote proxy server.
		/// </summary>
		/// <param name="ar">Stores state information for this asynchronous operation as well as any user-defined data.</param>
		private void OnReceive(IAsyncResult ar)
        {
			try
            {
				var received = Server.EndReceive(ar);
				if (received <= 0)
                {
                    handShakeComplete(new SocketException());
					return;
				}

				Received += received;

				if (Received == 8)
                {
					if (Buffer[1] == 90)
                        handShakeComplete(null);
					else
                    {
						Server.Close();
                        handShakeComplete(new SocksProxyException("Negotiation failed."));
					}
				}
                else
                {
					Server.BeginReceive(Buffer, Received, Buffer.Length - Received, SocketFlags.None, OnReceive, Server);
				}
			}
            catch (Exception e)
            {
                handShakeComplete(e);
			}
        }

        #endregion Methods
    }
}


