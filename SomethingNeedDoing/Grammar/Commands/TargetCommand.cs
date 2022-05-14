using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;

namespace SomethingNeedDoing.Grammar.Commands;

/// <summary>
/// The /target command.
/// </summary>
internal class TargetCommand : MacroCommand
{
    private static readonly Regex Regex = new(@"^/target\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string targetName;

    /// <summary>
    /// Initializes a new instance of the <see cref="TargetCommand"/> class.
    /// </summary>
    /// <param name="text">Original text.</param>
    /// <param name="targetName">Target name.</param>
    /// <param name="wait">Wait value.</param>
    private TargetCommand(string text, string targetName, WaitModifier wait)
        : base(text, wait)
    {
        this.targetName = targetName.ToLowerInvariant();
    }

    /// <summary>
    /// Parse the text as a command.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>A parsed command.</returns>
    public static TargetCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);

        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var nameValue = ExtractAndUnquote(match, "name");

        return new TargetCommand(text, nameValue, waitModifier);
    }

    /// <inheritdoc/>
    public async override Task Execute(CancellationToken token)
    {
        PluginLog.Debug($"Executing: {this.Text}");

        var target = Service.ObjectTable.FirstOrDefault(obj => obj.Name.TextValue.ToLowerInvariant() == this.targetName);

        if (target == default)
            throw new MacroCommandError("Could not find target");

        Service.TargetManager.SetTarget(target);

        await this.PerformWait(token);
    }
}
