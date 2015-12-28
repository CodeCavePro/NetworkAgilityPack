using System.Net;

namespace CodeCave.NetworkAgilityPack.Auth
{
    public abstract class AuthUserPass<T> : AuthMethod<T>
    {
        protected NetworkCredential credential;

        /// <summary>
        /// Initializes a new AuthUserPass instance.
        /// </summary>
        /// <param name="server">The socket connection with the proxy server.</param>
        /// <param name="credential"></param>
        /// <exception cref="T:System.ArgumentNullException"><c>credential</c> is null.</exception>
        protected AuthUserPass(T server, NetworkCredential credential)
            : base(server)
        {
            this.credential = credential ?? new NetworkCredential();
        }

        /// <summary>
        /// Gets or sets the username to use when authenticating with the proxy server.
        /// </summary>
        /// <value>The username to use when authenticating with the proxy server.</value>
        /// <exception cref="T:System.ArgumentNullException">The specified value is null.</exception>
        public string Username
        {
            get { return (string.IsNullOrWhiteSpace(credential?.UserName)) ? null : credential.UserName; }
            internal set { credential.UserName = value; }
        }

        /// <summary>
        /// Gets or sets the password to use when authenticating with the proxy server.
        /// </summary>
        /// <value>The password to use when authenticating with the proxy server.</value>
        /// <exception cref="T:System.ArgumentNullException">The specified value is null.</exception>
        public string Password
        {
            get { return (IsAnonymous) ? null : credential.Password; }
            internal set { credential.Password = value; }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is anonymous.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is anonymous; otherwise, <c>false</c>.
        /// </value>
        public override bool IsAnonymous => (string.IsNullOrWhiteSpace(credential?.UserName));
    }
}
