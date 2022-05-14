using System;

namespace SomethingNeedDoing.Exceptions;

/// <summary>
/// Error thrown when a /craft or /gate command has reached the limit.
/// </summary>
internal class GateComplete : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GateComplete"/> class.
    /// </summary>
    public GateComplete()
        : base("Gate reached")
    {
    }
}
