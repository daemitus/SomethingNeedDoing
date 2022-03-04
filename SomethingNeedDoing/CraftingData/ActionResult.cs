namespace SomethingNeedDoing.CraftingData;

/// <summary>
/// Event action result types.
/// </summary>
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

    // ActionFailure = 2,
    // ActionSuccess = 18,
    // CraftSuccess = 22,
    // CraftFailure = 26,
    // ActionNoChange16 = 16,  // Final Appraisal as first step
    // ActionNoChange17 = 17,
    // ActionNoChange19 = 19,
    // TrialCraftFailure = 10,
    // TrialCraftSuccess = 30,
}
