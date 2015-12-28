using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CodeCave.NetworkAgilityPack.Http;
using CodeCave.NetworkAgilityPack.Web;

namespace CodeCave.NetworkAgilityPack.Socks.Web
{
    public sealed class HttpWebRequestSocks : WebRequest
    {
        private byte[] _requestContentBuffer;
        private NetworkStream _networkStream;
        private SocketWithProxy _socksConnection;
        private string _requestMethod;

        #region Constructor

        /// <summary>
        /// Prevents a default instance of the <see cref="HttpWebRequestSocks"/> class from being created.
        /// </summary>
        /// <param name="requestRequestUri">The request URI.</param>
        private HttpWebRequestSocks(Uri requestRequestUri)
        {
            Method = KnownHttpVerb.Get.ToString().ToUpperInvariant();
            Headers = new WebHeaderCollection();
            MaximumAllowedRedirections = 50;
            Address = requestRequestUri;
        }

        #endregion Constructor      

        #region Properties

        /// <summary>
        /// When overridden in a descendant class, gets the URI of the Internet resource associated with the request.
        /// </summary>
        /// <returns>A <see cref="T:System.Uri"/> representing the resource associated with the request </returns>
        public override Uri RequestUri => Address;

        /// <summary>
        /// When overridden in a descendant class, gets or sets the network proxy to use to access this Internet resource.
        /// </summary>
        /// <returns>The <see cref="T:System.Net.IWebProxy"/> to use to access the Internet resource.</returns>
        public override IWebProxy Proxy { get; set; }

        /// <summary>
        /// When overridden in a descendant class, gets or sets the protocol method to use in this request.
        /// </summary>
        /// <returns>The protocol method to use in this request.</returns>
        public override string Method
        {
            get
            {
                return _requestMethod;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("HTTP verb cannot be null or empty.", nameof(value));

                value = value.ToUpperInvariant();
                var validVerbs = Enum.GetNames(typeof (KnownHttpVerb)).Select(h => h.ToUpperInvariant());

                if (!validVerbs.Contains(value))
                    throw new ArgumentException($"'{value}' is an unknown HTTP verb.", nameof(value));

                _requestMethod = value;
            }
        }

        public override WebHeaderCollection Headers { get; set; }

        /// <summary>
        /// When overridden in a descendant class, gets or sets the content length of the request data being sent.
        /// </summary>
        /// <returns>The number of bytes of request data being sent.</returns>
        public override long ContentLength { get; set; }

        /// <summary>
        /// When overridden in a descendant class, gets or sets the content type of the request data being sent.
        /// </summary>
        /// <returns>The content type of the request data.</returns>
        public override string ContentType { get; set; }

        public override ICredentials Credentials { get; set; }

        public Encoding Encoding { get; set; } = Encoding.UTF8;

        private int MaximumAllowedRedirections { get; set; }

        public bool AllowAutoRedirect { get; set; } = true;

        internal Uri Address { get; private set; }

