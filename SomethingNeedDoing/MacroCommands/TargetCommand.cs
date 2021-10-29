using System.Threading;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /target command.
    /// </summary>
    internal class TargetCommand : MacroCommand
    {
        private readonly string targetName;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="targetName">Target name.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        public TargetCommand(string text, string targetName, float wait, float waitUntil)
            : base(text, wait, waitUntil)
        {
            this.targetName = targetName;
        }

        /// <inheritdoc/>
        public async override void Execute(CancellationToken token)
        {
            await this.PerformWait(token);
        }
    }
}
