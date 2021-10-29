using System.Threading;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /runmacro command.
    /// </summary>
    internal class RunMacroCommand : MacroCommand
    {
        private readonly string macroName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RunMacroCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="macroName">Macro name.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        public RunMacroCommand(string text, string macroName, float wait, float waitUntil)
            : base(text, wait, waitUntil)
        {
            this.macroName = macroName;
        }

        /// <inheritdoc/>
        public async override void Execute(CancellationToken token)
        {
            await this.PerformWait(token);
        }
    }
}
