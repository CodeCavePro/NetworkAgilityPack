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
        /// <param name="processedBytes">Transfered bytes.</param>
        /// <param name="totalBytes">Total bytes to transfer.</param>
        /// <param name="timeElapsed">Time elapsed.</param>
        /// <param name="transferRate">The transfer rate.</param>
        /// <param name="percentageOverride">The percentage override.</param>
        internal WebRequestProgressChangedEventArgs(long processedBytes, long totalBytes, TimeSpan timeElapsed, double transferRate, int percentageOverride = 0) 
            : base((percentageOverride <= 100) ? percentageOverride : (processedBytes <= 0) ? 0 : (int)(processedBytes * 100f / ((totalBytes <= 0) ? processedBytes : totalBytes)), 
                  Convert.ToBase64String(Guid.NewGuid().ToByteArray()))
        {
            ProcessedBytes = processedBytes;
            TotalBytes = (totalBytes > 0) ? totalBytes : processedBytes;
            TimeElapsed = timeElapsed;
            TransferRate = transferRate;
        }

        /// <summary>
        /// The number of processed bytes.
        /// </summary>
        /// <value>
        /// Number of processed bytes.
        /// </value>
        public long ProcessedBytes { get; }
        
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
        public long TotalBytes { get; }

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
