using System;

namespace SomethingNeedDoing.Exceptions
{
    /// <summary>
    /// Error thrown when an error occurs inside a command.
    /// </summary>
    internal class MacroCommandError : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MacroCommandError"/> class.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public MacroCommandError(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MacroCommandError"/> class.
        /// </summary>
        /// <param name="message">Message to show.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public MacroCommandError(string message, Exception? innerException)
            : base(message, innerException)
        {
        }
    }
}
