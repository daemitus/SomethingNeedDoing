using System;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /send command.
    /// </summary>
    internal class SendCommand : MacroCommand
    {
        private readonly string keyName;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="keyName">VirtualKey name.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        public SendCommand(string text, string keyName, int wait, int waitUntil)
            : base(text, wait, waitUntil)
        {
            this.keyName = keyName;
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            if (!Enum.TryParse<VirtualKey>(this.keyName, true, out var vkCode))
                throw new MacroCommandError("Invalid virtual key");

            KeyboardManager.Send(vkCode);

            await this.PerformWait(token);
        }
    }
}
