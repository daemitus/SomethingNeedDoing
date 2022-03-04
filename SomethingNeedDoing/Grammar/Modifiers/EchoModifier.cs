using System.Text.RegularExpressions;

namespace SomethingNeedDoing.Grammar.Modifiers;

/// <summary>
/// The &lt;echo&gt; modifier.
/// </summary>
internal class EchoModifier : MacroModifier
{
    private static readonly Regex Regex = new(@"(?<modifier><echo>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private EchoModifier(bool echo)
    {
        this.PerformEcho = echo;
    }

    /// <summary>
    /// Gets a value indicating whether to perform an echo.
    /// </summary>
    public bool PerformEcho { get; }

    /// <summary>
    /// Parse the text as a modifier.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="command">A parsed modifier.</param>
    /// <returns>A value indicating whether the modifier matched.</returns>
    public static bool TryParse(ref string text, out EchoModifier command)
    {
        var match = Regex.Match(text);
        var success = match.Success;

        if (success)
        {
            var group = match.Groups["modifier"];
            text = text.Remove(group.Index, group.Length);
        }

        command = new EchoModifier(success);

        return success;
    }
}
