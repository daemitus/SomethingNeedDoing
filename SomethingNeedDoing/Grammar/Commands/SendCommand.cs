using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;

namespace SomethingNeedDoing.Grammar.Commands
{
    /// <summary>
    /// The /send command.
    /// </summary>
    internal class SendCommand : MacroCommand
    {
        private static readonly Regex Regex = new(@"^/send\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly VirtualKey vkCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="vkCode">VirtualKey code.</param>
        /// <param name="wait">Wait value.</param>
        private SendCommand(string text, VirtualKey vkCode, WaitModifier wait)
            : base(text, wait)
        {
            this.vkCode = vkCode;
        }

        /// <summary>
        /// Parse the text as a command.
        /// </summary>
        /// <param name="text">Text to parse.</param>
        /// <returns>A parsed command.</returns>
        public static SendCommand Parse(string text)
        {
            _ = WaitModifier.TryParse(ref text, out var waitModifier);

            var match = Regex.Match(text);
            if (!match.Success)
                throw new MacroSyntaxError(text);

            var nameValue = ExtractAndUnquote(match, "name");

            if (!Enum.TryParse<VirtualKey>(nameValue, true, out var vkCode))
                throw new MacroCommandError("Invalid virtual key");

            return new SendCommand(text, vkCode, waitModifier);
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            KeyboardManager.Send(this.vkCode);

            await this.PerformWait(token);
        }
    }
}
