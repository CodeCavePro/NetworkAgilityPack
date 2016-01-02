using System.Net;
using CodeCave.NetworkAgilityPack.Socks;

namespace CodeCave.NetworkAgilityPack.Web
{
    /// <summary>
    /// WebProxy via Socks protocol
    /// </summary>
    /// <seealso cref="System.Net.WebProxy" />
    public sealed class WebProxySocks : WebProxy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebProxySocks" /> class.
        /// </summary>
        /// <param name="socksType">Type of the socks.</param>
        /// <param name="credentials">Proxy credentials.</param>
        public WebProxySocks(SocksType socksType, WebProxyCredential credentials)
            : base(credentials.IPAddress.ToString(), credentials.Port)
        {
            SocksType = socksType;
            Credentials = credentials;
        }

        /// <summary>
        /// Gets the type of the socks.
        /// </summary>
        /// <value>
        /// The type of the socks.
        /// </value>
        public SocksType SocksType
        {
            get;
        }
    }
}
