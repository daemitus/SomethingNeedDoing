using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// A command handled by the game.
    /// </summary>
    internal class NativeCommand : MacroCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NativeCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        public NativeCommand(string text, int wait, int waitUntil)
            : base(text, wait, waitUntil)
        {
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            Service.ChatManager.SendMessage(this.Text);

            await this.PerformWait(token);
        }
    }
}
