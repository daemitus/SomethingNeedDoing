using System.Text.RegularExpressions;

namespace SomethingNeedDoing.Grammar.Modifiers
{
    /// <summary>
    /// The &lt;unsafe&gt; modifier.
    /// </summary>
    internal class UnsafeModifier : MacroModifier
    {
        private static readonly Regex Regex = new(@"(?<modifier><unsafe>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private UnsafeModifier()
        {
        }

        /// <summary>
        /// Parse the text as a modifier.
        /// </summary>
        /// <param name="text">Text to parse.</param>
        /// <param name="command">A parsed modifier.</param>
        /// <returns>A value indicating whether the modifier matched.</returns>
        public static bool TryParse(ref string text, out UnsafeModifier command)
        {
            var match = Regex.Match(text);
            if (!match.Success)
            {
                command = new UnsafeModifier();
                return false;
            }

            var group = match.Groups["modifier"];
            text = text.Remove(group.Index, group.Length);

            command = new UnsafeModifier();
            return true;
        }
    }
}
