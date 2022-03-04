using System.Text.RegularExpressions;

namespace SomethingNeedDoing.Grammar.Modifiers;

/// <summary>
/// The &lt;unsafe&gt; modifier.
/// </summary>
internal class UnsafeModifier : MacroModifier
{
    private static readonly Regex Regex = new(@"(?<modifier><unsafe>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private UnsafeModifier(bool isUnsafe)
    {
        this.IsUnsafe = isUnsafe;
    }

    /// <summary>
    /// Gets a value indicating whether the modifier was present.
    /// </summary>
    public bool IsUnsafe { get; }

    /// <summary>
    /// Parse the text as a modifier.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="command">A parsed modifier.</param>
    /// <returns>A value indicating whether the modifier matched.</returns>
    public static bool TryParse(ref string text, out UnsafeModifier command)
    {
        var match = Regex.Match(text);
        var success = match.Success;

        if (success)
        {
            var group = match.Groups["modifier"];
            text = text.Remove(group.Index, group.Length);
        }

        command = new UnsafeModifier(success);

        return success;
    }
}
