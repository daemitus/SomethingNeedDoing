using FFXIVClientStructs.FFXIV.Client.UI;

namespace SomethingNeedDoing.Clicks
{
    public sealed class ClickSelectIconString : ClickBase
    {
        public override string Name => "SelectIconString";
        public override string AddonName => "SelectIconString";

        public unsafe ClickSelectIconString(SomethingNeedDoingPlugin plugin) : base(plugin)
        {
            //AvailableClicks["select_icon_string"] = (addon) => SendClick(0, 0, ((AddonSelectIconString*)addon)->AtkComponentList->AtkComponentBase.ULDData.Objects->NodeList[0]);

            /*
            Component::GUI::AtkComponentListItemRenderer.ReceiveEvent    this=0x17AA3230610 evt=EventType.MOUSE_CLICK          a3=6   a4=0x17AB89F9FF0 a5=0x5C34CFEC08
            0, collisionNode, listItemRenderer

            Component::GUI::AtkComponentList.ReceiveEvent                this=0x17AA3231F70 evt=EventType.CHANGE               a3=1   a4=0x17AB89FD010 a5=0x5C34CFE928
            0, componentNode, componentList
             */
        }
    }
}
