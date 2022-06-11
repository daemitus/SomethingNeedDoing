using System.Collections.Generic;
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
/// The /require command.
/// </summary>
internal class RequireCommand : MacroCommand
{
    private const int StatusCheckMaxWait = 1000;
    private const int StatusCheckInterval = 250;

    private static readonly Regex Regex = new(@"^/require\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly uint[] statusIDs;
    private readonly int maxWait;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireCommand"/> class.
    /// </summary>
    /// <param name="text">Original text.</param>
    /// <param name="statusName">Status name.</param>
    /// <param name="wait">Wait value.</param>
    /// <param name="maxWait">MaxWait value.</param>
    private RequireCommand(string text, string statusName, WaitModifier wait, MaxWaitModifier maxWait)
        : base(text, wait)
    {
        statusName = statusName.ToLowerInvariant();
        var sheet = Service.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Status>()!;
        this.statusIDs = sheet
            .Where(row => row.Name.RawString.ToLowerInvariant() == statusName)
            .Select(row => row.RowId)
            .ToArray()!;

        this.maxWait = maxWait.Wait == 0
            ? StatusCheckMaxWait
            : maxWait.Wait;
    }

    /// <summary>
    /// Parse the text as a command.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>A parsed command.</returns>
    public static RequireCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);
        _ = MaxWaitModifier.TryParse(ref text, out var maxWaitModifier);

        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var nameValue = ExtractAndUnquote(match, "name");

        return new RequireCommand(text, nameValue, waitModifier, maxWaitModifier);
    }

    /// <inheritdoc/>
    public async override Task Execute(ActiveMacro macro, CancellationToken token)
    {
        PluginLog.Debug($"Executing: {this.Text}");

        bool IsStatusPresent() => CommandInterface.HasStatusId(this.statusIDs);

        var hasStatus = await this.LinearWait(StatusCheckInterval, this.maxWait, IsStatusPresent, token);

        if (!hasStatus)
            throw new MacroCommandError("Status effect not found");

        await this.PerformWait(token);
    }
}
