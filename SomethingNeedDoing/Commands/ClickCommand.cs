using System;
using System.Threading;
using System.Threading.Tasks;

using ClickLib;
using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;

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
        public ClickCommand(string text, string clickName, int wait, int waitUntil)
            : base(text, wait, waitUntil)
        {
            this.clickName = clickName;
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            try
            {
                Click.SendClick(this.clickName);
            }
            catch (ClickNotFoundError)
            {
                throw new MacroCommandError("Click not found");
            }
            catch (Exception ex)
            {
                throw new MacroCommandError("Unexpected click error", ex);
            }

            await this.PerformWait(token);
        }
    }
}
