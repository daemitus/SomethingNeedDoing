using System.Threading;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /waitaddon command.
    /// </summary>
    internal class WaitAddonCommand : MacroCommand
    {
        private readonly string addonName;
        private readonly float maxwait;

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitAddonCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="addonName">Addon name.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        /// <param name="maxwait">MaxWait value.</param>
        public WaitAddonCommand(string text, string addonName, float wait, float waitUntil, float maxwait)
            : base(text, wait, waitUntil)
        {
            this.addonName = addonName;
            this.maxwait = maxwait;
        }

        /// <inheritdoc/>
        public async override void Execute(CancellationToken token)
        {
            await this.PerformWait(token);
        }
    }
}
