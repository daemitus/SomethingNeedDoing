using System;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SomethingNeedDoing.Exceptions;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /waitaddon command.
    /// </summary>
    internal class WaitAddonCommand : MacroCommand
    {
        private const int AddonCheckMaxWait = 5000;
        private const int AddonCheckInterval = 500;

        private readonly string addonName;
        private readonly int maxWait;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitAddonCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="addonName">Addon name.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        /// <param name="maxWait">MaxWait value.</param>
        public WaitAddonCommand(string text, string addonName, int wait, int waitUntil, int maxWait)
            : base(text, wait, waitUntil)
        {
            this.addonName = addonName;
            this.maxWait = maxWait == 0 ? AddonCheckMaxWait : maxWait;
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            var (addonPtr, isVisible) = await this.LinearWait(AddonCheckInterval, this.maxWait, this.IsAddonVisible, token);

            if (addonPtr == IntPtr.Zero)
                throw new MacroCommandError("Addon not found");

            if (!isVisible)
                throw new MacroCommandError("Addon not visible");

            await this.PerformWait(token);
        }

        private unsafe (IntPtr Addon, bool IsVisible) IsAddonVisible()
        {
            var addonPtr = Service.GameGui.GetAddonByName(this.addonName, 1);
            if (addonPtr == IntPtr.Zero)
                return (addonPtr, false);

            var addon = (AtkUnitBase*)addonPtr;
            if (!addon->IsVisible || addon->UldManager.LoadedState != 3)
                return (addonPtr, false);

            return (addonPtr, true);
        }
    }
}
