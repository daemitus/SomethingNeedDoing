using System.Threading;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// A command handled by the game.
    /// </summary>
    internal class NativeCommand : MacroCommand
    {
        private readonly string text;

        /// <summary>
        /// Initializes a new instance of the <see cref="NativeCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        public NativeCommand(string text, float wait, float waitUntil)
            : base(text, wait, waitUntil)
        {
            this.text = text;
        }

        /// <inheritdoc/>
        public async override void Execute(CancellationToken token)
        {
            await this.PerformWait(token);
        }
    }
}
