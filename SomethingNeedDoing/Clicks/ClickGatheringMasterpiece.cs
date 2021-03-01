using FFXIVClientStructs.FFXIV.Client.UI;

namespace SomethingNeedDoing.Clicks
{
    public class ClickGatheringMasterpiece : ClickBase
    {
        public override string Name => "Collectables";
        public override string AddonName => "GatheringMasterpiece";

        public unsafe ClickGatheringMasterpiece(SomethingNeedDoingPlugin plugin) : base(plugin)
        {
            AvailableClicks["collect"] = (addon) => SendClick(addon, EventType.ICON_TEXT_ROLL_OUT, 112, ((AddonGatheringMasterpiece*)addon)->CollectDragDrop->AtkComponentBase.OwnerNode);
        }
    }
}
