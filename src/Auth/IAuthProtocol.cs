using System;
using System.Net;
using CodeCave.NetworkAgilityPack.Socks;

namespace CodeCave.NetworkAgilityPack.Auth
{
    /// <summary>
    /// Interface which defines basic methods for authentication protocol
    /// </summary>
    public interface IAuthProtocol
    {
        #region Methods

        /// <summary>
        /// Authenticates the user.
        /// </summary>
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        void Authenticate();

        /// <summary>
        /// Authenticates the user asynchronously.
        /// </summary>
        /// <param name="callback">The method to call when the authentication is complete.</param>
        /// <exception cref="ProtocolViolationException">The proxy server uses an invalid protocol.</exception>
        /// <exception cref="ObjectDisposedException">The Socket has been closed.</exception>
        void BeginAuthenticate(HandShakeComplete callback);

        #endregion Methods

        #region Properties

        /// <summary>
        /// Gets or sets a byte array that can be used to store data.
        /// </summary>
        /// <value>A byte array to store data.</value>
        byte[] Buffer { get; set; }

        /// <summary>
        /// Gets or sets the number of bytes that have been received from the remote proxy server.
        /// </summary>
        /// <value>An integer that holds the number of bytes that have been received from the remote proxy server.</value>
        int Received { get; set; }

        #endregion Properties
    }
}
