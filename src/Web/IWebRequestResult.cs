using System;
using System.Net;
using System.Text;

namespace CodeCave.NetworkAgilityPack.Web
{
    public interface IWebRequestResult
    {
        /// <summary>
        /// Gets or sets the request.
        /// </summary>
        /// <value>
        /// The request.
        /// </value>
        WebRequest Request { get; }

        /// <summary>
        /// Gets or sets the response.
        /// </summary>
        /// <value>
        /// The response.
        /// </value>
        WebResponse Response { get; }

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        /// <value>
        /// The exception.
        /// </value>
        Exception Exception { get; }

        /// <summary>
        /// Gets or sets the encoding.
        /// </summary>
        /// <value>
        /// The encoding.
        /// </value>
        Encoding Encoding { get; }

        /// <summary>
        /// Gets the status code.
        /// </summary>
        /// <value>
        /// The status code.
        /// </value>
        int StatusCode { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is successful.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is successful; otherwise, <c>false</c>.
        /// </value>
        bool IsSuccessful { get; }

        /// <summary>
        /// Determines whether [is status error].
        /// </summary>
        /// <returns></returns>
        bool IsStatusError();

        /// <summary>
        /// Sends request synchronously.
        /// </summary>
        void Send();

        /// <summary>
        /// Sends request asynchronously.
        /// </summary>
        void SendAsync();

        #region Events

        /// <summary>
        /// Occurs when [progress completed].
        /// </summary>
        event WebRequestProgressChangedEventHandler ProgressCompleted;

        /// <summary>
        /// Occurs when [progress changed].
        /// </summary>
        event WebRequestProgressChangedEventHandler ProgressChanged;

        #endregion
    }
}
