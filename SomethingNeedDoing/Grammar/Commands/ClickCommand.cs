using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using ClickLib;
using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;

namespace SomethingNeedDoing.Grammar.Commands
{
    /// <summary>
    /// The /click command.
    /// </summary>
    internal class ClickCommand : MacroCommand
    {
        private static readonly Regex Regex = new(@"^/click\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly string clickName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClickCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="clickName">Click name.</param>
        /// <param name="wait">Wait value.</param>
        private ClickCommand(string text, string clickName, WaitModifier wait)
            : base(text, wait)
        {
            this.clickName = clickName;
        }

        /// <summary>
        /// Parse the text as a command.
        /// </summary>
        /// <param name="text">Text to parse.</param>
        /// <returns>A parsed command.</returns>
        public static ClickCommand Parse(string text)
        {
            _ = WaitModifier.TryParse(ref text, out var waitModifier);

            var match = Regex.Match(text);
            if (!match.Success)
                throw new MacroSyntaxError(text);

            var nameValue = ExtractAndUnquote(match, "name");

            return new ClickCommand(text, nameValue, waitModifier);
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            try
            {
                Click.SendClick(this.clickName.ToLowerInvariant());
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
