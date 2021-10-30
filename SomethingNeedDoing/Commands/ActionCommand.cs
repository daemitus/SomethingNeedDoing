using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /action command.
    /// </summary>
    internal class ActionCommand : MacroCommand
    {
        private const int SafeCraftMaxWait = 5000;

        private static readonly HashSet<string> CraftingActionNames = new();

        private readonly string actionName;
        private readonly bool safely;

        static ActionCommand()
        {
            PopulateCraftingNames();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="actionName">Action name.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        /// <param name="safely">Execute safely.</param>
        public ActionCommand(string text, string actionName, int wait, int waitUntil, bool safely)
            : base(text, wait, waitUntil)
        {
            this.actionName = actionName.ToLowerInvariant();
            this.safely = safely;
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            if (IsCraftingAction(this.actionName))
            {
                var dataWaiter = Service.EventFrameworkManager.DataAvailableWaiter;
                dataWaiter.Reset();

                Service.ChatManager.SendMessage(this.Text);

                await this.PerformWait(token);

                if (this.safely && !dataWaiter.WaitOne(SafeCraftMaxWait))
                    throw new MacroCommandError("Did not receive a timely response");
            }
            else
            {
                Service.ChatManager.SendMessage(this.Text);

                await this.PerformWait(token);
            }
        }

        private static bool IsCraftingAction(string name)
            => CraftingActionNames.Contains(name);

        private static void PopulateCraftingNames()
        {
            var actions = Service.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()!;
            foreach (var row in actions)
            {
                var job = row.ClassJob?.Value?.ClassJobCategory?.Value;
                if (job == null)
                    continue;

                if (job.CRP || job.BSM || job.ARM || job.GSM || job.LTW || job.WVR || job.ALC || job.CUL)
                {
                    var name = row.Name.RawString.ToLowerInvariant();
                    if (name.Length == 0)
                        continue;

                    CraftingActionNames.Add(name);
                }
            }

            var craftActions = Service.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.CraftAction>()!;
            foreach (var row in craftActions)
            {
                var name = row.Name.RawString.ToLowerInvariant();
                if (name.Length == 0)
                    continue;

                CraftingActionNames.Add(name);
            }
        }
    }
}
