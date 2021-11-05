using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Grammar.Modifiers;

namespace SomethingNeedDoing.Grammar.Commands
{
    /// <summary>
    /// The base command other commands inherit from.
    /// </summary>
    internal abstract class MacroCommand
    {
        private static readonly Random Rand = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MacroCommand"/> class.
        /// </summary>
        /// <param name="text">Original line text.</param>
        /// <param name="wait">Wait value.</param>
        protected MacroCommand(string text, WaitModifier wait)
            : this(text, wait.Wait, wait.Until)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MacroCommand"/> class.
        /// </summary>
        /// <param name="text">Original line text.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="until">WaitUntil value.</param>
        protected MacroCommand(string text, int wait, int until)
        {
            this.Text = text;
            this.Wait = wait;
            this.WaitUntil = until;

            if (this.WaitUntil > 0 && this.WaitUntil < this.Wait)
                throw new ArgumentException("WaitUntil must not be larger than the Wait value");
        }

        /// <summary>
        /// Gets the original line text.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// Gets the WaitModifier "wait" value.
        /// </summary>
        protected int Wait { get; }

        /// <summary>
        /// Gets the WaitModifier "waitUntil" value.
        /// </summary>
        protected int WaitUntil { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Text;
        }

        /// <summary>
        /// Execute a macro command.
        /// </summary>
        /// <param name="token">Async cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public abstract Task Execute(CancellationToken token);

        /// <summary>
        /// Extract a match group and unquote if necessary.
        /// </summary>
        /// <param name="match">Match group.</param>
        /// <param name="groupName">Group name.</param>
        /// <returns>Extracted and unquoted group value.</returns>
        protected static string ExtractAndUnquote(Match match, string groupName)
        {
            var group = match.Groups[groupName];
            var groupValue = group.Value;

            if (groupValue.StartsWith('"') && groupValue.EndsWith('"'))
                groupValue = groupValue.Trim('"');

            return groupValue;
        }

        /// <summary>
        /// Perform a wait given the values in <see cref="Wait"/> and <see cref="WaitUntil"/>.
        /// May be zero.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task PerformWait(CancellationToken token)
            => await this.PerformWait(this.Wait, this.WaitUntil, token);

        /// <summary>
        /// Perform a wait.
        /// </summary>
        /// <param name="wait">Seconds to wait.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task PerformWait(int wait, CancellationToken token)
            => await this.PerformWait(wait, 0, token);

        /// <summary>
        /// Perform a wait.
        /// </summary>
        /// <param name="wait">Seconds to wait.</param>
        /// <param name="until">Max seconds to wait.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task PerformWait(int wait, int until, CancellationToken token)
        {
            if (wait == 0 && until == 0)
                return;

            TimeSpan sleep;
            if (until == 0)
            {
                sleep = TimeSpan.FromMilliseconds(wait);
                PluginLog.Debug($"Sleeping for {sleep.TotalMilliseconds} millis");
            }
            else
            {
                var value = Rand.Next(wait, until);
                sleep = TimeSpan.FromMilliseconds(value);
                PluginLog.Debug($"Sleeping for {sleep.TotalMilliseconds} millis ({wait} to {until})");
            }

            await Task.Delay(sleep, token);
        }

        /// <summary>
        /// Perform an action every <paramref name="interval"/> seconds until either the action succeeds or <paramref name="until"/> seconds elapse.
        /// </summary>
        /// <param name="interval">Action execution interval.</param>
        /// <param name="until">Maximum time to wait.</param>
        /// <param name="action">Action to execute.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A value indicating whether the action succeeded.</returns>
        /// <typeparam name="T">Result type.</typeparam>
        protected async Task<(T? Result, bool Success)> LinearWait<T>(int interval, int until, Func<(T? Result, bool Success)> action, CancellationToken token)
        {
            var totalWait = 0;
            while (true)
            {
                var (result, success) = action();
                if (success)
                    return (result, true);

                totalWait += interval;
                if (totalWait > until)
                    return (result, false);

                await Task.Delay(interval, token);
            }
        }
    }
}
