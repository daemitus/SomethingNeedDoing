using System;

using SomethingNeedDoing.Misc;

namespace SomethingNeedDoing.Exceptions;

/// <summary>
/// Error thrown when a macro needs to pause, but not treat it like an error.
/// </summary>
internal partial class MacroPause : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MacroPause"/> class.
    /// </summary>
    /// <param name="command">The reason for stopping.</param>
    /// <param name="color">SeString color.</param>
    public MacroPause(string command, UiColor color)
        : base($"Macro paused: {command}")
    {
        this.Color = color;
    }

    /// <summary>
    /// Gets the color.
    /// </summary>
    public UiColor Color { get; }
}
