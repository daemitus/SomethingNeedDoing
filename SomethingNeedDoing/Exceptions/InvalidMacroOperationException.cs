using System;

namespace SomethingNeedDoing.Exceptions
{
    /// <summary>
    /// Error thrown when an invalid macro operation occurs.
    /// </summary>
    internal class InvalidMacroOperationException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidMacroOperationException"/> class.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public InvalidMacroOperationException(string message)
            : base(message)
        {
        }
    }
}
