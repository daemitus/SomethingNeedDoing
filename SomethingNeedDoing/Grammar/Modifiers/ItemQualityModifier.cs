using System.Text.RegularExpressions;

namespace SomethingNeedDoing.Grammar.Modifiers;

/// <summary>
/// The &lt;itemquality&gt; modifier.
/// </summary>
internal class ItemQualityModifier : MacroModifier
{
    private static readonly Regex Regex = new(@"(?<modifier><hq>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private ItemQualityModifier(bool isHQ)
    {
        this.IsHq = isHQ;
    }

    /// <summary>
    /// Gets a value indicating whether the hq item is used.
    /// </summary>
    public bool IsHq { get; }

    /// <summary>
    /// Parse the text as a modifier.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="command">A parsed modifier.</param>
    /// <returns>A value indicating whether the modifier matched.</returns>
    public static bool TryParse(ref string text, out ItemQualityModifier command)
    {
        var match = Regex.Match(text);
        var success = match.Success;

        if (success)
        {
            var group = match.Groups["modifier"];
            text = text.Remove(group.Index, group.Length);
        }

        command = new ItemQualityModifier(success);

        return success;
    }
}
