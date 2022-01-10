using System;

using Dalamud.Game;
using Dalamud.Logging;

namespace SomethingNeedDoing
{
    /// <summary>
    /// Plugin address resolver.
    /// </summary>
    internal class PluginAddressResolver : BaseAddressResolver
    {
        private const string SendChatSignature = "48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9";
        private const string EventFrameworkSignature = "48 8D 0D ?? ?? ?? ?? 48 8B AC 24 ?? ?? ?? ?? 33 C0";  // g_EventFramework + 0x44
        private const string EventFrameworkFunctionSignature = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 54 24 ?? 56 57 41 56 48 83 EC 50";

        /// <summary>
        /// Gets the address of the SendChat method.
        /// </summary>
        public IntPtr SendChatAddress { get; private set; }

        /// <summary>
        /// Gets the address of the event framework.
        /// </summary>
        public IntPtr EventFrameworkAddress { get; private set; }

        /// <summary>
        /// Gets the address of the event framework function.
        /// </summary>
        public IntPtr EventFrameworkFunctionAddress { get; private set; }

        /// <inheritdoc/>
        protected override void Setup64Bit(SigScanner scanner)
        {
            this.SendChatAddress = scanner.ScanText(SendChatSignature);
            this.EventFrameworkAddress = scanner.GetStaticAddressFromSig(EventFrameworkSignature) + 1;
            this.EventFrameworkFunctionAddress = scanner.ScanText(EventFrameworkFunctionSignature);

            PluginLog.Verbose("===== SOMETHING NEED DOING =====");
            PluginLog.Verbose($"{nameof(this.SendChatAddress)} {this.SendChatAddress.ToInt64():X}");
            PluginLog.Verbose($"{nameof(this.EventFrameworkAddress)} {this.EventFrameworkAddress.ToInt64():X}");
            PluginLog.Verbose($"{nameof(this.EventFrameworkFunctionAddress)} {this.EventFrameworkFunctionAddress.ToInt64():X}");
        }
    }
}
