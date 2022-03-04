using System;

namespace SomethingNeedDoing.CraftingData;

/// <summary>
/// Event action result types.
/// </summary>
[Flags]
internal enum ActionResultFlags : ushort
{
    // Unk0 = 1 << 0,
    // NotStep1 = 1 << 1,
    // CraftingSuccess = 1 << 2,
    // CraftingFailure = 1 << 3,
    // ActionSuccess = 1 << 4,
}
