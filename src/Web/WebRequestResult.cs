using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace CodeCave.NetworkAgilityPack.Web
{
    public abstract class WebRequestResult<TWreq, TWresp> : IWebRequestResult, IDisposable
        where TWreq : WebRequest
        where TWresp : WebResponse
    {
        protected byte[] bufferRead;        // Buffer to read data into
        protected Stream streamResponse;    // Stream to read from
        protected DateTime transferStart;   // Used for tracking x
        protected int statusCode;

        private event EventHandler<WebRequestProgressChangedEventArgs> _progressCompleted;
        private event EventHandler<WebRequestProgressChangedEventArgs> _progressChanged;
        private event EventHandler<WebRequestProgressChangedEventArgs> _progressFailed;
       
        #region Constructors / Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRequestResult{TWreq, TWresp}"/> class.
        /// </summary>
        /// <param name="buffSize">Size of the buff.</param>
        protected WebRequestResult(int buffSize = 8192)
        {
            bufferRead = new byte[buffSize];
            streamResponse = null;
            statusCode = -1;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="WebRequestResult{TWreq, TWresp}"/> class.
        /// </summary>
        ~WebRequestResult()
        {
            Dispose();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        public void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            try
            {
                streamResponse?.Dispose();
            }
            finally
            {
                streamResponse = null;
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>
        /// The request.
        /// </value>
        public TWreq Request { get; internal set; }

        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        /// <value>
        /// The response.
        /// </value>
        public TWresp Response { get; internal set; }

        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        /// <value>
        /// The response.
        /// </value>
        WebResponse IWebRequestResult.Response => Response;

        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>
        /// The request.
        /// </value>
        WebRequest IWebRequestResult.Request => Request;

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        /// <value>
        /// The exception.
        /// </value>
        public Exception Exception { get; internal set; }

        /// <summary>
        /// Gets or sets the encoding.
        /// </summary>
        /// <value>
        /// The encoding.
        /// </value>
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <value>
        /// The URI.
        /// </value>
        public Uri Uri { get; internal set; }

        /// <summary>
        /// Gets the status code.
        /// </summary>
        /// <value>
        /// The status code.
        /// </value>
        public virtual int StatusCode => statusCode = Response?.GetStatusCode() ?? 0;

        /// <summary>
        /// Gets the total bytes.
        /// </summary>
        /// <value>
        /// The total bytes.
        /// </value>
        public long TotalBytes => Response?.ContentLength ?? 0L;

        /// <summary>
        /// Gets a value indicating whether this instance is successful.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is successful; otherwise, <c>false</c>.
        /// </value>
        public bool IsSuccessful => (Exception == null && !IsStatusError());

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether [is status error].
        /// </summary>
        /// <returns></returns>
        public abstract bool IsStatusError();

        /// <summary>
        /// Sends request asynchronously.
        /// </summary>
        public void SendAsync()
        {
            Request?.BeginGetResponse(GotWebResponse, this);
        }

        /// <summary>
        /// Sends request synchronously.
        /// </summary>
        public void Send()
        {
            try
            {
                using (Response = Request?.GetResponse() as TWresp)
                {
                    ProcessResponse();
                }
            }
            catch (WebException ex)
            {
                Exception = ex;
                ex.Response?.Close();

                OnProgressFailed(new WebRequestProgressChangedEventArgs(ex));
            }
            finally
            {
                Response?.Close();
            }
        }

        /// <summary>
        /// Gots the response.
        /// </summary>
        /// <param name="asyncResult">The asynchronous result.</param>
        private void GotWebResponse(IAsyncResult asyncResult)
        {
            try
            {
                using (Response = Request.EndGetResponse(asyncResult) as TWresp)
                {
                    ProcessResponse();
                }
            }
            catch (WebException ex)
            {
                Exception = ex;
                ex.Response?.Close();

                OnProgressFailed(new WebRequestProgressChangedEventArgs(ex));
            }
            finally
            {
                Response?.Close();
            }
        }

        /// <summary>
        /// Processes the response.
        /// </summary>
        private void ProcessResponse()
        {
            try
            {
                transferStart = DateTime.Now;

                // Skip the reqst if the request contains error
                if (!IsSuccessful)
                {
                    Exception = new InvalidOperationException("Failed to get the resposnse stream!");
                    bufferRead = null;
                    return;
                }

                // Skip the rest of the method for HEAD request (which needs only response headers)
                if (Request.Method.Equals(KnownHttpVerb.Head.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    bufferRead = new byte[0];
                    return;
                }

                // Get response stream
                streamResponse = Response?.GetResponseStream();

                if (streamResponse == null)
                {
                    Exception = new InvalidOperationException("Failed to get the resposnse stream!");
                    bufferRead = null;
                    return;
                }

                long bytesRead;
                const short numReadsBeforeProgUpdateDefault = 32;
                var numReadsCounter = numReadsBeforeProgUpdateDefault / 2;
                var bytesReadSoFar = 0L;
                var totalBytesToRead = TotalBytes;
                var lastUpdateTime = DateTime.Now;
                var lastUpdateDownloadedSize = 0L;

                do
                {
                    // Read stream content
                    bytesRead = streamResponse.Read(bufferRead, 0, bufferRead.Length);
                    bytesReadSoFar += bytesRead;

                    if (numReadsCounter > numReadsBeforeProgUpdateDefault)
                    {
                        // Calculate request progress and report it
                        var dateTimeNow = DateTime.Now;
                        var timeDiff = (dateTimeNow - lastUpdateTime).TotalSeconds;
                        var sizeDiff = bytesReadSoFar - lastUpdateDownloadedSize;
                        var transferSpeed = Math.Round(sizeDiff / timeDiff / 1024f, 2);
                        lastUpdateDownloadedSize = bytesReadSoFar;
                        lastUpdateTime = dateTimeNow;

                        // Reset reads counter
                        numReadsCounter = 0;

                        // Report the progress
                        var args = new WebRequestProgressChangedEventArgs(bytesReadSoFar, totalBytesToRead, DateTime.Now - transferStart, transferSpeed, 99);
                        OnProgressChanged(args);
                    }
                    else
                    {
                        numReadsCounter++;
                    }
                }
                // go if some amount of bytes has been read and stream can go on reading 
                while (bytesRead > 0 && streamResponse.CanRead);

                OnProgressCompleted(new WebRequestProgressChangedEventArgs(bytesReadSoFar, totalBytesToRead, DateTime.Now - transferStart, 0));
            }
            catch (Exception ex)
            {
                Exception = ex;
                bufferRead = null;
                throw;
            }
            finally
            {
                streamResponse?.Close();
            }
        }

        /// <summary>
        /// Adds the headers.
        /// </summary>
        /// <param name="headers">The headers.</param>
        public virtual void AddHeaders(Dictionary<HttpRequestHeader, string> headers)
        {
            if (headers == null || !headers.Any())
                return;

            // Apply all header changes
            foreach (var headerKey in headers.Select(header => header.Key))
            {
                switch (headerKey)
                {
                    case HttpRequestHeader.ContentLength:
                        Request.ContentLength = long.Parse(headers[headerKey]); // TODO make long parsing safer
                        continue;
                    case HttpRequestHeader.ContentType:
                        Request.ContentType = headers[headerKey];
                        continue;
                }

                // Set the rest of the headers
                Request.Headers.Set(headerKey, headers[headerKey]);
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [progress completed].
        /// </summary>
        public event EventHandler<WebRequestProgressChangedEventArgs> ProgressCompleted
        {
            add { _progressCompleted += value; }
            remove { _progressCompleted -= value; }
        }

        /// <summary>
        /// Raises the <see cref="E:ProgressCompleted" /> event.
        /// </summary>
        /// <param name="e">The <see cref="WebRequestProgressChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnProgressCompleted(WebRequestProgressChangedEventArgs e)
        {
            var handler = _progressCompleted;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Occurs when [progress changed].
        /// </summary>
        public event EventHandler<WebRequestProgressChangedEventArgs> ProgressChanged
        {
            add { _progressChanged += value; }
            remove { _progressChanged -= value; }
        }

        /// <summary>
        /// Raises the <see cref="E:ProgressChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="WebRequestProgressChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnProgressChanged(WebRequestProgressChangedEventArgs e)
        {
            var handler = _progressChanged;
            handler?.Invoke(this, e);
        }

        /// <summary>
        /// Occurs when [progress failed].
        /// </summary>
        public event EventHandler<WebRequestProgressChangedEventArgs> ProgressFailed
        {
            add { _progressFailed += value; }
            remove { _progressFailed -= value; }
        }

        /// <summary>
        /// Raises the <see cref="E:ProgressFailed" /> event.
        /// </summary>
        /// <param name="e">The <see cref="WebRequestProgressChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnProgressFailed(WebRequestProgressChangedEventArgs e)
        {
            var handler = _progressFailed;
            handler?.Invoke(this, e);
        }

        #endregion
    }
}
