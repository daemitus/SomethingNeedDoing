using System;
using System.Runtime.InteropServices;

namespace SomethingNeedDoing
{
    [StructLayout(LayoutKind.Explicit, Size = 0x64)]
    internal struct CraftingData
    {
        [FieldOffset(0x00)] public ActionCategory ActionCategory;
        // [FieldOffset(0x04)] public int Unk04;
        // [FieldOffset(0x08)] public int Unk08;
        // [FieldOffset(0x0C)] public int Unk0C;
        [FieldOffset(0x10)] public int ActionID;
        // [FieldOffset(0x14)] public int Unk14;
        [FieldOffset(0x18)] public int CurrentStep;
        [FieldOffset(0x1C)] public int CurrentProgress;
        [FieldOffset(0x20)] public int ProgressIncrease;
        [FieldOffset(0x24)] public int CurrentQuality;
        [FieldOffset(0x28)] public int QualityIncrease;
        [FieldOffset(0x2C)] public ushort PercentHQ;
        // [FieldOffset(0x2E)] public ushort Unk2E;
        [FieldOffset(0x30)] public int CurrentDurability;
        [FieldOffset(0x34)] public int DurabilityDelta;
        [FieldOffset(0x38)] public CraftingCondition CurrentCondition;
        [FieldOffset(0x3C)] public CraftingCondition PreviousCondition;
        [FieldOffset(0x40)] public ActionResult Result;
        [FieldOffset(0x40)] private readonly ActionResultFlags ResultFlags;
        // [FieldOffset(0x42)] public ushort Unk42;
        // [FieldOffset(0x44)] public int Unk44;
        // [FieldOffset(0x48)] public int Unk48;
        // [FieldOffset(0x4C)] public int Unk4C;
        // [FieldOffset(0x50)] public int Unk50;
        // [FieldOffset(0x54)] public int Unk54;
        // [FieldOffset(0x58)] public int Unk58;
        // [FieldOffset(0x5C)] public int Unk5C;
        // [FieldOffset(0x60)] public int Unk60;

        public bool Flag0 => ResultFlags.HasFlag(ActionResultFlags.Unk0);
        public bool Step1 => !ResultFlags.HasFlag(ActionResultFlags.NotStep1);
        public bool CraftingSuccess => ResultFlags.HasFlag(ActionResultFlags.CraftingSuccess);
        public bool CraftingFailure => ResultFlags.HasFlag(ActionResultFlags.CraftingFailure);
        public bool ActionSuccess => ResultFlags.HasFlag(ActionResultFlags.ActionSuccess);
    }

    /*
    It's important to remember that the state in memory will be 0 when you haven't crafted yet
    or the last state of the previous craft. The first state is always normal when starting out
    so don't rely on the condition from memory on the first step. When failing a craft the actual
    failure state is only preset for a short time before the values are all reset to 0.
     */

    public enum ActionCategory : int
    {
        CraftAction = 9,
        Action = 10,
    }

    internal enum CraftingCondition : int
    {
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

    [Flags]
    internal enum ActionResultFlags : ushort
    {
        Unk0 = 1 << 0,
        NotStep1 = 1 << 1,
        CraftingSuccess = 1 << 2,
        CraftingFailure = 1 << 3,
        ActionSuccess = 1 << 4,
    }

    internal enum ActionResult : short
    {
        /*
         FLAGS
         0 0 0 0   0 0 0 0   0
         0 0 0 0   0 0 1 0   2      failure, any action
         0 0 0 0   0 0 1 1   3      failure, first action
         0 0 0 0   1 0 1 0   10
         0 0 0 1   0 0 0 0   16
         0 0 0 1   0 0 0 1   17
         0 0 0 1   0 0 1 0   18   
         0 0 0 1   0 0 1 1   19
         0 0 0 1   0 1 1 0   22
         0 0 0 1   1 0 1 0   26
         0 0 0 1   1 0 1 1   27 craft failure
         0 0 0 1   1 1 1 0   30

        Byte4=crafting complete?

         */

        ActionFailure = 2,
        ActionSuccess = 18,
        CraftSuccess = 22,
        CraftFailure = 26,

        ActionNoChange16 = 16,  // Final Appraisal as first step
        ActionNoChange17 = 17,
        ActionNoChange19 = 19,
        TrialCraftFailure = 10,
        TrialCraftSuccess = 30,
    }
}
