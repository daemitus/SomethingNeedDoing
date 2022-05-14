using System;
using System.Globalization;
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
/// The /requiremateria command.
/// </summary>
internal class RequireSpiritbondCommand : MacroCommand
{
    private static readonly Regex Regex = new(@"^/requirespiritbond(\s+(?<within>\d+(?:\.\d+)?))?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly float within;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireSpiritbondCommand"/> class.
    /// </summary>
    /// <param name="text">Original text.</param>
    /// <param name="within">Check if other items are within a certain percentage.</param>
    /// <param name="wait">Wait value.</param>
    private RequireSpiritbondCommand(string text, float within, WaitModifier wait)
        : base(text, wait)
    {
        this.within = within;
    }

    /// <summary>
    /// Parse the text as a command.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <returns>A parsed command.</returns>
    public static RequireSpiritbondCommand Parse(string text)
    {
        _ = WaitModifier.TryParse(ref text, out var waitModifier);

        var match = Regex.Match(text);
        if (!match.Success)
            throw new MacroSyntaxError(text);

        var withinGroup = match.Groups["within"];
        var withinValue = withinGroup.Success ? withinGroup.Value : "0";
        var within = float.Parse(withinValue, CultureInfo.InvariantCulture);

        return new RequireSpiritbondCommand(text, within, waitModifier);
    }

    /// <inheritdoc/>
    public async override Task Execute(CancellationToken token)
    {
        PluginLog.Debug($"Executing: {this.Text}");

        if (this.CanExtractMateria())
            throw new MacroPause("You can extract materia now", UiColor.Green);

        await this.PerformWait(token);
    }

    private unsafe bool CanExtractMateria()
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

        var nextHighest = 0f;
        var canExtract = false;
        var allExtract = true;
        for (var i = 0; i < equipped->Size; i++)
        {
            var item = equipped->GetInventorySlot(i);
            if (item == null)
                continue;

            var spiritbond = item->Spiritbond / 100;
            if (spiritbond == 100f)
            {
                canExtract = true;
            }
            else
            {
                allExtract = false;
                nextHighest = Math.Max(spiritbond, nextHighest);
            }
        }

        if (allExtract)
        {
            PluginLog.Debug("All items are spiritbound, pausing");
            return true;
        }

        if (canExtract)
        {
            // Don't wait, extract immediately
            if (this.within == 0)
            {
                PluginLog.Debug("An item is spiritbound, pausing");
                return true;
            }

            // Keep going if the next highest spiritbonded item is within the allowed range
            // i.e. 100 and 99, do another craft to finish the 99.
            if (nextHighest >= this.within)
            {
                PluginLog.Debug($"The next highest spiritbond is above ({nextHighest} >= {this.within}), keep going");
                return false;
            }
            else
            {
                PluginLog.Debug($"The next highest spiritbond is below ({nextHighest} < {this.within}), pausing");
                return true;
            }
        }

        return false;
    }
}
