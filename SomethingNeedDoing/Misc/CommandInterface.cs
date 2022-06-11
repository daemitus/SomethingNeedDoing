using System;
using System.Linq;

using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SomethingNeedDoing.Exceptions;

namespace SomethingNeedDoing.Misc;

/// <summary>
/// Miscellaneous functions that commands/scripts can use.
/// </summary>
public static class CommandInterface
{
    /// <summary>
    /// Get a value indicating whether the player is crafting.
    /// </summary>
    /// <returns>True or false.</returns>
    public static unsafe bool IsCrafting()
        => Service.Condition[ConditionFlag.Crafting] && !Service.Condition[ConditionFlag.PreparingToCraft];

    /// <summary>
    /// Get a value indicating whether the player is not crafting.
    /// </summary>
    /// <returns>True or false.</returns>
    public static unsafe bool IsNotCrafting()
        => !IsCrafting();

    /// <summary>
    /// Get a value indicating whether the current craft is collectable.
    /// </summary>
    /// <returns>A value indicating whether the current craft is collectable.</returns>
    public static unsafe bool IsCollectable()
    {
        var addon = GetSynthesisAddon();

        return addon->AtkUnitBase.UldManager.NodeList[34]->IsVisible;
    }

    /// <summary>
    /// Get the current synthesis condition.
    /// </summary>
    /// <param name="lower">A value indicating whether the result should be lowercased.</param>
    /// <returns>The current synthesis condition.</returns>
    public static unsafe string GetCondition(bool lower = true)
    {
        var addon = GetSynthesisAddon();

        var text = addon->Condition->NodeText.ToString();

        if (lower)
            text = text.ToLowerInvariant();

        return text;
    }

    /// <summary>
    /// Get a value indicating whether the current condition is active.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="lower">A value indicating whether the result should be lowercased.</param>
    /// <returns>A value indcating whether the current condition is active.</returns>
    public static bool HasCondition(string condition, bool lower = true)
    {
        var actual = GetCondition(lower);
        return condition == actual;
    }

    /// <summary>
    /// Get the current and max progress values.
    /// </summary>
    /// <returns>The current and max progress.</returns>
    public static unsafe (int Current, int Max) GetProgress()
    {
        return (GetCurrentProgress(), GetMaxProgress());
    }

    /// <summary>
    /// Get the current progress value.
    /// </summary>
    /// <returns>The current progress value.</returns>
    public static unsafe int GetCurrentProgress()
    {
        var addon = GetSynthesisAddon();
        return GetNodeTextAsInt(addon->CurrentProgress, "Could not parse current progress number in the Synthesis addon");
    }

    /// <summary>
    /// Get the max progress value.
    /// </summary>
    /// <returns>The max progress value.</returns>
    public static unsafe int GetMaxProgress()
    {
        var addon = GetSynthesisAddon();
        return GetNodeTextAsInt(addon->MaxProgress, "Could not parse max progress number in the Synthesis addon");
    }

    /// <summary>
    /// Get a value indicating whether max progress has been achieved.
    /// This is useful when a crafting animation is finishing.
    /// </summary>
    /// <returns>A value indicating whether max progress has been achieved.</returns>
    public static bool HasMaxProgress()
    {
        var (current, max) = GetProgress();
        return current == max;
    }

    /// <summary>
    /// Get the current and max quality values.
    /// </summary>
    /// <returns>The current and max quality.</returns>
    public static unsafe (int Current, int Max) GetQuality()
    {
        return (GetCurrentQuality(), GetMaxQuality());
    }

    /// <summary>
    /// Get the current quality value.
    /// </summary>
    /// <returns>The current quality value.</returns>
    public static unsafe int GetCurrentQuality()
    {
        var addon = GetSynthesisAddon();
        return GetNodeTextAsInt(addon->CurrentQuality, "Could not parse current quality number in the Synthesis addon");
    }

    /// <summary>
    /// Get the max quality value.
    /// </summary>
    /// <returns>The max quality value.</returns>
    public static unsafe int GetMaxQuality()
    {
        var addon = GetSynthesisAddon();
        return GetNodeTextAsInt(addon->MaxQuality, "Could not parse max quality number in the Synthesis addon");
    }

    /// <summary>
    /// Get a value indicating whether max quality has been achieved.
    /// </summary>
    /// <returns>A value indicating whether max quality has been achieved.</returns>
    public static bool HasMaxQuality()
    {
        var step = GetStep();

        if (step <= 1)
            return false;

        if (IsCollectable())
        {
            var (current, max) = GetQuality();
            return current == max;
        }
        else
        {
            var percentHq = GetPercentHQ();
            return percentHq == 100;
        }
    }

