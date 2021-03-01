using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SomethingNeedDoing.Clicks
{
    public abstract class ClickBase
    {
        public abstract string Name { get; }
        public abstract string AddonName { get; }

        public static List<ClickBase> Clickables { get; } = new List<ClickBase>();
        public Dictionary<string, Action<IntPtr>> AvailableClicks { get; } = new Dictionary<string, Action<IntPtr>>();

        protected delegate void ReceiveEventDelegate(IntPtr addon, EventType evt, uint a3, IntPtr a4, IntPtr a5);

        public SomethingNeedDoingPlugin Plugin { get; private set; }

        public ClickBase(SomethingNeedDoingPlugin plugin)
        {
            Plugin = plugin;
        }

        public static void Register(ClickBase clickable)
        {
            Clickables.Add(clickable);
        }

        public bool Click(string name)
        {
            if (AvailableClicks.TryGetValue(name, out Action<IntPtr> clickDelegate))
            {
                var addon = GetAddonByName(AddonName);
                clickDelegate(addon);
                return true;
            }
            return false;
        }

        protected unsafe void SendClick(IntPtr arg1, EventType arg2, uint arg3, void* target, int arg5 = 0)
        {
            var receiveEvent = GetReceiveEventDelegate((AtkEventListener*)arg1);

            var mem4 = Marshal.AllocHGlobal(0x40);
            var mem5 = Marshal.AllocHGlobal(0x40);

            Marshal.WriteIntPtr(mem4 + 0x8, new IntPtr(target));
            Marshal.WriteIntPtr(mem4 + 0x10, arg1);

            Marshal.WriteInt32(mem5, arg5);

            receiveEvent(arg1, arg2, arg3, mem4, mem5);

            Marshal.FreeHGlobal(mem4);
            Marshal.FreeHGlobal(mem5);
        }

        protected IntPtr GetAddonByName(string name) => GetAddonByName(name, 1);

        protected IntPtr GetAddonByName(string name, int index)
        {
            var addon = Plugin.Interface.Framework.Gui.GetUiObjectByName(name, index);
            if (addon == IntPtr.Zero)
                throw new InvalidClickException($"Window is not available for that click");
            return addon;
        }

        protected unsafe ReceiveEventDelegate GetReceiveEventDelegate(AtkEventListener* eventListener)
        {
            var receiveEventAddress = new IntPtr(eventListener->vfunc[2]);
            return Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>(receiveEventAddress);
        }

        protected unsafe (float, float) BacktrackNodePoint(AtkResNode* node)
        {
            if (node == null)
                throw new Exception("Node does not exist");

            float x = node->X + (node->Width / 2);
            float y = node->Y + (node->Height / 2);

            AtkResNode* parent = node;
            while ((parent = parent->ParentNode) != null)
            {
                x += parent->X;
                y += parent->Y;
            }
            return (x, y);
        }

        protected unsafe (float, float) ConvertToAbsolute(float x, float y)
        {
            var ax = x * 0xffff / GetSystemMetrics(SM_CXSCREEN);
            var ay = y * 0xffff / GetSystemMetrics(SM_CYSCREEN);
            return (ax, ay);
        }

        private const int SM_CXSCREEN = 0x0;
        private const int SM_CYSCREEN = 0x1;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int smIndex);
    }
}
