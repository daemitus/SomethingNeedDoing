using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;

namespace SomethingNeedDoing.Grammar.Commands
{
    /// <summary>
    /// The /action command.
    /// </summary>
    internal class ActionCommand : MacroCommand
    {
        private const int SafeCraftMaxWait = 5000;

        private static readonly Regex Regex = new(@"^/(?:ac|action)\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
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
        /// <param name="safely">Perform the action safely.</param>
        private ActionCommand(string text, string actionName, WaitModifier wait, bool safely)
            : base(text, wait.Wait, wait.Until)
        {
            this.actionName = actionName.ToLowerInvariant();
            this.safely = safely;
        }

        /// <summary>
        /// Parse the text as a command.
        /// </summary>
        /// <param name="text">Text to parse.</param>
        /// <returns>A parsed command.</returns>
        public static ActionCommand Parse(string text)
        {
            _ = WaitModifier.TryParse(ref text, out var waitModifier);
            var hasUnsafe = UnsafeModifier.TryParse(ref text, out var _);

            var match = Regex.Match(text);
            if (!match.Success)
                throw new MacroSyntaxError(text);

            var nameValue = ExtractAndUnquote(match, "name");

            return new ActionCommand(text, nameValue, waitModifier, !hasUnsafe);
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
