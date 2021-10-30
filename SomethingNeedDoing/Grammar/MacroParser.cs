using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

using Dalamud.Logging;
using Eto.Parse;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.MacroCommands;

[assembly: InternalsVisibleTo("Test")]

namespace SomethingNeedDoing.Grammar
{
    /// <summary>
    /// A macro parser.
    /// </summary>
    internal static class MacroParser
    {
        /// <summary>
        /// Parse a macro and return a series of executable statements.
        /// </summary>
        /// <param name="macroText">Macro to parse.</param>
        /// <returns>A series of executable statements.</returns>
        public static IEnumerable<MacroCommand> Parse(string macroText)
        {
            // The current grammar does not handle an empty line.
            while (macroText.Contains("\n\n"))
                macroText = macroText.Replace("\n\n", "\n \n");

            var result = MacroGrammar.Definition.Match(macroText);
            if (!result.Success)
                throw new MacroSyntaxError(result.ErrorMessage, result.ErrorLine, result.ErrorIndex);

            foreach (var match in result.Matches)
            {
                var original = match.Text;
                switch (match.Name)
                {
                    case "actionCommand":
                        {
                            var name = FindSubMatch(match, "actionName")!.Text;
                            var (wait, until) = ExtractWaitModifier(match, ref original);
                            var safely = !ExtractUnsafeModifier(match, ref original);
                            yield return new ActionCommand(original, name, wait, until, safely);
                            break;
                        }

                    case "clickCommand":
                        {
                            var name = FindSubMatch(match, "clickName")!.Text;
                            var (wait, until) = ExtractWaitModifier(match, ref original);
                            yield return new ClickCommand(original, name, wait, until);
                            break;
                        }

                    case "loopCommand":
                        {
                            var count = ParseInt(FindSubMatch(match, "loopCount"));
                            var (wait, until) = ExtractWaitModifier(match, ref original);
                            yield return new LoopCommand(original, count, wait, until);
                            break;
                        }

                    case "requireCommand":
                        {
                            var name = FindSubMatch(match, "requireName")!.Text;
                            var (wait, until) = ExtractWaitModifier(match, ref original);
                            var maxwait = ExtractMaxWaitModifier(match, ref original);
                            yield return new RequireCommand(original, name, wait, until, maxwait);
                            break;
                        }

                    case "runMacroCommand":
                        {
                            var name = FindSubMatch(match, "macroName")!.Text;
                            var (wait, until) = ExtractWaitModifier(match, ref original);
                            yield return new RunMacroCommand(original, name, wait, until);
                            break;
                        }

                    case "sendCommand":
                        {
                            var name = FindSubMatch(match, "sendName")!.Text;
                            var (wait, until) = ExtractWaitModifier(match, ref original);
                            yield return new SendCommand(original, name, wait, until);
                            break;
                        }

                    case "targetCommand":
                        {
                            var name = FindSubMatch(match, "targetName")!.Text;
                            var (wait, until) = ExtractWaitModifier(match, ref original);
                            yield return new TargetCommand(original, name, wait, until);
                            break;
                        }

                    case "waitCommand":
                        {
                            var wait = (int)(ParseFloat(FindSubMatch(match, "wait")) * 1000);
                            var until = (int)(ParseFloat(FindSubMatch(match, "waitUntil")) * 1000);
                            yield return new WaitCommand(original, wait, until);
                            break;
                        }

                    case "waitAddonCommand":
                        {
                            var name = FindSubMatch(match, "addonName")!.Text;
                            var (wait, until) = ExtractWaitModifier(match, ref original);
                            var maxwait = ExtractMaxWaitModifier(match, ref original);
                            yield return new WaitAddonCommand(original, name, wait, until, maxwait);
                            break;
                        }

                    case "comment":
                        {
                            break;
                        }

                    case "nativeCommand":
                        {
                            var text = FindSubMatch(match, "text")!.Text;
                            var wait = (int)(ParseFloat(FindSubMatch(match, "wait")) * 1000);
                            var until = (int)(ParseFloat(FindSubMatch(match, "waitUntil")) * 1000);
                            yield return new NativeCommand(text, wait, until);
                            break;
                        }

                    default:
                        throw new ArgumentException($"Unknown parser name: {match.Name}");
                }
            }

            yield break;
        }

        private static Match? FindSubMatch(Match match, string name)
                => match!.Matches.FirstOrDefault(m => m.Name == name);

        private static (int Wait, int Until) ExtractWaitModifier(Match match, ref string original)
        {
            var modifier = FindSubMatch(match, "waitModifier");
            if (modifier == default)
                return (0, 0);

            original = original.Replace(modifier.Text, string.Empty);

            var wait = (int)(ParseFloat(FindSubMatch(modifier, "wait")) * 1000);
            var until = (int)(ParseFloat(FindSubMatch(modifier, "waitUntil")) * 1000);
            return (wait, until);
        }

        private static int ExtractMaxWaitModifier(Match match, ref string original)
        {
            var modifier = FindSubMatch(match, "maxWaitModifier");
            if (modifier == default)
                return 0;

            original = original.Replace(modifier.Text, string.Empty);

            return (int)(ParseFloat(FindSubMatch(modifier, "maxWait")) * 1000);
        }

        private static bool ExtractUnsafeModifier(Match match, ref string original)
        {
            var modifier = FindSubMatch(match, "unsafeModifier");
            if (modifier == default)
                return false;

            original = original.Replace(modifier.Text, string.Empty);

            return true;
        }

        private static int ParseInt(Match? match, int defaultValue = 0)
            => match == default ? defaultValue : int.Parse(match.Text, CultureInfo.InvariantCulture);

        private static float ParseFloat(Match? match, float defaultValue = 0)
            => match == default ? defaultValue : float.Parse(match.Text, NumberStyles.Any, CultureInfo.InvariantCulture);
    }
}