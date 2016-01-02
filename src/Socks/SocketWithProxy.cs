using System;
using System.Net;
using System.Net.Sockets;
using CodeCave.NetworkAgilityPack.Web;

namespace CodeCave.NetworkAgilityPack.Socks
{
	internal class SocketWithProxy : Socket
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the ProxySocket class.
        /// </summary>
        /// <param name="addressFamily">One of the AddressFamily values.</param>
        /// <param name="socketType">One of the SocketType values.</param>
        /// <param name="protocolType">One of the ProtocolType values.</param>
        /// <exception cref="SocketException">The combination of addressFamily, socketType, and protocolType results in an invalid socket.</exception>
        /// <exception cref="ArgumentNullException"><c>proxyUsername</c> -or- <c>proxyPassword</c> is null.</exception>
        private SocketWithProxy(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
            : base(addressFamily, socketType, protocolType)
        {
            ProxyType = ProxyType.None;
            Exception = new WebException();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SocketWithProxy"/> class.
        /// </summary>
        /// <param name="addressFamily">The address family.</param>
        /// <param name="socketType">Type of the socket.</param>
        /// <param name="protocolType">Type of the protocol.</param>
        /// <param name="proxyType">Type of the proxy.</param>
        /// <param name="proxyCredentials">The authentication.</param>
        internal SocketWithProxy(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType, ProxyType proxyType, WebProxyCredential proxyCredentials) 
            : this(addressFamily, socketType, protocolType)
        {
            ProxyType = proxyType;
            ProxyCredentials = proxyCredentials;
        }

        #endregion

        #region Methods

        #region Sync

        /// <summary>
        /// Establishes a connection to a remote device.
        /// </summary>
        /// <param name="remoteEndPoint">An EndPoint that represents the remote device.</param>
        /// <exception cref="ArgumentNullException">The remoteEP parameter is a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        public new void Connect(EndPoint remoteEndPoint)
        {
            if (remoteEndPoint == null)
                throw new ArgumentNullException(nameof(remoteEndPoint));

            if (ProtocolType != ProtocolType.Tcp || ProxyType == ProxyType.None || ProxyIpEndPoint == null)
                base.Connect(remoteEndPoint);

            else
            {
                base.Connect(ProxyIpEndPoint);
                switch (ProxyType)
                {
                    case ProxyType.Socks4:
                        (new Socks4Protocol(this, ProxyCredentials.UserName)).Negotiate((IPEndPoint)remoteEndPoint);
                        break;
                    case ProxyType.Socks5:
                        (new Socks5Protocol(this, ProxyCredentials.UserName, ProxyCredentials.Password)).Negotiate((IPEndPoint)remoteEndPoint);
                        break;
                    default:
                        throw new InvalidOperationException($"Wrong proxy type {ProxyType}");
                }
            }
        }

        /// <summary>
        /// Establishes a connection to a remote device.
        /// </summary>
        /// <param name="host">The remote host to connect to.</param>
        /// <param name="port">The remote port to connect to.</param>
        /// <exception cref="ArgumentNullException">The host parameter is a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException">The port parameter is invalid.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        /// <remarks>
        /// If you use this method with a SOCKS4 server, it will let the server resolve the hostname. Not all SOCKS4 servers support this 'remote DNS' though.
        /// </remarks>
        new public void Connect(string host, int port)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            if (port <= 0 || port > 65535)
                throw new ArgumentException("Invalid port.");

            if (ProtocolType != ProtocolType.Tcp || ProxyType == ProxyType.None || ProxyIpEndPoint == null)
            {
                base.Connect(new IPEndPoint(host.ResolveHostDns(), port));
            }
            else
            {
                base.Connect(ProxyIpEndPoint);
                switch (ProxyType)
                {
                    case ProxyType.Socks4:
                        (new Socks4Protocol(this, ProxyCredentials.UserName)).Negotiate(host, port);
                        break;
                    case ProxyType.Socks5:
                        (new Socks5Protocol(this, ProxyCredentials.UserName, ProxyCredentials.Password)).Negotiate(host, port);
                        break;
                    default:
                        throw new InvalidOperationException($"Wrong proxy type {ProxyType}");
                }
            }
        }

        #endregion

        #region Async

        /// <summary>
        /// Begins an asynchronous request for a connection to a network device.
        /// </summary>
        /// <param name="remoteEndPoint">An EndPoint that represents the remote device.</param>
        /// <param name="callback">The AsyncCallback delegate.</param>
        /// <param name="state">An object that contains state information for this request.</param>
        /// <returns>
        /// An IAsyncResult that references the asynchronous connection.
        /// </returns>
        /// <exception cref="ArgumentNullException">The remoteEP parameter is a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="SocketException">An operating system error occurs while creating the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        public new IAsyncResult BeginConnect(EndPoint remoteEndPoint, AsyncCallback callback, object state)
        {
            if (remoteEndPoint == null || callback == null)
                throw new ArgumentNullException();

            if (ProtocolType != ProtocolType.Tcp || ProxyType == ProxyType.None || ProxyIpEndPoint == null)
            {
                Exception = null;
                return base.BeginConnect(remoteEndPoint, callback, state);
            }

            CallBack = callback;
            AsyncResult = null;
            switch (ProxyType)
            {
                case ProxyType.Socks4:
                    AsyncResult = (new Socks4Protocol(this, ProxyCredentials.UserName)).BeginNegotiate((IPEndPoint)remoteEndPoint, OnHandShakeComplete, ProxyIpEndPoint);
                    break;
                case ProxyType.Socks5:
                    AsyncResult = (new Socks5Protocol(this, ProxyCredentials.UserName, ProxyCredentials.Password)).BeginNegotiate((IPEndPoint)remoteEndPoint, OnHandShakeComplete, ProxyIpEndPoint);
                    break;
                default:
                    throw new InvalidOperationException($"Wrong proxy type {ProxyType}");
            }

            return AsyncResult;
        }

        /// <summary>
        /// Begins the connect.
        /// </summary>
        /// <param name="addresses">The addresses.</param>
        /// <param name="port">The port.</param>
        /// <param name="callback">The callback.</param>
        /// <param name="state">The state.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public new IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback callback, object state)
        {
            if (addresses == null || addresses.Length == 0 || callback == null)
                throw new ArgumentNullException();

            var remoteEndPoint = new IPEndPoint(addresses[0], port);
            if (ProtocolType != ProtocolType.Tcp || ProxyType == ProxyType.None || ProxyIpEndPoint == null)
            {
                Exception = null;
                return base.BeginConnect(addresses, port, callback, state);
            }

            CallBack = callback;
            AsyncResult = null;

            switch (ProxyType)
            {
                case ProxyType.Socks4:
                    AsyncResult = (new Socks4Protocol(this, ProxyCredentials.UserName)).BeginNegotiate(remoteEndPoint, OnHandShakeComplete, ProxyIpEndPoint);
                    break;
                case ProxyType.Socks5:
                    AsyncResult = (new Socks5Protocol(this, ProxyCredentials.UserName, ProxyCredentials.Password)).BeginNegotiate(remoteEndPoint, OnHandShakeComplete, ProxyIpEndPoint);
                    break;
                default:
                    throw new InvalidOperationException($"Wrong proxy type {ProxyType}");
            }
            return AsyncResult;
        }

        /// <summary>
        /// Begins an asynchronous request for a connection to a network device.
        /// </summary>
        /// <param name="host">The host to connect to.</param>
        /// <param name="port">The port on the remote host to connect to.</param>
        /// <param name="callback">The AsyncCallback delegate.</param>
        /// <param name="state">An object that contains state information for this request.</param>
        /// <returns>An IAsyncResult that references the asynchronous connection.</returns>
        /// <exception cref="ArgumentNullException">The host parameter is a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException">The port parameter is invalid.</exception>
        /// <exception cref="SocketException">An operating system error occurs while creating the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        new public IAsyncResult BeginConnect(string host, int port, AsyncCallback callback, object state)
        {
            if (host == null || callback == null)
                throw new ArgumentNullException();
            if (port <= 0 || port > 65535)
                throw new ArgumentException();

            CallBack = callback;
            AsyncResult = null;
            State = state;

            if (ProtocolType != ProtocolType.Tcp || ProxyType == ProxyType.None || ProxyIpEndPoint == null)
            {
                ProxyPort = port;
                AsyncResult = BeginDns(host, OnHandShakeComplete);
            }
            else
            {
                switch (ProxyType)
                {
                    case ProxyType.Socks4:
                        AsyncResult = (new Socks4Protocol(this, ProxyCredentials.UserName)).BeginNegotiate(host, port, OnHandShakeComplete, ProxyIpEndPoint);
                        break;
                    case ProxyType.Socks5:
                        AsyncResult = (new Socks5Protocol(this, ProxyCredentials.UserName, ProxyCredentials.Password)).BeginNegotiate(host, port, OnHandShakeComplete, ProxyIpEndPoint);
                        break;
                    default:
                        throw new InvalidOperationException($"Wrong proxy type {ProxyType}");
                }
            }

            return AsyncResult;
        }

        /// <summary>
        /// Ends a pending asynchronous connection request.
        /// </summary>
        /// <param name="asyncResult">Stores state information for this asynchronous operation as well as any user-defined data.</param>
        /// <exception cref="ArgumentNullException">The asyncResult parameter is a null reference (Nothing in Visual Basic).</exception>
        /// <exception cref="ArgumentException">The asyncResult parameter was not returned by a call to the BeginConnect method.</exception>
        /// <exception cref="SocketException">An operating system error occurs while accessing the Socket.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        /// <exception cref="InvalidOperationException">EndConnect was previously called for the asynchronous connection.</exception>
        public new void EndConnect(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException();
            if (!asyncResult.IsCompleted)
                throw new ArgumentException();
            if (Exception != null)
                throw Exception;
        }

        /// <summary>
        /// Begins an asynchronous request to resolve a DNS host name or IP address in dotted-quad notation to an IPAddress instance.
        /// </summary>
        /// <param name="host">The host to resolve.</param>
        /// <param name="callback">The method to call when the hostname has been resolved.</param>
        /// <returns>An IAsyncResult instance that references the asynchronous request.</returns>
        /// <exception cref="SocketException">There was an error while trying to resolve the host.</exception>
        internal AsyncSocksResult BeginDns(string host, HandShakeComplete callback)
        {
            try
            {
                Dns.BeginGetHostEntry(host, OnResolved, this);
                return new AsyncSocksResult();
            }
            catch
            {
                throw new SocketException();
            }
        }

        /// <summary>
        /// Called when the specified hostname has been resolved.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        private void OnResolved(IAsyncResult asyncResult)
        {
            try
            {
                var dns = Dns.EndGetHostEntry(asyncResult);
                base.BeginConnect(new IPEndPoint(dns.AddressList[0], ProxyPort), OnConnect, State);
            }
            catch (Exception e)
            {
                OnHandShakeComplete(e);
            }
        }

        /// <summary>
        /// Called when the Socket is connected to the remote host.
        /// </summary>
        /// <param name="asyncResult">The result of the asynchronous operation.</param>
        private void OnConnect(IAsyncResult asyncResult)
        {
            try
            {
                base.EndConnect(asyncResult);
                OnHandShakeComplete(null);
            }
            catch (Exception e)
            {
                OnHandShakeComplete(e);
            }
        }

        /// <summary>
        /// Called when the Socket has finished talking to the proxy server and is ready to relay data.
        /// </summary>
        /// <param name="exception">The error to throw when the EndConnect method is called.</param>
        private void OnHandShakeComplete(Exception exception)
        {
            if (exception != null)
                Close();

            Exception = exception;
            AsyncResult.Reset();
            CallBack?.Invoke(AsyncResult);
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
	    /// Gets or sets the EndPoint of the proxy server.
	    /// </summary>
	    /// <value>
	    /// An IPEndPoint object that holds the IP address and the port of the proxy server.
	    /// </value>
        public IPEndPoint ProxyIpEndPoint => ProxyCredentials?.IPEndPoint;

	    /// <summary>
	    /// Gets or sets the type of proxy server to use.
	    /// </summary>
	    /// <value>One of the ProxyType values.</value>
        public ProxyType ProxyType { get; internal set; }

        /// <summary>
        /// Gets or sets the remote port the user wants to connect to.
        /// </summary>
        /// <value>An integer that specifies the port the user wants to connect to.</value>
        private int ProxyPort { get; set; }

        /// <summary>
        /// Gets the credentials.
        /// </summary>
        /// <value>
        /// The credentials.
        /// </value>
        private WebProxyCredential ProxyCredentials { get; }

        /// <summary>
        /// Gets or sets the asynchronous result.
        /// </summary>
        /// <value>
        /// The asynchronous result.
        /// </value>
        private AsyncSocksResult AsyncResult { get; set; }

	    /// <summary>
        /// Gets or sets the exception to throw when the EndConnect method is called.
        /// </summary>
        /// <value>An instance of the Exception class (or subclasses of Exception).</value>
        private Exception Exception { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>
        /// The state.
        /// </value>
        private object State { get; set; }

        /// <summary>
        /// Gets or sets the call back.
        /// </summary>
        /// <value>
        /// The call back.
        /// </value>
        private AsyncCallback CallBack { get; set; }

        #endregion
    }
}