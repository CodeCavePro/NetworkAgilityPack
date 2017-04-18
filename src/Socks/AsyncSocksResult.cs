using System;
using System.Threading;

namespace CodeCave.NetworkAgilityPack.Socks
{
    internal sealed class AsyncSocksResult : IAsyncResult
    {
        /// <summary>
        /// Initializes the specified state object.
        /// </summary>
        /// <param name="stateObject">The state object.</param>
        internal void Init(object stateObject)
        {
            AsyncState = stateObject;
            IsCompleted = false;
            Handle?.Reset();
        }

        /// <summary>
        /// Resets this instance and initializes object properties to their default values.
        /// </summary>
        internal void Reset()
        {
            AsyncState = null;
            IsCompleted = true;
            Handle?.Set();
        }

        public bool IsCompleted { get; private set; } = true;

        public bool CompletedSynchronously => false;

        public object AsyncState { get; private set; }

        public ManualResetEvent Handle { get; private set; }

        public WaitHandle AsyncWaitHandle => Handle ?? (Handle = new ManualResetEvent(false));
    }
}
