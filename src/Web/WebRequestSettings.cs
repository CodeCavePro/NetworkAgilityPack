using System.Collections.Generic;
using System.Net;
using System.Text;
using CodeCave.NetworkAgilityPack.Socks;

namespace CodeCave.NetworkAgilityPack.Web
{
    /// <summary>
    /// Web request settings
    /// </summary>
    public class WebRequestSettings
    {
        #region Properties

        /// <summary>
        /// Gets or sets the encoding.
        /// </summary>
        /// <value>
        /// The encoding.
        /// </value>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Gets or sets the headers override.
        /// </summary>
        /// <value>
        /// The headers override.
        /// </value>
        public Dictionary<HttpRequestHeader, string> Headers { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the defaults.
        /// </summary>
        /// <returns>Default web request settings</returns>
        public static WebRequestSettings GetDefault()
        {
            return new WebRequestSettings
            {
                ProxyType = ProxyType.System,
                Encoding = null,
                Headers = new Dictionary<HttpRequestHeader, string>(),
            };
        }

        /// <summary>
        /// Gets or sets the type of the proxy.
        /// </summary>
        /// <value>
        /// The type of the proxy.
        /// </value>
        public ProxyType ProxyType { get; set; }

        /// <summary>
        /// Gets or sets the proxy credentials.
        /// </summary>
        /// <value>
        /// The proxy credentials.
        /// </value>
        public WebProxyCredential ProxyCredentials { get; set; }

        /// <summary>
        /// Gets or sets the proxy.
        /// </summary>
        /// <value>
        /// The proxy.
        /// </value>
        public virtual IWebProxy Proxy
        {
            get
            {
                if (ProxyCredentials != null && (ProxyType.None == ProxyType || ProxyType.System == ProxyType))
                {
                    ProxyType = ProxyType.Http;
                }

                switch (ProxyType)
                {
                    case ProxyType.None:
                        return null;

                    case ProxyType.Http:
                        return WebRequest.GetSystemWebProxy();
                    
                    case ProxyType.System:
                        if (ProxyCredentials != null)
                        {
                            ProxyCredentials = null;  // remove network credentials when using system proxy
                        }
                        return new WebProxy { Credentials = ProxyCredentials };
                    
                    case ProxyType.Socks4:
                    case ProxyType.Socks5:
                        if (ProxyCredentials == null)
                            throw new SocksProxyException("Please specify proxy credentials in order to use Socks4/5");

                        return new WebProxySocks((SocksType)ProxyType, ProxyCredentials);
                }

                return null;
            }
        }

        #endregion
    }
}
