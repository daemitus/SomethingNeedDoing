using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SomethingNeedDoing.Grammar.Modifiers
{
    /// <summary>
    /// The &lt;wait&gt; modifier.
    /// </summary>
    internal class WaitModifier : MacroModifier
    {
        private static readonly Regex Regex = new(@"(?<modifier><wait\.(?<wait>\d+(?:\.\d+)?)(?:-(?<until>\d+(?:\.\d+)?))?>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private WaitModifier(int wait, int until)
        {
            this.Wait = wait;
            this.Until = until;
        }

        /// <summary>
        /// Gets the milliseconds to wait.
        /// </summary>
        public int Wait { get; }

        /// <summary>
        /// Gets the milliseconds to wait until.
        /// </summary>
        public int Until { get; }

        /// <summary>
        /// Parse the text as a modifier.
        /// </summary>
        /// <param name="text">Text to parse.</param>
        /// <param name="command">A parsed modifier.</param>
        /// <returns>A value indicating whether the modifier matched.</returns>
        public static bool TryParse(ref string text, out WaitModifier command)
        {
            var match = Regex.Match(text);
            var success = match.Success;

            if (!success)
            {
                command = new WaitModifier(0, 0);
                return false;
            }

            var group = match.Groups["modifier"];
            text = text.Remove(group.Index, group.Length);

            var waitGroup = match.Groups["wait"];
            var waitValue = waitGroup.Value;
            var wait = (int)(float.Parse(waitValue, CultureInfo.InvariantCulture) * 1000);

            var untilGroup = match.Groups["until"];
            var untilValue = untilGroup.Success ? untilGroup.Value : "0";
            var until = (int)(float.Parse(untilValue, CultureInfo.InvariantCulture) * 1000);

            if (wait > until && until > 0)
                throw new ArgumentException("Until value cannot be lower than the wait value");

            command = new WaitModifier(wait, until);
            return true;
        }
    }
}
