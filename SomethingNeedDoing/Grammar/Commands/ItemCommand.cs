using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;
using SomethingNeedDoing.Misc;

using Sheets = Lumina.Excel.GeneratedSheets;

namespace SomethingNeedDoing.Grammar.Commands;

/// <summary>
/// The /item command.
/// </summary>
internal class ItemCommand : MacroCommand
{
    private static readonly Regex Regex = new(@"^/item\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string itemName;
    private readonly ItemQualityModifier itemQualityMod;

    /// <summary>
    /// Initializes a new instance of the <see cref="ItemCommand"/> class.
    /// </summary>
    /// <param name="text">Original text.</param>
    /// <param name="itemName">Item name.</param>
    /// <param name="wait">Wait value.</param>
    /// <param name="itemQualityMod">Required quality of the item used.</param>
    private ItemCommand(string text, string itemName, WaitModifier wait, ItemQualityModifier itemQualityMod)
        : base(text, wait)
    {
        this.itemName = itemName.ToLowerInvariant();
        this.itemQualityMod = itemQualityMod;
    }

    /// <summary>
    /// Parse the text as a command.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>A parsed command.</returns>
    public static ItemCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);
        _ = ItemQualityModifier.TryParse(ref text, out var itemQualityModifier);

        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var nameValue = ExtractAndUnquote(match, "name");

        return new ItemCommand(text, nameValue, waitModifier, itemQualityModifier);
    }

    /// <inheritdoc/>
    public async override Task Execute(ActiveMacro macro, CancellationToken token)
    {
        PluginLog.Debug($"Executing: {this.Text}");

        var itemId = this.SearchItemId(this.itemName);
        PluginLog.Debug($"Item found: {itemId}");

        var count = this.GetInventoryItemCount(itemId, this.itemQualityMod.IsHq);
        PluginLog.Debug($"Item Count: {count}");
        if (count == 0)
            throw new MacroCommandError("You do not have that item");

        this.UseItem(itemId, this.itemQualityMod.IsHq);

        await this.PerformWait(token);
    }

    private unsafe void UseItem(uint itemID, bool isHQ = false)
    {
        var agent = AgentInventoryContext.Instance();
        if (agent == null)
            throw new MacroCommandError("AgentInventoryContext not found");

        if (isHQ)
            itemID += 1_000_000;

        var result = agent->UseItem(itemID);
        if (result != 0)
            throw new MacroCommandError("Failed to use item");
    }

    private unsafe int GetInventoryItemCount(uint itemID, bool isHQ)
    {
        var inventoryManager = InventoryManager.Instance();
        if (inventoryManager == null)
            throw new MacroCommandError("InventoryManager not found");

        return inventoryManager->GetInventoryItemCount(itemID, isHQ);
    }

    private uint SearchItemId(string itemName)
    {
        var sheet = Service.DataManager.GetExcelSheet<Sheets.Item>()!;
        var item = sheet.FirstOrDefault(r => r.Name.ToString().ToLowerInvariant() == itemName);
        if (item == null)
            throw new MacroCommandError("Item not found");

        return item.RowId;
    }
}
