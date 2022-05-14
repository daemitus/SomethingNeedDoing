using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;
using SomethingNeedDoing.Managers;

namespace SomethingNeedDoing.Grammar.Commands;

/// <summary>
/// The /requirerepair command.
/// </summary>
internal class RequireRepairCommand : MacroCommand
{
    private static readonly Regex Regex = new(@"^/requirerepair\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireRepairCommand"/> class.
    /// </summary>
    /// <param name="text">Original text.</param>
    /// <param name="wait">Wait value.</param>
    private RequireRepairCommand(string text, WaitModifier wait)
        : base(text, wait)
    {
    }

    /// <summary>
    /// Parse the text as a command.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>A parsed command.</returns>
    public static RequireRepairCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);

        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        return new RequireRepairCommand(text, waitModifier);
    }

    /// <inheritdoc/>
    public async override Task Execute(CancellationToken token)
    {
        PluginLog.Debug($"Executing: {this.Text}");

        if (this.ShouldRepair())
            throw new MacroPause("You need to repair", UiColor.Yellow);

        await this.PerformWait(token);
    }

    private unsafe bool ShouldRepair()
    {
        var im = InventoryManager.Instance();
        if (im == null)
        {
            PluginLog.Error("InventoryManager was null");
            return false;
        }

        var equipped = im->GetInventoryContainer(InventoryType.EquippedItems);
        if (equipped == null)
        {
            PluginLog.Error("InventoryContainer was null");
            return false;
        }

        if (equipped->Loaded == 0)
        {
            PluginLog.Error($"InventoryContainer is not loaded");
            return false;
        }

        for (var i = 0; i < equipped->Size; i++)
        {
            var item = equipped->GetInventorySlot(i);
            if (item == null)
                continue;

            if (item->Condition == 0)
                return true;
        }

        return false;
    }
}
