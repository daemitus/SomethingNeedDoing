using System.Threading;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /loop command.
    /// </summary>
    internal class LoopCommand : MacroCommand
    {
        private readonly int loopCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="loopCount">Loop count.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        public LoopCommand(string text, int loopCount, float wait, float waitUntil)
            : base(text, wait, waitUntil)
        {
            this.loopCount = loopCount;
        }

        /// <inheritdoc/>
        public async override void Execute(CancellationToken token)
        {
            await this.PerformWait(token);
        }
    }
}
