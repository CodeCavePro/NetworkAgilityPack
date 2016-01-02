using System;
using System.Net;

namespace CodeCave.NetworkAgilityPack.Web
{
    /// <summary>
    /// WebProxy credentials
    /// </summary>
    /// <seealso cref="System.Net.NetworkCredential" />
    public class WebProxyCredential : NetworkCredential
    {
        /// <summary>
        /// Proxy IP address
        /// </summary>
        protected readonly IPAddress ipAddress;

        /// <summary>
        /// Proxy port
        /// </summary>
        protected readonly int port;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxyCredential"/> class.
        /// </summary>
        /// <param name="ipAddress">The IP address.</param>
        /// <param name="port">The port.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        public WebProxyCredential(IPAddress ipAddress, int port, string userName = "", string password = "")
        {
            this.ipAddress = ipAddress;
            this.port = port;
            UserName = userName;
            Password = password;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxyCredential" /> class.
        /// </summary>
        /// <param name="proxyHostname">The proxy hostname.</param>
        /// <param name="port">The port.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        public WebProxyCredential(string proxyHostname, int port, string userName = "", string password = "")
        {
            Uri proxyAddress;
            if (Uri.TryCreate(proxyHostname, UriKind.Absolute, out proxyAddress))
            {
                proxyAddress.TryResolveIpAddress(out ipAddress);
            }
            else if (IPAddress.TryParse(proxyHostname, out ipAddress))
            {
            }
            else
            {
                throw new InvalidOperationException();
            }

            this.port = port;
            UserName = userName;
            Password = password;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxyCredential"/> class.
        /// </summary>
        /// <param name="proxyAddress">The proxy address.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        public WebProxyCredential(Uri proxyAddress, string userName = "", string password = "")
        {
            proxyAddress.TryResolveIpAddress(out ipAddress);
            port = proxyAddress.Port;
            UserName = userName;
            Password = password;
        }

        /// <summary>
        /// Gets authentication domain.
        /// </summary>
        /// <value>
        /// Authentication domain.
        /// </value>
        public new string Domain => $"{IPAddress}:{Port}";

        /// <summary>
        /// Gets the address.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        public IPAddress IPAddress => ipAddress;

        /// <summary>
        /// Gets the port.
        /// </summary>
        /// <value>
        /// The port.
        /// </value>
        public int Port => port;

        /// <summary>
        /// Gets the ip end point.
        /// </summary>
        /// <value>
        /// The ip end point.
        /// </value>
        public IPEndPoint IPEndPoint => new IPEndPoint(IPAddress, Port);
    }
}
