using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The /require command.
    /// </summary>
    internal class RequireCommand : MacroCommand
    {
        private const int StatusCheckMaxWait = 1000;
        private const int StatusCheckInterval = 250;

        private readonly List<uint> statusIDs;
        private readonly int maxWait;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequireCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="statusName">Status name.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        /// <param name="maxWait">MaxWait value.</param>
        public RequireCommand(string text, string statusName, int wait, int waitUntil, int maxWait)
            : base(text, wait, waitUntil)
        {
            this.maxWait = maxWait == 0 ? StatusCheckMaxWait : maxWait;

            statusName = statusName.ToLowerInvariant();
            var sheet = Service.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Status>()!;
            this.statusIDs = sheet
                .Where(row => row.Name.RawString.ToLowerInvariant() == statusName)
                .Select(row => row.RowId)
                .ToList()!;
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            var (statusID, hasStatus) = await this.LinearWait(StatusCheckInterval, this.maxWait, this.IsStatusPresent, token);

            if (!hasStatus)
                throw new MacroCommandError("Status effect not found");

            await this.PerformWait(token);
        }

        private unsafe (uint StatusID, bool HasStatus) IsStatusPresent()
        {
            var statusID = Service.ClientState.LocalPlayer!.StatusList
                .Select(se => se.StatusId)
                .ToList().Intersect(this.statusIDs)
                .FirstOrDefault();

            if (statusID == default)
                return (0, false);

            return (statusID, true);
        }
    }
}
