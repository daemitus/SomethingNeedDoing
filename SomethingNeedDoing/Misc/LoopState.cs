namespace SomethingNeedDoing.Misc;

/// <summary>
/// The state of the macro manager.
/// </summary>
internal enum LoopState
{
    /// <summary>
    /// Not logged in.
    /// </summary>
    NotLoggedIn,

    /// <summary>
    /// Waiting.
    /// </summary>
    Waiting,

    /// <summary>
    /// Running.
    /// </summary>
    Running,

    /// <summary>
    /// Paused.
    /// </summary>
    Paused,

    /// <summary>
    /// Stopped.
    /// </summary>
    Stopped,
}
