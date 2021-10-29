using System.Threading;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /action command.
    /// </summary>
    internal class ActionCommand : MacroCommand
    {
        private readonly string actionName;
        private readonly bool safely;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="actionName">Action name.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        /// <param name="safely">Execute safely.</param>
        public ActionCommand(string text, string actionName, float wait, float waitUntil, bool safely)
            : base(text, wait, waitUntil)
        {
            this.actionName = actionName;
            this.safely = safely;
        }

        /// <inheritdoc/>
        public async override void Execute(CancellationToken token)
        {
            await this.PerformWait(token);
        }
    }
}
