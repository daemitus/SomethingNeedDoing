using System;
using System.Linq;
using System.Text.RegularExpressions;

using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace SomethingNeedDoing.Grammar.Modifiers
{
    /// <summary>
    /// The &lt;condition&gt; modifier.
    /// </summary>
    internal class ConditionModifier : MacroModifier
    {
        private static readonly Regex Regex = new(@"(?<modifier><condition\.(?<not>(not\.|\!))?(?<names>[a-zA-Z]+((,[a-zA-Z]+)+)?)>)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly string[] conditions;
        private readonly bool negated;

        private ConditionModifier(string[] conditions, bool negated)
        {
            this.conditions = conditions;
            this.negated = negated;
        }

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

                var conditionNames = match.Groups["names"].Value
                    .ToLowerInvariant().Split(",")
                    .Select(name => name.Trim())
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToArray();
                var negated = match.Groups["not"].Success;

                command = new ConditionModifier(conditionNames, negated);
            }
            else
            {
                command = new ConditionModifier(Array.Empty<string>(), false);
            }

            return success;
        }

        /// <summary>
        /// Check if the current crafting condition is active.
        /// </summary>
        /// <returns>A parsed command.</returns>
        public unsafe bool HasCondition()
        {
            if (this.conditions.Length == 0)
                return true;

            var addon = Service.GameGui.GetAddonByName("Synthesis", 1);
            if (addon == IntPtr.Zero)
            {
                PluginLog.Debug("Could not find Synthesis addon");
                return true;
            }

            var addonPtr = (AddonSynthesis*)addon;
            var text = addonPtr->Condition->NodeText.ToString().ToLowerInvariant();

            var matchesText = this.conditions.Any(name => name == text);

            if (this.negated)
                matchesText ^= true;

            return matchesText;
        }
    }
}
