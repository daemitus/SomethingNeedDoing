using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Modifiers;

namespace SomethingNeedDoing.Grammar.Commands
{
    /// <summary>
    /// The /loop command.
    /// </summary>
    internal class LoopCommand : MacroCommand
    {
        private static readonly Regex Regex = new(@"^/loop(?:\s+(?<count>\d+))?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private int loopsRemaining;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoopCommand"/> class.
        /// </summary>
        /// <param name="text">Original text.</param>
        /// <param name="loopCount">Loop count.</param>
        /// <param name="wait">Wait value.</param>
        private LoopCommand(string text, int loopCount, WaitModifier wait)
            : base(text, wait)
        {
            this.loopsRemaining = loopCount >= 0 ? loopCount : int.MaxValue;
        }

        /// <summary>
        /// Parse the text as a command.
        /// </summary>
        /// <param name="text">Text to parse.</param>
        /// <returns>A parsed command.</returns>
        public static LoopCommand Parse(string text)
        {
            _ = WaitModifier.TryParse(ref text, out var waitModifier);

            var match = Regex.Match(text);
            if (!match.Success)
                throw new MacroSyntaxError(text);

            var countGroup = match.Groups["count"];
            var countValue = countGroup.Success
                ? int.Parse(countGroup.Value, CultureInfo.InvariantCulture)
                : int.MaxValue;

            return new LoopCommand(text, countValue, waitModifier);
        }

        /// <inheritdoc/>
        public async override Task Execute(CancellationToken token)
        {
            PluginLog.Debug($"Executing: {this.Text}");

            if (this.loopsRemaining == 0)
                return;

            this.loopsRemaining--;
            Service.MacroManager.Loop();

            await this.PerformWait(token);
        }
    }
}
