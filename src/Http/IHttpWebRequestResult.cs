using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CodeCave.NetworkAgilityPack.Http
{
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
