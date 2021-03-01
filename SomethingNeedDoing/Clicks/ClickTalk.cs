using FFXIVClientStructs.FFXIV.Client.UI;

namespace SomethingNeedDoing.Clicks
{
    public sealed class ClickTalk : ClickBase
    {
        public override string Name => "Talk";
        public override string AddonName => "Talk";

        public unsafe ClickTalk(SomethingNeedDoingPlugin plugin) : base(plugin)
        {
            AvailableClicks["talk"] = (addon) => SendClick(addon, EventType.INPUT, 0, ((AddonTalk*)addon)->AtkStage);
        }
    }
}
