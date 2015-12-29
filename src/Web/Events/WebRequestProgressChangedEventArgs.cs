using System;
using System.ComponentModel;
using System.Net;

namespace CodeCave.NetworkAgilityPack.Web
{
    public sealed class WebRequestProgressChangedEventArgs : ProgressChangedEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebRequestProgressChangedEventArgs" /> class.
        /// </summary>
        /// <param name="bytes">Transfered bytes.</param>
        /// <param name="totalBytes">Total bytes to transfer.</param>
        /// <param name="timeElapsed">Time elapsed.</param>
        /// <param name="transferRate">The transfer rate.</param>
        /// <param name="percentageOverride">The percentage override.</param>
        internal WebRequestProgressChangedEventArgs(long bytes, long totalBytes, TimeSpan timeElapsed, double transferRate, int percentageOverride = 0) 
            : base((percentageOverride <= 100) ? percentageOverride : (bytes <= 0) ? 0 : (int)(bytes * 100f / ((totalBytes <= 0) ? bytes : totalBytes)), 
                  Convert.ToBase64String(Guid.NewGuid().ToByteArray()))
        {
            BytesReceived = bytes;
            TotalBytesToReceive = (totalBytes > 0) ? totalBytes : bytes;
            TimeElapsed = timeElapsed;
            TransferRate = transferRate;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebRequestProgressChangedEventArgs"/> class.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public WebRequestProgressChangedEventArgs(Exception exception) 
            : base(100, Convert.ToBase64String(Guid.NewGuid().ToByteArray()))
        {
            Exception = exception;

            try
            {
                var webException = exception as WebException;
                if (webException == null)
                    return;

                BytesReceived = 0;
                TotalBytesToReceive = webException.Response?.ContentLength ?? 0;
                TransferRate = 0;
            }
            finally
            {
                
            }
        }

        /// <summary>
        /// Gets the exception.
        /// </summary>
        /// <value>
        /// The exception.
        /// </value>
        public Exception Exception { get; }

        /// <summary>
        /// Gets the received bytes.
        /// </summary>
        /// <value>
        /// The received bytes.
        /// </value>
        public long BytesReceived { get; }
        
        /// <summary>
        /// Gets the KB received.
        /// </summary>
        /// <value>
        /// The kilo bytes received.
        /// </value>
        public double KiloBytesReceived => Math.Round(BytesReceived / 1024f, 1);

        /// <summary>
        /// Gets the MB received.
        /// </summary>
        /// <value>
        /// The mega bytes received.
        /// </value>
        public double MegaBytesReceived => Math.Round(BytesReceived / 1024f / 1024, 1);

        /// <summary>
        /// Gets total bytes to receive.
        /// </summary>
        /// <value>
        /// Total bytes to receive.
        /// </value>
        public long TotalBytesToReceive { get; }

        /// <summary>
        /// Gets the total KB to receive.
        /// </summary>
        /// <value>
        /// The total kilo bytes to receive.
        /// </value>
        public double TotalKiloBytesToReceive => Math.Round(TotalBytesToReceive / 1024f, 1);

        /// <summary>
        /// Gets the total MB to receive.
        /// </summary>
        /// <value>
        /// The total mega bytes to receive.
        /// </value>
        public double TotalMegaBytesToReceive => Math.Round(TotalBytesToReceive / 1024f / 1024, 1);

        /// <summary>
        /// Gets the time elapsed.
        /// </summary>
        /// <value>
        /// The time elapsed.
        /// </value>
        public TimeSpan TimeElapsed { get; }

        /// <summary>
        /// Gets the transfer rate - Kb per sec (download speed).
        /// </summary>
        /// <value>
        /// Transfer rate - Kb per sec (download speed).
        /// </value>
        public double TransferRate { get; }
    }
}
