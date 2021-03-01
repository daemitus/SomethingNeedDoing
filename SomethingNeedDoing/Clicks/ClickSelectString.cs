using FFXIVClientStructs.FFXIV.Client.UI;
using System;

namespace SomethingNeedDoing.Clicks
{
    public sealed class ClickSelectString : ClickBase
    {
        public override string Name => "SelectString";
        public override string AddonName => "SelectString";

        public unsafe ClickSelectString(SomethingNeedDoingPlugin plugin) : base(plugin)
        {
            for (int i = 1; i <= 8; i++)
                AvailableClicks[$"select_string{i}"] = (addon) => ClickItem(addon, i);
        }

        private unsafe void ClickItem(IntPtr addon, int index)
        {
            var compList = ((AddonSelectString*)addon)->AtkComponentList;
            SendClick(new IntPtr(compList), EventType.CHANGE, 1, compList->ItemRendererList[index].AtkComponentListItemRenderer->AtkComponentButton.AtkComponentBase.OwnerNode);
        }
    }
}
