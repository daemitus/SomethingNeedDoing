using FFXIVClientStructs.FFXIV.Client.UI;

namespace SomethingNeedDoing.Clicks
{
    public sealed class ClickSelectYesNo : ClickBase
    {
        public override string Name => "SelectYesno";
        public override string AddonName => "SelectYesno";

        public unsafe ClickSelectYesNo(SomethingNeedDoingPlugin plugin) : base(plugin)
        {
            AvailableClicks["select_yes"] = (addon) => SendClick(addon, EventType.CHANGE, 0, ((AddonSelectYesno*)addon)->YesButton->AtkComponentBase.OwnerNode);
            AvailableClicks["select_no"] = (addon) => SendClick(addon, EventType.CHANGE, 1, ((AddonSelectYesno*)addon)->NoButton->AtkComponentBase.OwnerNode);
        }
    }
}