        private AsyncCallback Callback { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        /// When overridden in a descendant class, returns a response to an Internet request.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Net.WebResponse"/> containing the response to the Internet request.
        /// </returns>
        public override WebResponse GetResponse()
        {
            if (Proxy == null)
                throw new InvalidOperationException("Proxy property cannot be null.");

            if (string.IsNullOrEmpty(Method))
                throw new InvalidOperationException("Method has not been set.");

            BeginGetResponse();
            return EndGetResponse();
        }

        /// <summary>
        /// Begins the get response.
        /// </summary>
        private void BeginGetResponse()
        {
            var proxyUri = Proxy.GetProxy(RequestUri);
            var credentials = (!string.IsNullOrEmpty(proxyUri?.AbsoluteUri) && !RequestUri.Equals((Proxy as WebProxy)?.Address))
                ? (Proxy as WebProxySocks)?.Credentials as WebProxyCredential
                : null;

            _socksConnection = new SocketWithProxy(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, ProxyType.Socks5, credentials);
            _socksConnection.Connect(RequestUri.Host, RequestUri.Port); // Open the connection 

            // Send request message
            _networkStream = new NetworkStream(_socksConnection);

            // Build request message if needed
            var requestMessage = BuildHttpRequestMessage();
            var requestMessageData = Encoding.GetBytes(requestMessage);
            _networkStream.Write(requestMessageData, 0, requestMessageData.Length);
        }

        /// <summary>
        /// Begins the get response.
        /// </summary>
        /// <param name="callback">The callback.</param>
        /// <param name="state">The state.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// Proxy property cannot be null.
        /// or
        /// Method has not been set.
        /// </exception>
        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            if (Proxy == null)
                throw new InvalidOperationException("Proxy property cannot be null.");
            if (string.IsNullOrEmpty(Method))
                throw new InvalidOperationException("Method has not been set.");

            var proxyUri = Proxy.GetProxy(RequestUri);
            var credentials = (!string.IsNullOrEmpty(proxyUri?.AbsoluteUri) && !RequestUri.Equals((Proxy as WebProxy)?.Address))
                ? (Proxy as WebProxySocks)?.Credentials as WebProxyCredential
                : null;

            _socksConnection = new SocketWithProxy(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp, ProxyType.Socks5, credentials);

            Callback = callback;

            // Open the connection 
            return _socksConnection.BeginConnect(RequestUri.Host, RequestUri.Port, EndConnectCallback, _socksConnection);
        }

        /// <summary>
        /// Ends the connect callback.
        /// </summary>
        /// <param name="ar">The ar.</param>
        /// <exception cref="SocketException"></exception>
        private void EndConnectCallback(IAsyncResult ar)
        {
            if (_socksConnection == null)
            {
                throw new SocketException();
            }

            _socksConnection.EndConnect(ar);

            // Send request message
            _networkStream = new NetworkStream(_socksConnection);
            var requestMessage = BuildHttpRequestMessage();
            var requestMessageData = Encoding.GetBytes(requestMessage); // Build request message if needed
            _networkStream.Write(requestMessageData, 0, requestMessageData.Length);

            Callback?.Invoke(ar);
        }

        /// <summary>
        /// Ends the get response.
        /// </summary>
        /// <param name="asyncResult">The asynchronous result.</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// Network is null or can't read
        /// or
        /// </exception>
        public override WebResponse EndGetResponse(IAsyncResult asyncResult)
        {
            return EndGetResponse();
        }

        /// <summary>
        /// Ends the get response.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">
        /// Network is null or can't read
        /// or
        /// </exception>
        public WebResponse EndGetResponse()
        {
            if (_networkStream == null || !_networkStream.CanRead)
                throw new InvalidOperationException("Network is null or can't read");

            HttpStatusCode responseStatusCode;
            var responseHeaders = BuildResponseHeaders(out responseStatusCode);

            Uri uri;
            string method;
            Exception error = null;
            var autoRedirect = 1;
            // Check for redirect options
            while (CheckForRedirection(responseStatusCode, responseHeaders, out uri, out method, ref error, ref autoRedirect))
            {
                autoRedirect++;

                Address = uri;
                Method = Method;

                _socksConnection.Close();
                _socksConnection.Dispose();
                _networkStream.Close();
                _networkStream.Dispose();

                BeginGetResponse();
                responseHeaders = BuildResponseHeaders(out responseStatusCode);
            }

            return new HttpWebResponseSocks(Address, Method.ToKnownHttpVerb(), responseStatusCode, responseHeaders, _networkStream);
        }

