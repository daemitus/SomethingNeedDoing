using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /wait command.
    /// </summary>
    internal class WaitCommand : MacroCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WaitCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        public WaitCommand(string text, int wait, int waitUntil)
            : base(text, wait, waitUntil)
        {
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            await this.PerformWait(token);
        }
    }
}
