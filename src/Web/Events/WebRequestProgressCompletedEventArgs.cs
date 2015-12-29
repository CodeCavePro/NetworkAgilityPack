using System;
using System.ComponentModel;
using System.Net;

namespace CodeCave.NetworkAgilityPack.Web
{
    public class WebRequestProgressCompletedEventArgs : ProgressChangedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebRequestProgressCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="error">The error.</param>
        /// <param name="cancelled">if set to <c>true</c> [cancelled].</param>
        internal WebRequestProgressCompletedEventArgs(Exception error, bool cancelled)
            : base(100, Convert.ToBase64String(Guid.NewGuid().ToByteArray()))
        {
            Error = error;
            Cancelled = cancelled;

            try
            {
                var webException = Error as WebException;
                if (webException == null)
                    return;

                ProcessedBytes = 0;
                TotalBytes = webException.Response?.ContentLength ?? 0;
            }
            finally
            {

            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRequestProgressCompletedEventArgs"/> class.
        /// </summary>
        /// <param name="processedBytes">The processed bytes.</param>
        /// <param name="totalBytes">The total bytes.</param>
        /// <param name="timeElapsed">The time elapsed.</param>
        /// <param name="timeStarted">The time started.</param>
        /// <param name="cancelled">if set to <c>true</c> [cancelled].</param>
        internal WebRequestProgressCompletedEventArgs(long processedBytes, long totalBytes, TimeSpan timeElapsed, DateTime timeStarted, bool cancelled)
            : base(100, Convert.ToBase64String(Guid.NewGuid().ToByteArray()))
        {
            ProcessedBytes = processedBytes;
            TotalBytes = totalBytes;
            TimeStarted = timeStarted;
            TimeElapsed = timeElapsed;
            Cancelled = cancelled;
        }

        /// <summary>
        /// Gets the time elapsed.
        /// </summary>
        /// <value>
        /// The time elapsed.
        /// </value>
        public TimeSpan TimeElapsed { get; internal set; }

        /// <summary>
        /// Gets the time started.
        /// </summary>
        /// <value>
        /// The time started.
        /// </value>
        public DateTime TimeStarted { get; internal set; }

        /// <summary>
        /// Gets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public Exception Error { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="WebRequestProgressCompletedEventArgs"/> is cancelled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if cancelled; otherwise, <c>false</c>.
        /// </value>
        public bool Cancelled { get; }


        /// <summary>
        /// The number of processed bytes.
        /// </summary>
        /// <value>
        /// Number of processed bytes.
        /// </value>
        public long ProcessedBytes { get; internal set; }

        /// <summary>
        /// Gets the KB received.
        /// </summary>
        /// <value>
        /// The kilo bytes received.
        /// </value>
        public double ProcessedKiloBytes => Math.Round(ProcessedBytes / 1024f, 1);

        /// <summary>
        /// Gets the MB received.
        /// </summary>
        /// <value>
        /// The mega bytes received.
        /// </value>
        public double ProcessedMegaBytes => Math.Round(ProcessedBytes / 1024f / 1024, 1);

        /// <summary>
        /// The total number of bytes.
        /// </summary>
        /// <value>
        /// Total number of bytes.
        /// </value>
        public long TotalBytes { get; internal set; }

        /// <summary>
        /// Gets the total KB to receive.
        /// </summary>
        /// <value>
        /// The total kilo bytes to receive.
        /// </value>
        public double TotalKiloBytes => Math.Round(TotalBytes / 1024f, 1);

        /// <summary>
        /// Gets the total MB to receive.
        /// </summary>
        /// <value>
        /// The total mega bytes to receive.
        /// </value>
        public double TotalMegaBytes => Math.Round(TotalBytes / 1024f / 1024, 1);
    }
}