        /// <summary>
        /// Builds the response headers.
        /// </summary>
        /// <param name="responseStatusCode">The response status code.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Failed to parse response status code</exception>
        public WebHeaderCollection BuildResponseHeaders(out HttpStatusCode responseStatusCode)
        {
            var responseHeaders = new WebHeaderCollection();

            // Read HttpStatusCode of the request
            var headerLine = ReadLine(_networkStream);
            if (string.IsNullOrWhiteSpace(headerLine) || !headerLine.TryGetStatusCode(out responseStatusCode))
            {
                throw new Exception("Failed to parse response status code");
            }

            // Read all the other headers
            while (true)
            {
                headerLine = ReadLine(_networkStream);
                if (headerLine.Length == 0 || headerLine.Equals(HttpControlChars.CRLF))
                    break;

                var headerEntry = headerLine.Split(new[] { ':' }, 2);
                responseHeaders.Add(headerEntry[0], headerEntry[1]);
            }

            return responseHeaders;
        }

        //
        // Check for Redirection
        //
        // Put another way:
        //  301 & 302  - All methods are redirected to the same method but POST. POST is redirected to a GET.
        //  303 - All methods are redirected to GET
        //  307 - All methods are redirected to the same method.
        /// <summary>
        /// Checks for redirection.
        /// </summary>
        /// <param name="responseStatusCode">The response status code.</param>
        /// <param name="responseHeaders">The headers.</param>
        /// <param name="newUri">The new URI.</param>
        /// <param name="newMethod">New request method.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="autoRedirects">The automatic redirects.</param>
        /// <param name="autoRedirect"></param>
        /// <returns></returns>
        private bool CheckForRedirection(HttpStatusCode responseStatusCode, WebHeaderCollection responseHeaders, out Uri newUri, out string newMethod, ref Exception exception, ref int autoRedirect)
        {
            newUri = null;
            newMethod = null;

            if (AllowAutoRedirect && (
                responseStatusCode != HttpStatusCode.Ambiguous && // 300
                responseStatusCode != HttpStatusCode.Moved && // 301
                responseStatusCode != HttpStatusCode.Redirect && // 302
                responseStatusCode != HttpStatusCode.RedirectMethod && // 303
                responseStatusCode != HttpStatusCode.RedirectKeepVerb)) // 307
            {
                return false;
            }

            if (autoRedirect > MaximumAllowedRedirections)
            {
                return false;
            }

            var location = responseHeaders[HttpResponseHeader.Location];
            if (string.IsNullOrWhiteSpace(location))
            {
                return false;
            }

            try
            {
                newUri = new Uri(location);
                if (string.IsNullOrWhiteSpace(newUri.Host) && string.IsNullOrWhiteSpace(newUri.Host))
                {
                    var ub = new UriBuilder(Address.Scheme, Address.Host, Address.Port, location);
                    newUri = ub.Uri;
                }
                    
            }
            catch (UriFormatException ex)
            {
                exception = ex;
                newUri = null;
                return false;
            }

            if (newUri.Scheme != Uri.UriSchemeHttp && newUri.Scheme != Uri.UriSchemeHttps)
            {
                return false;
            }

            // TODO check redirect permissions using "Microsoft.Security: CA2103:ReviewImperativeSecurity"

            if (!Method.Equals(KnownHttpVerb.Get.ToString().ToUpperInvariant()) &&
                !Method.Equals(KnownHttpVerb.Head.ToString().ToUpperInvariant()))
            {
                return true;
            }

            switch (responseStatusCode)
            {
                case HttpStatusCode.Moved:
                case HttpStatusCode.Redirect:
                    if (Method.Equals(KnownHttpVerb.Post.ToString().ToUpperInvariant()))
                        newMethod = KnownHttpVerb.Get.ToString().ToUpperInvariant();
                    break;
                case HttpStatusCode.RedirectKeepVerb:
                    break;
                default:
                    newMethod = KnownHttpVerb.Get.ToString().ToUpperInvariant();
                    break;
            }

            // Is current credential object a CredentialCache or NetworkCredential type?
            var authTemp = Credentials as CredentialCache ?? (ICredentials) (Credentials as NetworkCredential);
            if (authTemp == null)
            {
                // Object is not either type that is safe for redirection - remove it
                Credentials = null;
            }

            return true;
        }

