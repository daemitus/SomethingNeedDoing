using System;

namespace SomethingNeedDoing.Exceptions
{
    /// <summary>
    /// Error thrown when the syntax of a macro does not parse correctly.
    /// </summary>
    internal class MacroSyntaxError : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MacroSyntaxError"/> class.
        /// </summary>
        /// <param name="message">Message to show.</param>
        /// <param name="lineNumber">Error line number.</param>
        /// <param name="index">Error index.</param>
        public MacroSyntaxError(string message, int lineNumber, int index)
            : base(message)
        {
            this.LineNumber = lineNumber;
            this.Index = index;
        }

        /// <summary>
        /// Gets the line number the error occurred at.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Gets the character index the error occurred at.
        /// </summary>
        public int Index { get; }
    }
}
