using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;

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
        public RunMacroCommand(string text, string macroName, int wait, int waitUntil)
            : base(text, wait, waitUntil)
        {
            this.macroName = macroName;
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            var macroNode = Service.Configuration
                .GetAllNodes().OfType<MacroNode>()
                .FirstOrDefault(macro => macro.Name == this.macroName);

            if (macroNode == default)
                throw new MacroCommandError("No macro with that name");

            try
            {
                Service.MacroManager.EnqueueMacro(macroNode);
            }
            catch (MacroSyntaxError ex)
            {
                var errorLine = macroNode.Contents.Split('\n')[ex.LineNumber];
                throw new MacroCommandError($"Syntax error on line {ex.LineNumber + 1}: {errorLine}", ex);
            }

            await this.PerformWait(token);
        }
    }
}