        /// <summary>
        /// Reads the line.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        string ReadLine(Stream stream)
        {
            var lineBuffer = new List<byte>();
            while (true)
            {
                var b = stream.ReadByte();
                if (b == -1)
                    return null;

                if (b == 10)
                    break;

                if (b != 13)
                    lineBuffer.Add((byte)b);
            }
            return Encoding.GetString(lineBuffer.ToArray());
        }

        /// <summary>
        /// When overridden in a descendant class, returns a <see cref="T:System.IO.Stream"/> for writing data to the Internet resource.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.IO.Stream"/> for writing data to the Internet resource.
        /// </returns>
        public override Stream GetRequestStream()
        {
            if (_requestContentBuffer == null)
                _requestContentBuffer = new byte[ContentLength];
            else if (ContentLength == default(long))
                _requestContentBuffer = new byte[int.MaxValue];
            else if (ContentLength != _requestContentBuffer.Length)
                Array.Resize(ref _requestContentBuffer, (int)ContentLength);

            return new MemoryStream(_requestContentBuffer);
        }

        /// <summary>
        /// Creates the specified request URI.
        /// </summary>
        /// <param name="requestUri">The request URI.</param>
        /// <returns></returns>
        public static new WebRequest Create(string requestUri)
        {
            return new HttpWebRequestSocks(new Uri(requestUri));
        }

        /// <summary>
        /// Creates the specified request URI.
        /// </summary>
        /// <param name="requestUri">The request URI.</param>
        /// <returns></returns>
        public static new WebRequest Create(Uri requestUri)
        {
            return new HttpWebRequestSocks(requestUri);
        }

        /// <summary>
        /// Adds the range.
        /// </summary>
        /// <param name="range">The range.</param>
        public void AddRange(int range)
        {
            AddRange("bytes", range);
        }

        /// <summary>
        /// Adds the range.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public void AddRange(int from, int to)
        {
            AddRange("bytes", from, to);
        }

        /// <summary>
        /// Adds the range.
        /// </summary>
        /// <param name="rangeSpecifier">The range specifier.</param>
        /// <param name="range">The range.</param>
        public void AddRange(string rangeSpecifier, int range)
        {
            if (rangeSpecifier == null)
                throw new ArgumentNullException(nameof(rangeSpecifier));

            string header, value = null;
            var rangeKeyExists = Headers.ContainsHeader(HttpRequestHeader.Range, out header);
            if (rangeKeyExists)
                value = Headers[HttpRequestHeader.Range];

            if (string.IsNullOrEmpty(value))
                value = rangeSpecifier + "=";
            else if (value.StartsWith(rangeSpecifier + "=", StringComparison.OrdinalIgnoreCase))
                value += ",";
            else
                throw new InvalidOperationException("rangeSpecifier");

            if (rangeKeyExists)
                Headers.Remove(HttpRequestHeader.Range);

            Headers.Add(HttpRequestHeader.Range, value + range + "-");
        }

        /// <summary>
        /// Adds the range.
        /// </summary>
        /// <param name="rangeSpecifier">The range specifier.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public void AddRange(string rangeSpecifier, int from, int to)
        {
            if (rangeSpecifier == null)
                throw new ArgumentNullException(nameof(rangeSpecifier));
            if (from < 0 || to < 0 || from > to)
                throw new ArgumentOutOfRangeException();

            string header, value = null;
            var rangeKeyExists = Headers.ContainsHeader(HttpRequestHeader.Range, out header);
            if (rangeKeyExists)
                value = Headers[HttpRequestHeader.Range];

            if (string.IsNullOrEmpty(value))
                value = rangeSpecifier + "=";
            else if (value.ToLower().StartsWith(rangeSpecifier.ToLower() + "="))
                value += ",";
            else
                throw new InvalidOperationException("rangeSpecifier");

            if (rangeKeyExists)
                Headers.Remove(Headers[HttpRequestHeader.Range]);

            Headers.Add(HttpRequestHeader.Range, value + from + "-" + to);
        }

