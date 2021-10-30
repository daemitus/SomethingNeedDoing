using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;

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
        public TargetCommand(string text, string targetName, int wait, int waitUntil)
            : base(text, wait, waitUntil)
        {
            this.targetName = targetName.ToLowerInvariant();
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            var target = Service.ObjectTable.FirstOrDefault(obj => obj.Name.TextValue.ToLowerInvariant() == this.targetName);

            if (target == default)
                throw new MacroCommandError("Could not find target");

            Service.TargetManager.SetTarget(target);

            await this.PerformWait(token);
        }
    }
}
