using System.Threading;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /require command.
    /// </summary>
    internal class RequireCommand : MacroCommand
    {
        private readonly string effectName;
        private readonly float maxwait;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="effectName">Effect name.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        /// <param name="maxwait">MaxWait value.</param>
        public RequireCommand(string text, string effectName, float wait, float waitUntil, float maxwait)
            : base(text, wait, waitUntil)
        {
            this.effectName = effectName;
            this.maxwait = maxwait;
        }

        /// <inheritdoc/>
        public async override void Execute(CancellationToken token)
        {
            await this.PerformWait(token);
        }
    }
}
