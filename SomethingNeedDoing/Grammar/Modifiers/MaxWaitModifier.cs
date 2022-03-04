using System.Globalization;
using System.Text.RegularExpressions;

namespace SomethingNeedDoing.Grammar.Modifiers;

/// <summary>
/// The &lt;maxwait&gt; modifier.
/// </summary>
internal class MaxWaitModifier : MacroModifier
{
    private static readonly Regex Regex = new(@"(?<modifier><maxwait\.(?<wait>\d+(?:\.\d+)?)>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private MaxWaitModifier(int wait)
    {
        this.Wait = wait;
    }

    /// <summary>
    /// Gets the milliseconds to wait.
    /// </summary>
    public int Wait { get; }

    /// <summary>
    /// Parse the text as a modifier.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="command">A parsed modifier.</param>
    /// <returns>A value indicating whether the modifier matched.</returns>
    public static bool TryParse(ref string text, out MaxWaitModifier command)
    {
        var match = Regex.Match(text);
        var success = match.Success;

        if (success)
        {
            var group = match.Groups["modifier"];
            text = text.Remove(group.Index, group.Length);

            var waitGroup = match.Groups["wait"];
            var waitValue = waitGroup.Value;
            var wait = (int)(float.Parse(waitValue, CultureInfo.InvariantCulture) * 1000);

            command = new MaxWaitModifier(wait);
        }
        else
        {
            command = new MaxWaitModifier(0);
        }

        return success;
    }
}
