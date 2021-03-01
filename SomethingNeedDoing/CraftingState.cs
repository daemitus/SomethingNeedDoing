using System.Runtime.InteropServices;

namespace SomethingNeedDoing
{
    public enum ActionCategory : int
    {
        CraftAction = 9,
        Action = 10,
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x90)]
    internal struct CraftingData
    {
        [FieldOffset(0x58)] public int ActionID;
        // [FieldOffset(0x5B)] public int unk5B;
        [FieldOffset(0x60)] public int CurrentStep;
        [FieldOffset(0x64)] public int CurrentProgress;
        [FieldOffset(0x68)] public int ProgressIncrease;
        [FieldOffset(0x6B)] public int CurrentQuality;
        [FieldOffset(0x70)] public int QualityIncrease;
        [FieldOffset(0x74)] public int PercentHQ;
        [FieldOffset(0x78)] public int CurrentDurability;
        // [FieldOffset(0x7B)] public int unk7B;
        [FieldOffset(0x80)] public CraftingCondition CurrentCondition;
        [FieldOffset(0x84)] public CraftingCondition PreviousCondition;
        [FieldOffset(0x88)] public ActionResult Result;
    }

    /*
    It's important to remember that the state in memory will be 0 when you haven't crafted yet
    or the last state of the previous craft. The first state is always normal when starting out
    so don't rely on the condition from memory on the first step. When failing a craft the actual
    failure state is only preset for a short time before the values are all reset to 0.
     */

    internal enum CraftingCondition : int
    {
        NONE = 0,
        NORMAL = 1,
        GOOD = 2,
        EXCELLENT = 3,
        POOR = 4,
        CENTERED = 5,
        STURDY = 6,
        PLIANT = 7,
        MALLEABLE = 8,
        PRIMED = 9,
    }

    internal enum ActionResult : short
    {
        NONE = 0,
        ActionFailure = 2,
        ActionSuccess = 18,
        CraftSuccess = 22,
        CraftFailure = 26,
    }
}
