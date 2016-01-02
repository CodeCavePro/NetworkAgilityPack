using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CodeCave.NetworkAgilityPack.Auth;
using CodeCave.NetworkAgilityPack.Socks.Auth;

namespace CodeCave.NetworkAgilityPack.Socks
{
    internal sealed class Socks5Protocol : SocksProtocol
    {
        #region Constructors

        /// <summary>
        /// Initializes a new Socks5Handler instance.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        /// <exception cref="ArgumentNullException"><c>server</c>  is null.</exception>
        public Socks5Protocol(Socket server) : this(server, "")
        {
        }

        /// <summary>
        /// Initializes a new Socks5Handler instance.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        /// <param name="user">The username to use.</param>
        /// <exception cref="ArgumentNullException"><c>server</c> -or- <c>user</c> is null.</exception>
        public Socks5Protocol(Socket server, string user) : this(server, user, "")
        {
        }

        /// <summary>
        /// Initializes a new Socks5Handler instance.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        /// <param name="user">The username to use.</param>
        /// <param name="pass">The password to use.</param>
        /// <exception cref="ArgumentNullException"><c>server</c> -or- <c>user</c> -or- <c>pass</c> is null.</exception>
        public Socks5Protocol(Socket server, string user, string pass) : base(server, user)
        {
            Password = pass;
        }

        #endregion Constructors

        #region Methods

        /// <summary>
        /// Starts the synchronous authentication process.
        /// </summary>
        /// <exception cref="SocksProxyException">Authentication with the proxy server failed.</exception>
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        private void Authenticate()
        {
            Server.Send(new byte[] {5, 2, 0, 2});
            var buffer = ReadBytes(2);
            if (buffer[1] == 255) throw new SocksProxyException("No authentication method accepted.");

            // Detect authentication type
            IAuthProtocol authenticate;
            switch (buffer[1])
            {
                case 0:
                    authenticate = new AuthNoneSocket(Server);
                    break;
                case 2:
                    authenticate = new AuthUserPassSocket(Server, new NetworkCredential(Username, Password));
                    break;
                default:
                    throw new ProtocolViolationException();
            }

            // Attempt to authenticate
            authenticate.Authenticate();
        }

        /// <summary>
        /// Creates an array of bytes that has to be sent when the user wants to connect to a specific host/port combination.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <returns>An array of bytes that has to be sent when the user wants to connect to a specific host/port combination.</returns>
        /// <exception cref="ArgumentNullException"><c>host</c> is null.</exception>
        /// <exception cref="ArgumentException"><c>port</c> or <c>host</c> is invalid.</exception>
        private byte[] GetHostPortBytes(string host, int port)
        {
            if (host == null)
                throw new ArgumentNullException();

            if (port <= 0 || port > 65535 || host.Length > 255)
                throw new ArgumentException();

            var connect = new byte[7 + host.Length];
            connect[0] = 5;
            connect[1] = 1;
            connect[2] = 0; // Reserved byte
            connect[3] = 3;
            connect[4] = (byte) host.Length;
            Array.Copy(Encoding.ASCII.GetBytes(host), 0, connect, 5, host.Length);
            Array.Copy(PortToBytes(port), 0, connect, host.Length + 5, 2);
            return connect;
        }

        /// <summary>
        /// Creates an array of bytes that has to be sent when the user wants to connect to a specific IPEndPoint.
        /// </summary>
        /// <param name="remoteEndPoint">The IPEndPoint to connect to.</param>
        /// <returns>
        /// An array of bytes that has to be sent when the user wants to connect to a specific IPEndPoint.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="ArgumentNullException"><c>remoteEndPoint</c> is null.</exception>
        private byte[] GetEndPointBytes(IPEndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null)
                throw new ArgumentNullException(nameof(remoteEndPoint));

            var connect = new byte[10];
            connect[0] = 5;
            connect[1] = 1;
            connect[2] = 0; //reserved
            connect[3] = 1;

            Array.Copy(AddressToBytes(remoteEndPoint.Address.Address), 0, connect, 4, 4);
            Array.Copy(PortToBytes(remoteEndPoint.Port), 0, connect, 8, 2);
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
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
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
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
        public override void Negotiate(IPEndPoint remoteEndPoint)
        {
            Negotiate(GetEndPointBytes(remoteEndPoint));
        }

