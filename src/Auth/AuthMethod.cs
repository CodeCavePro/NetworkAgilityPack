using System;

namespace CodeCave.NetworkAgilityPack.Auth
{
	/// <summary>
	/// A base class for authentication
	/// </summary>
	/// <remarks>This is an abstract class; it must be inherited.</remarks>
	public abstract class AuthMethod<T> : IAuthMethod
    {
        #region Variables

        /// <summary>Holds the value of the Server property.</summary>
        protected T endpoint;

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Initializes an AuthMethod instance.
        /// </summary>
        /// <param name="endpoint">The socket connection with the proxy server.</param>
		protected AuthMethod(T endpoint)
        {
            Endpoint = endpoint;
		}

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets the enpoint (server) connection with the proxy server.
        /// </summary>
        /// <value>
        /// The server connection with the proxy server.
        /// </value>
        /// <exception cref="ArgumentNullException"></exception>
        public T Endpoint
        {
            get
            {
                return endpoint;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                endpoint = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is anonymous.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is anonymous; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsAnonymous { get; }

        #endregion Properties
    }
}