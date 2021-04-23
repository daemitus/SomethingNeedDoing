using Dalamud.Game.Internal;
using Dalamud.Plugin;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;

namespace SomethingNeedDoing
{
    internal class ChatManager : IDisposable
    {
        private readonly SomethingNeedDoingPlugin plugin;
        private readonly FrameworkGetUiModuleDelegate FrameworkGetUIModule;
        private readonly ProcessChatBoxDelegate ProcessChatBox;
        private readonly Channel<string> ChatBoxMessages = Channel.CreateUnbounded<string>();

        public ChatManager(SomethingNeedDoingPlugin plugin)
        {
            this.plugin = plugin;
            FrameworkGetUIModule = Marshal.GetDelegateForFunctionPointer<FrameworkGetUiModuleDelegate>(plugin.Address.FrameworkGetUIModuleAddress);
            ProcessChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(plugin.Address.ProcessChatBoxAddress);

            plugin.Interface.Framework.OnUpdateEvent += Framework_OnUpdateEvent;
        }

        public void Dispose()
        {
            plugin.Interface.Framework.OnUpdateEvent -= Framework_OnUpdateEvent;
            ChatBoxMessages.Writer.Complete();
        }

        public void PrintMessage(string message) => plugin.Interface.Framework.Gui.Chat.Print(message);

        public void PrintError(string message) => plugin.Interface.Framework.Gui.Chat.PrintError(message);

        private void Framework_OnUpdateEvent(Framework framework)
        {
            if (ChatBoxMessages.Reader.TryRead(out var message))
                SendChatBoxMessageInternal(message);
        }

        public async void SendChatBoxMessage(string message)
        {
            await ChatBoxMessages.Writer.WriteAsync(message);
        }

        private void SendChatBoxMessageInternal(string message)
        {
            var framework = Marshal.ReadIntPtr(plugin.Address.FrameworkPointerAddress);
            var uiModule = FrameworkGetUIModule(framework);

            var payloadSize = Marshal.SizeOf<ChatPayload>();
            var payloadPtr = Marshal.AllocHGlobal(payloadSize);
            var payload = new ChatPayload(message);

            Marshal.StructureToPtr(payload, payloadPtr, false);

            ProcessChatBox(uiModule, payloadPtr, IntPtr.Zero, 0);

            Marshal.FreeHGlobal(payloadPtr);
            payload.Dispose();
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
            var textBytes = Encoding.UTF8.GetBytes(text);
            textPtr = Marshal.AllocHGlobal(textBytes.Length + 1);

            Marshal.Copy(textBytes, 0, textPtr, textBytes.Length);
            Marshal.WriteByte(textPtr + textBytes.Length, 0);

            textLen = (ulong)(textBytes.Length + 1);
            unk1 = 64;
            unk2 = 0;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(textPtr);
        }
    }
}
