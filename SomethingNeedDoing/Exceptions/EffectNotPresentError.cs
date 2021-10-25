using System;

namespace SomethingNeedDoing.Exceptions
{
    /// <summary>
    /// Error thrown when an effect is not present.
    /// </summary>
    internal class EffectNotPresentError : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EffectNotPresentError"/> class.
        /// </summary>
        /// <param name="message">Message to show.</param>
        public EffectNotPresentError(string message)
            : base(message)
        {
        }
    }
}
