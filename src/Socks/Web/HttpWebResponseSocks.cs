using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Mime;

namespace CodeCave.NetworkAgilityPack.Socks.Web
{
    public sealed class HttpWebResponseSocks : WebResponse
    {
        #region Member Variables

        private Stream _responseStream;
        private SocketWithProxy _socksConnection;
        private readonly KnownHttpVerb _verb;

        #endregion Member Variables

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpWebResponseSocks" /> class.
        /// </summary>
        /// <param name="responseUri">The response URI.</param>
        /// <param name="verb">The verb.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="responseHeaders">The response headers.</param>
        /// <param name="responseStream">The response stream.</param>
        /// <exception cref="InvalidOperationException"></exception>
        internal HttpWebResponseSocks(Uri responseUri, KnownHttpVerb verb, HttpStatusCode statusCode, WebHeaderCollection responseHeaders, Stream responseStream)
        {
            StatusCode = statusCode;
            Headers = responseHeaders;
            ResponseUri = responseUri;
            _socksConnection = null;
            _verb = verb;

            // Build stream object for WebResponse
            string contentEncodingKey;
            DecompressionMethods compressionMethod;
            if (!Headers.ContainsHeaderKey(HttpResponseHeader.ContentEncoding.ToString(), out contentEncodingKey) ||
                !Enum.TryParse(Headers[contentEncodingKey], true, out compressionMethod))
            {
                _responseStream = responseStream;
                return;
            }

            // If content encoding is 
            var contentEncoding = Headers[contentEncodingKey].Trim().ToLowerInvariant();
            switch (compressionMethod)
            {
                case DecompressionMethods.GZip:
                    _responseStream = new GZipStream(responseStream, CompressionMode.Decompress);
                    Headers[HttpRequestHeader.ContentLength] = "-1";
                    break;

                case DecompressionMethods.Deflate:
                    _responseStream = new DeflateStream(responseStream, CompressionMode.Decompress);
                    Headers[HttpRequestHeader.ContentLength] = "-1";
                    break;
                default:
                    throw new InvalidOperationException($"Unknown encoding type '{contentEncoding}'");
            }
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="HttpWebResponseSocks"/> class.
        /// </summary>
        ~HttpWebResponseSocks()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region WebResponse Members

        #region Methods

        /// <summary>
        /// When overridden in a descendant class, returns the data stream from the Internet resource.
        /// </summary>
        /// <returns>
        /// An instance of the <see cref="T:System.IO.Stream"/> class for reading data from the Internet resource.
        /// </returns>
        public override Stream GetResponseStream()
        {
            return _responseStream;
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public override void Close()
        {
            try
            {
                _responseStream?.Close();
            }
            catch(Exception)
            {
                // TODO manage error
            }

            try
            {
                _socksConnection.Disconnect(false);
                _socksConnection?.Close();
            }
            catch (Exception)
            {
                // TODO manage error
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();

                try
                {
                    _socksConnection?.Dispose();
                    _responseStream?.Dispose();
                }
                finally
                {
                    _socksConnection = null;
                    _responseStream = null;
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion WebResponse Members

        #endregion Methods

        #region Properties

        /// <summary>
        /// Gets the status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// When overridden in a derived class, gets a collection of header name-value pairs associated with this request.
        /// </summary>
        /// <returns>An instance of the <see cref="T:System.Net.WebHeaderCollection"/> class that contains header values associated with this response.</returns>
        public override WebHeaderCollection Headers { get; }

        /// <summary>
        /// When overridden in a descendant class, gets or sets the content length of data being received.
        /// </summary>
        /// <returns>The number of bytes returned from the Internet resource.</returns>
        public override long ContentLength
        {
            get
            {
                var contentLength = -1L;
                if (Headers == null || !Headers.HasKeys())
                    return contentLength;

                var contentLengthStr = Headers[HttpRequestHeader.ContentLength];
                return (long.TryParse(contentLengthStr, out contentLength))
                    ? contentLength
                    : -1;
            }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the content type of the data being received.
        /// </summary>
        /// <returns>A string that contains the content type of the response.</returns>
        public override string ContentType
        {
            get
            {
                string keyName;
                return Headers.ContainsHeaderKey(HttpResponseHeader.ContentType.ToString(), out keyName)
                    ? Headers[keyName] 
                    : MediaTypeNames.Text.Html;
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the response object [supports headers].
        /// </summary>
        /// <value>
        ///   <c>true</c> if response object [supports headers]; otherwise, <c>false</c>.
        /// </value>
        public override bool SupportsHeaders => true;

        /// <summary>
        /// Gets the response URI.
        /// </summary>
        /// <value>
        /// The response URI.
        /// </value>
        public override Uri ResponseUri { get; }

        /// <summary>
        /// Gets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public string Method => _verb.ToString().ToUpperInvariant();

        /// <summary>
        /// Gets a value indicating whether this instance is from cache.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is from cache; otherwise, <c>false</c>.
        /// </value>
        public override bool IsFromCache => false;

        #endregion Properties
    }
}