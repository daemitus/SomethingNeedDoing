using System.Collections.Generic;
using System.IO;

using SomethingNeedDoing.Grammar.Commands;

namespace SomethingNeedDoing.Grammar;

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
        var line = string.Empty;
        using var reader = new StringReader(macroText);

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            // Empty
            if (line.Length == 0)
                continue;

            // Comment
            if (line.StartsWith('#') || line.StartsWith("//"))
                continue;

            // Extract the slash command
            var firstSpace = line.IndexOf(' ');
            var commandText = firstSpace != -1
                ? line[..firstSpace]
                : line;

            commandText = commandText.ToLowerInvariant();
            yield return commandText switch
            {
                "/ac" => ActionCommand.Parse(line),
                "/action" => ActionCommand.Parse(line),
                "/click" => ClickCommand.Parse(line),
                "/craft" => GateCommand.Parse(line),
                "/gate" => GateCommand.Parse(line),
                "/loop" => LoopCommand.Parse(line),
                "/require" => RequireCommand.Parse(line),
                "/requirestats" => RequireStatsCommand.Parse(line),
                "/runmacro" => RunMacroCommand.Parse(line),
                "/send" => SendCommand.Parse(line),
                "/target" => TargetCommand.Parse(line),
                "/waitaddon" => WaitAddonCommand.Parse(line),
                "/wait" => WaitCommand.Parse(line),
                _ => NativeCommand.Parse(line),
            };
        }

        yield break;
    }
}
