using FFXIVClientStructs.FFXIV.Client.UI;

namespace SomethingNeedDoing.Clicks
{
    public sealed class ClickRecipeNote : ClickBase
    {
        public override string Name => "RecipeBook";
        public override string AddonName => "RecipeNote";

        public unsafe ClickRecipeNote(SomethingNeedDoingPlugin plugin) : base(plugin)
        {
            AvailableClicks["synthesize"] = (addon) => SendClick(addon, EventType.CHANGE, 13, ((AddonRecipeNote*)addon)->SynthesizeButton->AtkComponentBase.OwnerNode);
            AvailableClicks["trial_synthesis"] = (addon) => SendClick(addon, EventType.CHANGE, 15, ((AddonRecipeNote*)addon)->TrialSynthesisButton->AtkComponentBase.OwnerNode);
        }
    }
}
