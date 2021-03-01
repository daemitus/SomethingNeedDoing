using FFXIVClientStructs.FFXIV.Client.UI;

namespace SomethingNeedDoing.Clicks
{
    public sealed class ClickJournalResult : ClickBase
    {
        public override string Name => "JournalResult";
        public override string AddonName => "JournalResult";

        public unsafe ClickJournalResult(SomethingNeedDoingPlugin plugin) : base(plugin)
        {
            AvailableClicks["journal_result_complete"] = (addon) => SendClick(addon, EventType.CHANGE, 1, ((AddonJournalResult*)addon)->CompleteButton->AtkComponentBase.OwnerNode);
            AvailableClicks["journal_result_decline"] = (addon) => SendClick(addon, EventType.CHANGE, 2, ((AddonJournalResult*)addon)->DeclineButton->AtkComponentBase.OwnerNode);
        }
    }
}
