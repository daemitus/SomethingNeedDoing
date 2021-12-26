using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SomethingNeedDoing.Grammar.Modifiers
{
    /// <summary>
    /// The &lt;condition&gt; modifier.
    /// </summary>
    internal class ConditionModifier : MacroModifier
    {
        private static readonly Regex Regex = new(@"(?<modifier><condition\.(?<name>[a-zA-Z]+)>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private ConditionModifier(string condition)
        {
            this.Condition = condition;
        }

        /// <summary>
        /// Gets the required condition name.
        /// </summary>
        public string Condition { get; }

        /// <summary>
        /// Parse the text as a modifier.
        /// </summary>
        /// <param name="text">Text to parse.</param>
        /// <param name="command">A parsed modifier.</param>
        /// <returns>A value indicating whether the modifier matched.</returns>
        public static bool TryParse(ref string text, out ConditionModifier command)
        {
            var match = Regex.Match(text);
            var success = match.Success;

            if (success)
            {
                var group = match.Groups["modifier"];
                text = text.Remove(group.Index, group.Length);

                var conditionName = match.Groups["name"].Value.ToLowerInvariant();
                command = new ConditionModifier(conditionName);
                return true;
            }

            command = new ConditionModifier(string.Empty);
            return false;
        }
    }
}
