using System;
using System.Net;

namespace CodeCave.NetworkAgilityPack.Auth
{
    /// <summary>
    /// Authentication method for an URI with username and password
    /// </summary>
    /// <seealso cref="T:CodeCave.NetworkAgilityPack.Auth.AuthUserPass{System.Uri}" />
    public class AuthUserPassUri : AuthUserPass<Uri>
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthUserPassUri"/> class.
        /// </summary>
        /// <param name="serverUrl">The server URL.</param>
        /// <param name="port">The port.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        /// <param name="domain">The domain.</param>
        public AuthUserPassUri(string serverUrl, int port, string userName = null, string password = null, string domain = null)
         : this(server: (port > 0)
                ? (new UriBuilder(serverUrl) { Port = port }).Uri
                : (new Uri(serverUrl)),
               credential:(string.IsNullOrWhiteSpace(userName))
                     ? null
                     : new NetworkCredential(userName, password, domain))
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthUserPassUri"/> class.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        /// <param name="credential"></param>
        public AuthUserPassUri(Uri server, NetworkCredential credential = null)
            : base(server, credential)
        {}

        #endregion

        #region Properties

        /// <summary>
        /// Gets the host.
        /// </summary>
        /// <value>
        /// The host.
        /// </value>
        public string Host
        {
            get { return Endpoint.Host; }
            internal set { Endpoint = (new UriBuilder($"{value}:{Port}")).Uri; }
        }

        /// <summary>
        /// Gets the absolute path.
        /// </summary>
        /// <value>
        /// The absolute path.
        /// </value>
        public string AbsolutePath => Endpoint.AbsolutePath;

        /// <summary>
        /// Gets the port.
        /// </summary>
        /// <value>
        /// The port.
        /// </value>
        public int Port
        {
            get { return Endpoint.Port; }
            internal set { Endpoint = (new UriBuilder($"{Host}:{value}")).Uri; }
        }

        #endregion

    }
}