    /// <summary>
    /// Get the current step value.
    /// </summary>
    /// <returns>The current step value.</returns>
    public static unsafe int GetStep()
    {
        var addon = GetSynthesisAddon();

        var step = GetNodeTextAsInt(addon->StepNumber, "Could not parse current step number in the Synthesis addon");

        return step;
    }

    /// <summary>
    /// Get the current percent HQ (and collectable) value.
    /// </summary>
    /// <returns>The current percent HQ value.</returns>
    public static unsafe int GetPercentHQ()
    {
        var addon = GetSynthesisAddon();

        var step = GetNodeTextAsInt(addon->HQPercentage, "Could not parse percent hq number in the Synthesis addon");

        return step;
    }

    /// <summary>
    /// Gets a value indicating whether any of the player's worn equipment is broken.
    /// </summary>
    /// <returns>A value indicating whether any of the player's worn equipment is broken.</returns>
    public static unsafe bool NeedsRepair()
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

    /// <summary>
    /// Gets a value indicating whether any of the player's worn equipment can have materia extracted.
    /// </summary>
    /// <param name="within">Return false if the next highest spiritbond is >= this value.</param>
    /// <returns>A value indicating whether any of the player's worn equipment can have materia extracted.</returns>
    public static unsafe bool CanExtractMateria(float within = 100)
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
            if (within == 100)
            {
                PluginLog.Debug("An item is spiritbound, pausing");
                return true;
            }

            // Keep going if the next highest spiritbonded item is within the allowed range
            // i.e. 100 and 99, do another craft to finish the 99.
            if (nextHighest >= within)
            {
                PluginLog.Debug($"The next highest spiritbond is above ({nextHighest} >= {within}), keep going");
                return false;
            }
            else
            {
                PluginLog.Debug($"The next highest spiritbond is below ({nextHighest} < {within}), pausing");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets a value indicating whether the required crafting stats have been met.
    /// </summary>
    /// <param name="craftsmanship">Craftsmanship.</param>
    /// <param name="control">Control.</param>
    /// <param name="cp">Crafting points.</param>
    /// <returns>A value indcating whether the required crafting stats bave been met.</returns>
    public static unsafe bool HasStats(uint craftsmanship, uint control, uint cp)
    {
        var uiState = UIState.Instance();
        if (uiState == null)
        {
            PluginLog.Error($"UIState is null");
            return false;
        }

        var hasStats =
            uiState->PlayerState.Attributes[70] >= craftsmanship &&
            uiState->PlayerState.Attributes[71] >= control &&
            uiState->PlayerState.Attributes[11] >= cp;

        return hasStats;
    }

    /// <summary>
    /// Gets a value indicating whether the given status is present on the player.
    /// </summary>
    /// <param name="statusName">Status name.</param>
    /// <returns>A value indicating whether the given status is present on the player.</returns>
    public static unsafe bool HasStatus(string statusName)
    {
        statusName = statusName.ToLowerInvariant();
        var sheet = Service.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Status>()!;
        var statusIDs = sheet
            .Where(row => row.Name.RawString.ToLowerInvariant() == statusName)
            .Select(row => row.RowId)
            .ToArray()!;

        return HasStatusId(statusIDs);
    }

    /// <summary>
    /// Gets a value indicating whether the given status is present on the player.
    /// </summary>
    /// <param name="statusIDs">Status IDs.</param>
    /// <returns>A value indicating whether the given status is present on the player.</returns>
    public static unsafe bool HasStatusId(params uint[] statusIDs)
    {
        var statusID = Service.ClientState.LocalPlayer!.StatusList
            .Select(se => se.StatusId)
            .ToList().Intersect(statusIDs)
            .FirstOrDefault();

        return statusID != default;
    }

    private static unsafe int GetNodeTextAsInt(AtkTextNode* node, string error)
    {
        try
        {
            if (node == null)
                throw new NullReferenceException("TextNode is null");

            var text = node->NodeText.ToString();
            var value = int.Parse(text);
            return value;
        }
        catch (Exception ex)
        {
            throw new MacroCommandError(error, ex);
        }
    }

    /// <summary>
    /// Get a pointer to the Synthesis addon.
    /// </summary>
    /// <returns>A valid pointer or throw.</returns>
    private static unsafe AddonSynthesis* GetSynthesisAddon()
    {
        var ptr = Service.GameGui.GetAddonByName("Synthesis", 1);
        if (ptr == IntPtr.Zero)
            throw new MacroCommandError("Could not find Synthesis addon");

        return (AddonSynthesis*)ptr;
    }
}
