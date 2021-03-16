using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SomethingNeedDoing
{
    internal class ChatManager : IDisposable
    {
        private readonly SomethingNeedDoingPlugin plugin;
        private readonly FrameworkGetUiModuleDelegate FrameworkGetUIModule;
        private readonly ProcessChatBoxDelegate ProcessChatBox;

        public ChatManager(SomethingNeedDoingPlugin plugin)
        {
            this.plugin = plugin;
            FrameworkGetUIModule = Marshal.GetDelegateForFunctionPointer<FrameworkGetUiModuleDelegate>(plugin.Address.FrameworkGetUIModuleAddress);
            ProcessChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(plugin.Address.ProcessChatBoxAddress);
        }

        public void Dispose() { }

        public void PrintMessage(string message) => plugin.Interface.Framework.Gui.Chat.Print(message);

        public void PrintError(string message) => plugin.Interface.Framework.Gui.Chat.PrintError(message);

        public void SendChatBoxMessage(string message)
        {
            var uiModule = FrameworkGetUIModule(Marshal.ReadIntPtr(plugin.Address.FrameworkPointerAddress));

            if (uiModule == IntPtr.Zero)
                throw new ApplicationException("uiModule was null");

            using var payload = new ChatPayload(message);
            var mem1 = Marshal.AllocHGlobal(400);

            for (var i = 0; i < 400; i++)
                Marshal.WriteByte(mem1 + i, 0);

            Marshal.StructureToPtr(payload, mem1, false);

            ProcessChatBox(uiModule, mem1, IntPtr.Zero, 0);

            Marshal.FreeHGlobal(mem1);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal readonly struct ChatPayload : IDisposable
    {
        [FieldOffset(0x0)]
        private readonly IntPtr textPtr;

        [FieldOffset(0x8)]
        private readonly ulong unk1;

        [FieldOffset(0x10)]
        private readonly ulong textLen;

        [FieldOffset(0x18)]
        private readonly ulong unk2;

        internal ChatPayload(string text)
        {
            var stringBytes = Encoding.UTF8.GetBytes(text);
            textPtr = Marshal.AllocHGlobal(stringBytes.Length + 30);

            for (var i = 0; i < stringBytes.Length; i++)
                Marshal.WriteByte(textPtr + i, 0);

            Marshal.Copy(stringBytes, 0, textPtr, stringBytes.Length);
            Marshal.WriteByte(textPtr + stringBytes.Length, 0);

            textLen = (ulong)(stringBytes.Length + 1);
            unk1 = 64;
            unk2 = 0;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(textPtr);
        }
    }
}
