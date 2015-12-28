using System;
using System.Collections.Generic;
using System.Net;
using CodeCave.NetworkAgilityPack.Web;

namespace CodeCave.NetworkAgilityPack.Http
{
    public sealed class HttpWebRequestResult : WebRequestResult<HttpWebRequest, HttpWebResponse>, IHttpWebRequestResult
    {
        /// <summary>
        /// Gets or sets the HTTP status code.
        /// </summary>
        /// <value>
        /// The HTTP status code.
        /// </value>
        public HttpStatusCode HttpStatusCode => (HttpStatusCode) StatusCode;

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
            return new HttpWebRequestResult
            {
                Request = (HttpWebRequest)WebRequest.Create(uri)
            };
        }

        /// <summary>
        /// Adds the headers.
        /// </summary>
        /// <param name="headers">The headers.</param>
        public override void AddHeaders(Dictionary<HttpRequestHeader, string> headers)
        {
            // Set "restricted" header fields directly http://stackoverflow.com/questions/239725/cannot-set-some-http-headers-when-using-system-net-webrequest
            foreach (var header in headers)
            {
                switch (header.Key)
                {
                    case HttpRequestHeader.Accept:
                        Request.Accept = header.Value;
                        continue;
                    case HttpRequestHeader.Connection:
                        Request.Connection = header.Value;
                        continue;
                    case HttpRequestHeader.Date:
                        Request.Date = DateTime.Parse(header.Value); // TODO make DateTime casting safer
                        continue;
                    case HttpRequestHeader.Expect:
                        Request.Expect = header.Value;
                        continue;
                    case HttpRequestHeader.Host:
                        Request.Host = header.Value;
                        continue;
                    case HttpRequestHeader.IfModifiedSince:
                        Request.IfModifiedSince = DateTime.Parse(header.Value); // TODO make DateTime casting safer
                        continue;
                    case HttpRequestHeader.Range:
                        var range = long.Parse(header.Value); // TODO make long parsing safer
                        Request.AddRange(range);
                        continue;
                    case HttpRequestHeader.Referer:
                        Request.Referer = header.Value;
                        continue;
                    case HttpRequestHeader.TransferEncoding:
                        Request.TransferEncoding = header.Value;
                        continue;
                    case HttpRequestHeader.UserAgent:
                        Request.UserAgent = header.Value;
                        continue;
                    case HttpRequestHeader.ContentLength:
                        Request.ContentLength = long.Parse(header.Value);
                        // TODO make long parsing safer
                        continue;
                    case HttpRequestHeader.ContentType:
                        Request.ContentType = header.Value;
                        continue;
                }

                // Set the rest of the headers
                Request.Headers.Set(header.Key, header.Value);
            }
        }
    }
}
