using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /loop command.
    /// </summary>
    internal class LoopCommand : MacroCommand
    {
        private int loopsRemaining;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="loopCount">Loop count.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        public LoopCommand(string text, int loopCount, int wait, int waitUntil)
            : base(text, wait, waitUntil)
        {
            this.loopsRemaining = loopCount == 0 ? int.MaxValue : loopCount;
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            if (this.loopsRemaining == 0)
                return;

            this.loopsRemaining--;
            Service.MacroManager.Loop();

            await this.PerformWait(token);
        }
    }
}
