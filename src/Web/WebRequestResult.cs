using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace CodeCave.NetworkAgilityPack.Web
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TWreq">The type of the wreq.</typeparam>
    /// <typeparam name="TWresp">The type of the wresp.</typeparam>
    public abstract class WebRequestResult<TWreq, TWresp> : IWebRequestResult, IDisposable
            where TWreq : WebRequest
            where TWresp : WebResponse
    {
        protected byte[] bufferRead;        // Buffer to read data into
        protected Stream streamResponse;    // Stream to read from
        protected DateTime transferStart;   // Used for tracking x
        protected int statusCode;
        protected long? totalBytes;
        protected bool cancelAsync;

        private event EventHandler<WebRequestProgressCompletedEventArgs> _progressCompleted;
        private event EventHandler<WebRequestProgressChangedEventArgs> _progressChanged;
        private event EventHandler<WebRequestProgressCompletedEventArgs> _progressFailed;

        #region Constructors / Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRequestResult{TWreq, TWresp}"/> class.
        /// </summary>
        /// <param name="buffSize">Size of the buff.</param>
        protected WebRequestResult(int buffSize = 8192)
        {
            Reset(buffSize);
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
            var changedInvocationList = _progressChanged?.GetInvocationList();
            if (changedInvocationList != null)
            {
                foreach (var d in changedInvocationList)
                {
                    _progressChanged -= (d as EventHandler<WebRequestProgressChangedEventArgs>);
                }
            }

            var completedInvocationList = _progressCompleted?.GetInvocationList();
            if (completedInvocationList != null)
            {
                foreach (var d in completedInvocationList)
                {
                    _progressCompleted -= (d as EventHandler<WebRequestProgressCompletedEventArgs>);
                }
            }

            var failedInvocationList = _progressFailed?.GetInvocationList();
            if (failedInvocationList != null)
            {
                foreach (var d in failedInvocationList)
                {
                    _progressFailed -= (d as EventHandler<WebRequestProgressCompletedEventArgs>);
                }
            }

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
        public virtual int StatusCode
        {
            get
            {
                if (statusCode < 0)
                {
                    statusCode = Response?.GetStatusCode() ?? 0;
                }
                
                return statusCode;
            }
        }

        /// <summary>
        /// Gets the total bytes.
        /// </summary>
        /// <value>
        /// The total bytes.
        /// </value>
        public long TotalBytes
        {
            get
            {
                if (totalBytes == null)
                {
                    totalBytes = Response?.ContentLength ?? 0L;
                }

                return totalBytes.GetValueOrDefault();
            }
        }

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
        /// Sends the asynchronous request.
        /// </summary>
        public void SendAsync()
        {
            transferStart = DateTime.Now;
            Reset(bufferRead.Length);
            Request?.BeginGetResponse(GotWebResponse, this);
        }

        /// <summary>
        /// Sends the asynchronous request.
        /// </summary>
        /// <param name="changed">Progress changed handler.</param>
        /// <param name="completed">Progress completed handler.</param>
        /// <param name="failed">Progress failed handler.</param>
        public void SendAsync(EventHandler<WebRequestProgressChangedEventArgs> changed, EventHandler<WebRequestProgressCompletedEventArgs> completed, EventHandler<WebRequestProgressCompletedEventArgs> failed)
        {
            ProgressChanged += changed;
            ProgressCompleted += completed;
            ProgressFailed += failed;

            SendAsync();
        }

        /// <summary>
        /// Sends request synchronously.
        /// </summary>
        public void Send()
        {
            transferStart = DateTime.Now;
            Reset(bufferRead.Length);

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

                OnProgressFailed(new WebRequestProgressCompletedEventArgs(ex, false));
            }
            finally
            {
                Response?.Close();
            }
        }

        /// <summary>
        /// Cancels the asynchronous SendAsync method.
        /// </summary>
        public void CancelAsync()
        {
            cancelAsync = true;
        }

        /// <summary>
        /// Call back executed when response object got asynchronously.
        /// </summary>
        /// <param name="asyncResult">The asynchronous result.</param>
        private void GotWebResponse(IAsyncResult asyncResult)
        {
            CheckIfCancelled();

            try
            {
                using (Response = Request.EndGetResponse(asyncResult) as TWresp)
                {
                    CheckIfCancelled();
                    ProcessResponse();
                }
            }
            catch (WebException ex)
            {
                Exception = ex;
                ex.Response?.Close();

                OnProgressFailed(new WebRequestProgressCompletedEventArgs(ex, false)
                {
                    TimeStarted = transferStart,
                    TimeElapsed = DateTime.Now - transferStart
                });
            }
            finally
            {
                Response?.Close();
            }
        }

        /// <summary>
        /// Checks if cancelled.
        /// </summary>
        private void CheckIfCancelled(long bytesReadSoFar = 0, long totalBytesToRead = 0)
        {
            if (!cancelAsync)
                return;

            var args = new WebRequestProgressCompletedEventArgs(null, true)
            {
                TimeStarted = transferStart,
                TimeElapsed = DateTime.Now - transferStart,
            };

            if (bytesReadSoFar >= 0)
                args.ProcessedBytes = bytesReadSoFar;

            if (bytesReadSoFar >= 0)
                args.TotalBytes = totalBytesToRead;

            OnProgressFailed(args);
        }

        /// <summary>
        /// Processes the response.
        /// </summary>
        private void ProcessResponse()
        {
            try
            {
                // Skip the request if the request contains error
                if (!IsSuccessful)
                {
                    Exception = new InvalidOperationException("Failed to get the response stream!");
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
                    Exception = new InvalidOperationException("Failed to get the response stream!");
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

                    CheckIfCancelled(bytesReadSoFar, totalBytesToRead);
                }
                // go if some amount of bytes has been read and stream can go on reading 
                while (bytesRead > 0 && streamResponse.CanRead);

                OnProgressCompleted(new WebRequestProgressCompletedEventArgs(bytesReadSoFar, totalBytesToRead, DateTime.Now - transferStart, transferStart, false));
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

        /// <summary>
        /// Resets the specified buff size.
        /// </summary>
        /// <param name="buffSize">Size of the buff.</param>
        private void Reset(int buffSize = 8192)
        {
            bufferRead = new byte[buffSize];
            streamResponse = null;
            statusCode = -1;
            totalBytes = null;
            cancelAsync = false;
            Encoding = Encoding.UTF8;
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when [progress completed].
        /// </summary>
        public event EventHandler<WebRequestProgressCompletedEventArgs> ProgressCompleted
        {
            add { _progressCompleted += value; }
            remove { _progressCompleted -= value; }
        }

        /// <summary>
        /// Raises the <see cref="E:ProgressCompleted" /> event.
        /// </summary>
        /// <param name="e">The <see cref="WebRequestProgressChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnProgressCompleted(WebRequestProgressCompletedEventArgs e)
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
        public event EventHandler<WebRequestProgressCompletedEventArgs> ProgressFailed
        {
            add { _progressFailed += value; }
            remove { _progressFailed -= value; }
        }

        /// <summary>
        /// Raises the <see cref="E:ProgressFailed" /> event.
        /// </summary>
        /// <param name="e">The <see cref="WebRequestProgressChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnProgressFailed(WebRequestProgressCompletedEventArgs e)
        {
            var handler = _progressFailed;
            handler?.Invoke(this, e);
        }

        #endregion
    }
}
