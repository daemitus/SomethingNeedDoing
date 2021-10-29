using System.Threading;

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
        public WaitCommand(string text, float wait, float waitUntil)
            : base(text, wait, waitUntil)
        {
        }

        /// <inheritdoc/>
        public async override void Execute(CancellationToken token)
        {
            await this.PerformWait(token);
        }
    }
}
