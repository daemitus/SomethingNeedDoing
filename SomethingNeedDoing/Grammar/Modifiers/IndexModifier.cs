using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SomethingNeedDoing.Grammar.Modifiers;

/// <summary>
/// The &lt;index&gt; modifier.
/// </summary>
internal class IndexModifier : MacroModifier
{
    private static readonly Regex Regex = new(@"(?<modifier><index\.(?<objectId>\d+(?:\.\d+)?)>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private IndexModifier(int objectId)
    {
        this.ObjectId = objectId;
    }

    /// <summary>
    /// Gets the objectIndex of the specified Target.
    /// </summary>
    public int ObjectId { get; }

    /// <summary>
    /// Parse the text as a modifier.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="command">A parsed modifier.</param>
    /// <returns>A value indicating whether the modifier matched.</returns>
    public static bool TryParse(ref string text, out IndexModifier command)
    {
        var match = Regex.Match(text);
        var success = match.Success;

        if (!success)
        {
            command = new IndexModifier(0);
            return false;
        }

        var group = match.Groups["modifier"];
        text = text.Remove(group.Index, group.Length);

        var indexGroup = match.Groups["objectId"];
        var indexValue = indexGroup.Value;
        var index = int.Parse(indexValue, CultureInfo.InvariantCulture);

        command = new IndexModifier(index);
        return true;
    }
}
