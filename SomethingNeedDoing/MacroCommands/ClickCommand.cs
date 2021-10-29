using System.Threading;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /click command.
    /// </summary>
    internal class ClickCommand : MacroCommand
    {
        private readonly string clickName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClickCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="clickName">Click name.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        public ClickCommand(string text, string clickName, float wait, float waitUntil)
            : base(text, wait, waitUntil)
        {
            this.clickName = clickName;
        }

        /// <inheritdoc/>
        public async override void Execute(CancellationToken token)
        {
            await this.PerformWait(token);
        }
    }
}
