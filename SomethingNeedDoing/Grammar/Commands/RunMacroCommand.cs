using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;

namespace SomethingNeedDoing.Grammar.Commands;

/// <summary>
/// The /runmacro command.
/// </summary>
internal class RunMacroCommand : MacroCommand
{
    private static readonly Regex Regex = new(@"^/runmacro\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string macroName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RunMacroCommand"/> class.
    /// </summary>
    /// <param name="text">Original text.</param>
    /// <param name="macroName">Macro name.</param>
    /// <param name="wait">Wait value.</param>
    private RunMacroCommand(string text, string macroName, WaitModifier wait)
        : base(text, wait)
    {
        this.macroName = macroName;
    }

    /// <summary>
    /// Parse the text as a command.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>A parsed command.</returns>
    public static RunMacroCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);

        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var nameValue = ExtractAndUnquote(match, "name");

        return new RunMacroCommand(text, nameValue, waitModifier);
    }

    /// <inheritdoc/>
    public async override Task Execute(CancellationToken token)
    {
        PluginLog.Debug($"Executing: {this.Text}");

        var macroNode = Service.Configuration
            .GetAllNodes().OfType<MacroNode>()
            .FirstOrDefault(macro => macro.Name == this.macroName);

        if (macroNode == default)
            throw new MacroCommandError("No macro with that name");

        Service.MacroManager.EnqueueMacro(macroNode);

        await this.PerformWait(token);
    }
}