        /// <summary>
        /// Starts negotiating asynchronously with the SOCKS server. 
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port to connect to.</param>
        /// <param name="callback">The method to call when the negotiation is complete.</param>
        /// <param name="proxyEndPoint">The IPEndPoint of the SOCKS proxy server.</param>
        /// <returns>An IAsyncProxyResult that references the asynchronous connection.</returns>
        public override AsyncSocksResult BeginNegotiate(string host, int port, HandShakeComplete callback,
            IPEndPoint proxyEndPoint)
        {
            handShakeComplete = callback;
            HandShake = GetHostPortBytes(host, port);
            Server.BeginConnect(proxyEndPoint, OnConnect, Server);
            AsyncResult = new AsyncSocksResult();
            return AsyncResult;
        }

        /// <summary>
        /// Starts negotiating asynchronously with the SOCKS server. 
        /// </summary>
        /// <param name="remoteEndPoint">An IPEndPoint that represents the remote device.</param>
        /// <param name="callback">The method to call when the negotiation is complete.</param>
        /// <param name="proxyEndPoint">The IPEndPoint of the SOCKS proxy server.</param>
        /// <returns>An IAsyncProxyResult that references the asynchronous connection.</returns>
        public override AsyncSocksResult BeginNegotiate(IPEndPoint remoteEndPoint, HandShakeComplete callback,
            IPEndPoint proxyEndPoint)
        {
            handShakeComplete = callback;
            HandShake = GetEndPointBytes(remoteEndPoint);
            Server.BeginConnect(proxyEndPoint, OnConnect, Server);
            AsyncResult = new AsyncSocksResult();
            return AsyncResult;
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
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
        private void Negotiate(byte[] connect)
        {
            Authenticate();
            Server.Send(connect);
            var buffer = ReadBytes(4);
            if (buffer[1] != 0)
            {
                Server.Close();
                throw SocksProxyException.FromSocks5Error(buffer[1]);
            }
            switch (buffer[3])
            {
                case 1:
                    ReadBytes(6); //IPv4 address with port
                    break;
                case 3:
                    buffer = ReadBytes(1);
                    ReadBytes(buffer[0] + 2); //domain name with port
                    break;
                case 4:
                    ReadBytes(18); //IPv6 address with port
                    break;
                default:
                    Server.Close();
                    throw new ProtocolViolationException();
            }
        }

        /// <summary>
        /// Called when the socket is connected to the remote server.
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
                Server.BeginSend(new byte[] {5, 2, 0, 2}, 0, 4, SocketFlags.None, OnAuthSent, Server);
            }
            catch (Exception e)
            {
                handShakeComplete(e);
            }
        }

