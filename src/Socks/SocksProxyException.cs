using System.Net;

namespace CodeCave.NetworkAgilityPack.Socks
{
	/// <summary>
	/// The exception that is thrown when a proxy error occurs.
	/// </summary>
	public class SocksProxyException : WebException
    {
		/// <summary>
		/// Initializes a new instance of the ProxyException class.
		/// </summary>
		public SocksProxyException() : this("An error occurred trying to connect to the proxy server.") {}

		/// <summary>
		/// Initializes a new instance of the ProxyException class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public SocksProxyException(string message) : base(message) {}

        /// <summary>
	    /// Initializes a new instance of the ProxyException class.
	    /// </summary>
	    /// <param name="socks4Error">The error number returned by a SOCKS5 server.</param>
	    public static SocksProxyException FromSocks4Error(int socks4Error)
        {
            var message = EnumExtensions.GetEnumDescription((SocksV4Status)socks4Error);
            return new SocksProxyException(message);
        }

        /// <summary>
        /// Initializes a new instance of the ProxyException class.
        /// </summary>
        /// <param name="socks5Error">The error number returned by a SOCKS5 server.</param>
        public static SocksProxyException FromSocks5Error(int socks5Error)
        {
            var message = EnumExtensions.GetEnumDescription((SocksV5Status)socks5Error);
            return new SocksProxyException(message);
        }
    }
}