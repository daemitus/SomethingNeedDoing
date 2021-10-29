using System.Threading;

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
        public SendCommand(string text, string keyName, float wait, float waitUntil)
            : base(text, wait, waitUntil)
        {
            this.keyName = keyName;
        }

        /// <inheritdoc/>
        public async override void Execute(CancellationToken token)
        {
            await this.PerformWait(token);
        }
    }
}