        /// <summary>
        /// Aborts the Request
        /// </summary>
        /// <exception cref="T:System.NotImplementedException">Any attempt is made to access the method, when the method is not overridden in a descendant class. </exception>
        public new void Abort()
        {
            throw new NotImplementedException(); // TODO implement
        }

        #endregion Methods

        #region Helpers

        /// <summary>
        /// Builds the HTTP 1.1 request message.
        /// </summary>
        /// <returns></returns>
        private string BuildHttpRequestMessage3()
        {
            var message = new StringBuilder();
            message.AppendFormat("{0} {1} HTTP/1.1{3}Host: {2}{3}",
                Method, RequestUri.PathAndQuery, RequestUri.Host, HttpControlChars.CRLF);

            // Add the headers
            foreach (var key in Headers.Keys)
            {
                message.AppendFormat("{0}: {1}{2}", key, Headers[key.ToString()], HttpControlChars.CRLF);
            }

            // Add content type information
            if (!string.IsNullOrEmpty(ContentType))
                message.AppendFormat("Content-Type: {0}{1}", ContentType, HttpControlChars.CRLF);

            // Add content length information
            if (ContentLength > 0)
                message.AppendFormat("Content-Length: {0}{1}", ContentLength, HttpControlChars.CRLF);

            // Additional info
            message.AppendFormat("Connection: {0}{1}", "Close", HttpControlChars.CRLF);
            message.AppendFormat("Accept-Encoding: {0}{1}", "gzip", HttpControlChars.CRLF);

            // Add a blank line to indicate the end of the headers
            message.Append(HttpControlChars.CRLF);

            // No content to add
            if (_requestContentBuffer == null || _requestContentBuffer.Length <= 0) 
                return message.ToString();

            // Add content by reading data back from the content buffer
            using (var stream = new MemoryStream(_requestContentBuffer, false))
            {
                using (var reader = new StreamReader(stream))
                {
                    message.Append(reader.ReadToEnd());
                }
            }

            return message.ToString();
        }

        private string BuildHttpRequestMessage()
        {
            var message = new StringBuilder();
            message.AppendFormat("{0} {1} HTTP/1.1{2}", Method, RequestUri.PathAndQuery, HttpControlChars.CRLF);

            var messageHeaders = new WebHeaderCollection
            {
                [HttpRequestHeader.Host] = RequestUri.Host,
                [HttpRequestHeader.Connection] = "Close",
                [HttpRequestHeader.AcceptEncoding] = "gzip, deflate"
            };

            // Add content type information
            if (!string.IsNullOrEmpty(ContentType))
                messageHeaders[HttpRequestHeader.ContentType] = ContentType;

            // Add content length information
            if (ContentLength > 0)
                messageHeaders[HttpRequestHeader.ContentLength] = ContentLength.ToString();

            // Add the headers
            foreach (string key in Headers.Keys)
            {
                messageHeaders.Add(key, Headers[key]);
            }

            foreach (string key in messageHeaders)
            {
                message.AppendFormat("{0}: {1}{2}", key, messageHeaders[key], HttpControlChars.CRLF);
            }

            Headers = messageHeaders;

            // Add a blank line to indicate the end of the headers
            message.Append(HttpControlChars.CRLF);

            // No content to add
            if (_requestContentBuffer == null || _requestContentBuffer.Length <= 0)
                return message.ToString();

            // Add content by reading data back from the content buffer
            using (var stream = new MemoryStream(_requestContentBuffer, false))
            {
                using (var reader = new StreamReader(stream))
                {
                    message.Append(reader.ReadToEnd());
                }
            }

            return message.ToString();
        }

        #endregion Helpers
    }
}