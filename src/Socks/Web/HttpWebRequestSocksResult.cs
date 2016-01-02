using System;
using System.Net;
using CodeCave.NetworkAgilityPack.Http;
using CodeCave.NetworkAgilityPack.Web;

namespace CodeCave.NetworkAgilityPack.Socks.Web
{
    /// <summary>
    /// A wrapper for HttpWebRequests and HttpWebResponse via Socks protocol
    /// </summary>
    /// <seealso cref="T:CodeCave.NetworkAgilityPack.Web.WebRequestResult{CodeCave.NetworkAgilityPack.Socks.Web.HttpWebRequestSocks, CodeCave.NetworkAgilityPack.Socks.Web.HttpWebResponseSocks}" />
    /// <seealso cref="T:CodeCave.NetworkAgilityPack.Http.IHttpWebRequestResult" />
    public sealed class HttpWebRequestSocksResult : WebRequestResult<HttpWebRequestSocks, HttpWebResponseSocks>, IHttpWebRequestResult
    {
        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        /// <value>
        /// The HTTP status code.
        /// </value>
        public HttpStatusCode HttpStatusCode => (HttpStatusCode)StatusCode;

        /// <summary>
        /// Determines whether [is status error].
        /// </summary>
        /// <returns></returns>
        public override bool IsStatusError()
        {
            return StatusCode >= 400;
        }

        /// <summary>
        /// Creates the request object for the specified URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <returns></returns>
        public static IWebRequestResult Create(Uri uri)
        {
            return new HttpWebRequestSocksResult
            {
                Request = (HttpWebRequestSocks) HttpWebRequestSocks.Create(uri),
                Uri = uri
            };
        }
    }
}
