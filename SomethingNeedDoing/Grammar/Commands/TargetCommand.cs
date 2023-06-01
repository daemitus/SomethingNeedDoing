using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;
using SomethingNeedDoing.Misc;

namespace SomethingNeedDoing.Grammar.Commands;

/// <summary>
/// The /target command.
/// </summary>
internal class TargetCommand : MacroCommand
{
    private static readonly Regex Regex = new(@"^/target\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string targetName;
    private readonly int targetIndex;

    /// <summary>
    /// Initializes a new instance of the <see cref="TargetCommand"/> class.
    /// </summary>
    /// <param name="text">Original text.</param>
    /// <param name="targetName">Target name.</param>
    /// <param name="wait">Wait value.</param>
    /// <param name="index">Object index.</param>
    private TargetCommand(string text, string targetName, WaitModifier wait, IndexModifier index)
        : base(text, wait, index)
    {
        this.targetIndex = index.ObjectId;
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
        _ = IndexModifier.TryParse(ref text, out var indexModifier);
        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var nameValue = ExtractAndUnquote(match, "name");
        return new TargetCommand(text, nameValue, waitModifier, indexModifier);
    }

    /// <inheritdoc/>
    public override async Task Execute(ActiveMacro macro, CancellationToken token)
    {
        PluginLog.Debug($"Executing: {this.targetIndex}");

        var target = Service.ObjectTable.FirstOrDefault(obj => obj.Name.TextValue.ToLowerInvariant() == this.targetName &&
                                                               (this.targetIndex <= 0 || obj.ObjectIndex == this.targetIndex));

        if (target == default)
            throw new MacroCommandError("Could not find target");

        Service.TargetManager.SetTarget(target);

        await this.PerformWait(token);
    }
}
