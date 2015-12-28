using System;
using System.Net;
using System.Net.Sockets;
using CodeCave.NetworkAgilityPack.Auth;

namespace CodeCave.NetworkAgilityPack.Socks.Auth
{
    internal class AuthNoneSocket : AuthUserPass<Socket>, IAuthProtocol
    {
        #region Methods

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthNoneSocket"/> class.
        /// </summary>
        /// <param name="server">The server.</param>
        public AuthNoneSocket(Socket server) : base(server, null)
        {}

        /// <summary>
        /// Authenticates the user.
        /// </summary>
        public virtual void Authenticate() {}

        /// <summary>
        /// Authenticates the user asynchronously.
        /// </summary>
        /// <param name="callback">The method to call when the authentication is complete.</param>
        /// <remarks>This method immediately calls the callback method.</remarks>
        public void BeginAuthenticate(HandShakeComplete callback)
        {
            callback(null);
        }

        /// <summary>
        /// Gets or sets a byte array that can be used to store data.
        /// </summary>
        /// <value>
        /// A byte array to store data.
        /// </value>
        public byte[] Buffer { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes that have been received from the remote proxy server.
        /// </summary>
        /// <value>
        /// An integer that holds the number of bytes that have been received from the remote proxy server.
        /// </value>
        public int Received { get; set; }

        #endregion Methods
    }
}
