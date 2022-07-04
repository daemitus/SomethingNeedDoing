using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;
using SomethingNeedDoing.Misc;

namespace SomethingNeedDoing.Grammar.Commands;

/// <summary>
/// The /requirestats command.
/// </summary>
internal class RequireStatsCommand : MacroCommand
{
    private const int StatusCheckMaxWait = 1000;
    private const int StatusCheckInterval = 250;

    private static readonly Regex Regex = new(@"^/requirestats\s+(?<craftsmanship>\d+)\s+(?<control>\d+)\s+(?<cp>\d+)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly uint requiredCraftsmanship;
    private readonly uint requiredControl;
    private readonly uint requiredCp;
    private readonly int maxWait;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireStatsCommand"/> class.
    /// </summary>
    /// <param name="text">Original text.</param>
    /// <param name="craftsmanship">Craftsmanship value.</param>
    /// <param name="control">Control value.</param>
    /// <param name="cp">Cp value.</param>
    /// <param name="wait">Wait value.</param>
    /// <param name="maxWait">MaxWait value.</param>
    private RequireStatsCommand(string text, uint craftsmanship, uint control, uint cp, WaitModifier wait, MaxWaitModifier maxWait)
        : base(text, wait)
    {
        this.requiredCraftsmanship = craftsmanship;
        this.requiredControl = control;
        this.requiredCp = cp;

        this.maxWait = maxWait.Wait == 0
            ? StatusCheckMaxWait
            : maxWait.Wait;
    }

    /// <summary>
    /// Parse the text as a command.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>A parsed command.</returns>
    public static RequireStatsCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);
        _ = MaxWaitModifier.TryParse(ref text, out var maxWaitModifier);

        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var craftsmanshipValue = match.Groups["craftsmanship"].Value;
        var craftsmanship = uint.Parse(craftsmanshipValue, CultureInfo.InvariantCulture);

        var controlValue = match.Groups["control"].Value;
        var control = uint.Parse(controlValue, CultureInfo.InvariantCulture);

        var cpValue = match.Groups["cp"].Value;
        var cp = uint.Parse(cpValue, CultureInfo.InvariantCulture);

        return new RequireStatsCommand(text, craftsmanship, control, cp, waitModifier, maxWaitModifier);
    }

    /// <inheritdoc/>
    public async override Task Execute(ActiveMacro macro, CancellationToken token)
    {
        PluginLog.Debug($"Executing: {this.Text}");

        bool AreStatsGood() => CommandInterface.Instance.HasStats(this.requiredCraftsmanship, this.requiredControl, this.requiredCp);

        var hasStats = await this.LinearWait(StatusCheckInterval, this.maxWait, AreStatsGood, token);

        if (!hasStats)
            throw new MacroCommandError("Required stats were not found");

        await this.PerformWait(token);
    }
}
