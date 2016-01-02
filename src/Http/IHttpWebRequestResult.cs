using System.Net;

namespace CodeCave.NetworkAgilityPack.Http
{
    /// <summary>
    /// Interface which defines methods and properties of HTTP requests
    /// </summary>
    public interface IHttpWebRequestResult
    {
        /// <summary>
        /// Gets the HTTP status code.
        /// </summary>
        /// <value>
        /// The HTTP status code.
        /// </value>
        HttpStatusCode HttpStatusCode { get; }
    }
}
