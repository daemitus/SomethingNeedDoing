namespace SomethingNeedDoing.Misc;

/// <summary>
/// Miscellaneous functions that commands/scripts can use.
/// </summary>
public interface ICommandInterface
{
    /// <summary>
    /// Get a value indicating whether the player is crafting.
    /// </summary>
    /// <returns>True or false.</returns>
    public bool IsCrafting();

    /// <summary>
    /// Get a value indicating whether the player is not crafting.
    /// </summary>
    /// <returns>True or false.</returns>
    public bool IsNotCrafting();

    /// <summary>
    /// Get a value indicating whether the current craft is collectable.
    /// </summary>
    /// <returns>A value indicating whether the current craft is collectable.</returns>
    public bool IsCollectable();

    /// <summary>
    /// Get the current synthesis condition.
    /// </summary>
    /// <param name="lower">A value indicating whether the result should be lowercased.</param>
    /// <returns>The current synthesis condition.</returns>
    public string GetCondition(bool lower = true);

    /// <summary>
    /// Get a value indicating whether the current condition is active.
    /// </summary>
    /// <param name="condition">The condition to check.</param>
    /// <param name="lower">A value indicating whether the result should be lowercased.</param>
    /// <returns>A value indcating whether the current condition is active.</returns>
    public bool HasCondition(string condition, bool lower = true);

    /// <summary>
    /// Get the current progress value.
    /// </summary>
    /// <returns>The current progress value.</returns>
    public int GetProgress();

    /// <summary>
    /// Get the max progress value.
    /// </summary>
    /// <returns>The max progress value.</returns>
    public int GetMaxProgress();

    /// <summary>
    /// Get a value indicating whether max progress has been achieved.
    /// This is useful when a crafting animation is finishing.
    /// </summary>
    /// <returns>A value indicating whether max progress has been achieved.</returns>
    public bool HasMaxProgress();

    /// <summary>
    /// Get the current quality value.
    /// </summary>
    /// <returns>The current quality value.</returns>
    public int GetQuality();

    /// <summary>
    /// Get the max quality value.
    /// </summary>
    /// <returns>The max quality value.</returns>
    public int GetMaxQuality();

    /// <summary>
    /// Get a value indicating whether max quality has been achieved.
    /// </summary>
    /// <returns>A value indicating whether max quality has been achieved.</returns>
    public bool HasMaxQuality();

    /// <summary>
    /// Get the current durability value.
    /// </summary>
    /// <returns>The current durability value.</returns>
    public int GetDurability();

    /// <summary>
    /// Get the max durability value.
    /// </summary>
    /// <returns>The max durability value.</returns>
    public int GetMaxDurability();

    /// <summary>
    /// Get the current CP amount.
    /// </summary>
    /// <returns>The current CP amount.</returns>
    public int GetCp();

    /// <summary>
    /// Get the max CP amount.
    /// </summary>
    /// <returns>The max CP amount.</returns>
    public int GetMaxCp();

    /// <summary>
    /// Get the current step value.
    /// </summary>
    /// <returns>The current step value.</returns>
    public int GetStep();

    /// <summary>
    /// Get the current percent HQ (and collectable) value.
    /// </summary>
    /// <returns>The current percent HQ value.</returns>
    public int GetPercentHQ();

    /// <summary>
    /// Gets a value indicating whether any of the player's worn equipment is broken.
    /// </summary>
    /// <param name="below">Return true if the gear durability is less than or eqal to this number.</param>
    /// <returns>A value indicating whether any of the player's worn equipment is broken.</returns>
    public bool NeedsRepair(float below = 0);

    /// <summary>
    /// Gets a value indicating whether any of the player's worn equipment can have materia extracted.
    /// </summary>
    /// <param name="within">Return false if the next highest spiritbond is >= this value.</param>
    /// <returns>A value indicating whether any of the player's worn equipment can have materia extracted.</returns>
    public bool CanExtractMateria(float within = 100);

    /// <summary>
    /// Gets a value indicating whether the required crafting stats have been met.
    /// </summary>
    /// <param name="craftsmanship">Craftsmanship.</param>
    /// <param name="control">Control.</param>
    /// <param name="cp">Crafting points.</param>
    /// <returns>A value indcating whether the required crafting stats bave been met.</returns>
    public bool HasStats(uint craftsmanship, uint control, uint cp);

    /// <summary>
    /// Gets a value indicating whether the given status is present on the player.
    /// </summary>
    /// <param name="statusName">Status name.</param>
    /// <returns>A value indicating whether the given status is present on the player.</returns>
    public bool HasStatus(string statusName);

    /// <summary>
    /// Gets a value indicating whether the given status is present on the player.
    /// </summary>
    /// <param name="statusIDs">Status IDs.</param>
    /// <returns>A value indicating whether the given status is present on the player.</returns>
    public bool HasStatusId(params uint[] statusIDs);

    /// <summary>
    /// Gets a value indicating whether an addon is visible.
    /// </summary>
    /// <param name="addonName">Addon name.</param>
    /// <returns>A value indicating whether an addon is visible.</returns>
    public bool IsAddonVisible(string addonName);

    /// <summary>
    /// Gets a value indicating whether an addon is ready to be used. It may not be visible.
    /// </summary>
    /// <param name="addonName">Addon name.</param>
    /// <returns>A value indicating whether an addon is ready to be used.</returns>
    public bool IsAddonReady(string addonName);

    /// <summary>
    /// Get the text of a TextNode by its index number. You can find this by using the addon inspector.
    /// In general, these numbers do not change.
    /// </summary>
    /// <param name="addonName">Addon name.</param>
    /// <param name="nodeNumbers">Node numbers, can fetch nested nodes.</param>
    /// <returns>The node text.</returns>
    public string GetNodeText(string addonName, params int[] nodeNumbers);

    /// <summary>
    /// Get the text of a 0-indexed SelectIconString entry.
    /// </summary>
    /// <param name="index">Item number, 0 indexed.</param>
    /// <returns>The item text, or an empty string.</returns>
    public string GetSelectStringText(int index);

    /// <summary>
    /// Get the text of a 0-indexed SelectIconString entry.
    /// </summary>
    /// <param name="index">Item number, 0 indexed.</param>
    /// <returns>The item text, or an empty string.</returns>
    public string GetSelectIconStringText(int index);
}