        /// <summary>
        /// Called when the authentication bytes have been sent.
        /// </summary>
        /// <param name="ar">Stores state information for this asynchronous operation as well as any user-defined data.</param>
        private void OnAuthSent(IAsyncResult ar)
        {
            try
            {
                Server.EndSend(ar);
            }
            catch (Exception e)
            {
                handShakeComplete(e);
                return;
            }

            try
            {
                Buffer = new byte[1024];
                Received = 0;
                Server.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, OnAuthReceive, Server);
            }
            catch (Exception e)
            {
                handShakeComplete(e);
            }
        }

        /// <summary>
        /// Called when an authentication reply has been received.
        /// </summary>
        /// <param name="ar">Stores state information for this asynchronous operation as well as any user-defined data.</param>
        private void OnAuthReceive(IAsyncResult ar)
        {
            try
            {
                Received += Server.EndReceive(ar);
                if (Received <= 0) throw new SocketException();
            }
            catch (Exception e)
            {
                handShakeComplete(e);
                return;
            }
            try
            {
                if (Received < 2)
                {
                    Server.BeginReceive(Buffer, Received, Buffer.Length - Received, SocketFlags.None, OnAuthReceive,
                        Server);
                }
                else
                {
                    IAuthProtocol authenticate;
                    switch (Buffer[1])
                    {
                        case 0:
                            authenticate = new AuthNoneSocket(Server);
                            break;
                        case 2:
                            var credential = new NetworkCredential(Username, Password);
                            authenticate = new AuthUserPassSocket(Server, credential);
                            break;
                        default:
                            handShakeComplete(new SocketException());
                            return;
                    }

                    authenticate.BeginAuthenticate(OnAuthenticated);
                }
            }
            catch (Exception e)
            {
                handShakeComplete(e);
            }
        }

        /// <summary>
        /// Called when the socket has been successfully authenticated with the server.
        /// </summary>
        /// <param name="e">The exception that has occurred while authenticating, or <em>null</em> if no error occurred.</param>
        private void OnAuthenticated(Exception e)
        {
            if (e != null)
            {
                handShakeComplete(e);
                return;
            }

            try
            {
                Server.BeginSend(HandShake, 0, HandShake.Length, SocketFlags.None, OnSent, Server);
            }
            catch (Exception ex)
            {
                handShakeComplete(ex);
            }
        }

        /// <summary>
        /// Called when the connection request has been sent.
        /// </summary>
        /// <param name="ar">Stores state information for this asynchronous operation as well as any user-defined data.</param>
        private void OnSent(IAsyncResult ar)
        {
            try
            {
                Server.EndSend(ar);
            }
            catch (Exception e)
            {
                handShakeComplete(e);
                return;
            }

            try
            {
                Buffer = new byte[5];
                Received = 0;
                Server.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, OnReceive, Server);
            }
            catch (Exception e)
            {
                handShakeComplete(e);
            }
        }

        /// <summary>
        /// Called when a connection reply has been received.
        /// </summary>
        /// <param name="ar">Stores state information for this asynchronous operation as well as any user-defined data.</param>
        private void OnReceive(IAsyncResult ar)
        {
            try
            {
                Received += Server.EndReceive(ar);
            }
            catch (Exception e)
            {
                handShakeComplete(e);
                return;
            }

            try
            {
                // Check if reached the end
                if (Received == Buffer.Length)
                {
                    ProcessReply(Buffer);
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

        /// <summary>
        /// Processes the received reply.
        /// </summary>
        /// <param name="buffer">The received reply</param>
        /// <exception cref="ProtocolViolationException">The received reply is invalid.</exception>
        private void ProcessReply(byte[] buffer)
        {
            // TODO backup host/address values
            switch (buffer[3])
            {
                case 1:
                    Buffer = new byte[5]; //IPv4 address with port - 1 byte
                    break;
                case 3:
                    Buffer = new byte[buffer[4] + 2]; //domain name with port
                    break;
                case 4:
                    Buffer = new byte[17]; //IPv6 address with port - 1 byte
                    break;
                default:
                    throw new ProtocolViolationException();
            }

            Received = 0;
            Server.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, OnReadLast, Server);
        }

        /// <summary>
        /// Called when the last bytes are read from the socket.
        /// </summary>
        /// <param name="ar">Stores state information for this asynchronous operation as well as any user-defined data.</param>
        private void OnReadLast(IAsyncResult ar)
        {
            try
            {
                Received += Server.EndReceive(ar);
            }
            catch (Exception e)
            {
                handShakeComplete(e);
                return;
            }

            try
            {
                // Check if reached the end
                if (Received == Buffer.Length)
                {
                    handShakeComplete(null);
                }
                else
                {
                    Server.BeginReceive(Buffer, Received, Buffer.Length - Received, SocketFlags.None, OnReadLast, Server);
                }
            }
            catch (Exception e)
            {
                handShakeComplete(e);
            }
        }

        #endregion Methods

        #region Properties

        /// <summary>
        /// Gets or sets the password to use when authenticating with the SOCKS5 server.
        /// </summary>
        /// <value>The password to use when authenticating with the SOCKS5 server.</value>
        private string Password { get; }

        /// <summary>
        /// Gets or sets the bytes to use when sending a connect request to the proxy server.
        /// </summary>
        /// <value>The array of bytes to use when sending a connect request to the proxy server.</value>
        private byte[] HandShake { get; set; }

        #endregion Properties
    }
}