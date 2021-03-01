using FFXIVClientStructs.FFXIV.Client.UI;

namespace SomethingNeedDoing.Clicks
{
    public sealed class ClickJournalDetail : ClickBase
    {
        public override string Name => "JournalDetail";
        public override string AddonName => "JournalDetail";

        public unsafe ClickJournalDetail(SomethingNeedDoingPlugin plugin) : base(plugin)
        {
            AvailableClicks["journal_detail_accept"] = (addon) => SendClick(addon, EventType.CHANGE, 1, ((AddonJournalDetail*)addon)->AcceptButton);
            AvailableClicks["journal_detail_decline"] = (addon) => SendClick(addon, EventType.CHANGE, 2, ((AddonJournalDetail*)addon)->AcceptButton);
        }
    }
}
