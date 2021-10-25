using System;

namespace SomethingNeedDoing.Exceptions
{
    /// <summary>
    /// Error thrown when a timeout occurs.
    /// </summary>
    internal class EventFrameworkTimeoutError : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventFrameworkTimeoutError"/> class.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public EventFrameworkTimeoutError(string message)
            : base(message)
        {
        }
    }
}
