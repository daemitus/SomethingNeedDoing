using FFXIVClientStructs.FFXIV.Client.UI;

namespace SomethingNeedDoing.Clicks
{
    public sealed class ClickRequest : ClickBase
    {
        public override string Name => "Request";
        public override string AddonName => "Request";

        public unsafe ClickRequest(SomethingNeedDoingPlugin plugin) : base(plugin)
        {
            AvailableClicks["request_hand_over"] = (addon) => SendClick(addon, EventType.CHANGE, 0, ((AddonRequest*)addon)->HandOverButton);
            AvailableClicks["request_cancel"] = (addon) => SendClick(addon, EventType.CHANGE, 1, ((AddonRequest*)addon)->CancelButton);
        }
    }
}
