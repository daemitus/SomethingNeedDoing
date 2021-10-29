using System;
using System.Threading;
using System.Threading.Tasks;

namespace SomethingNeedDoing.MacroCommands
{
    /// <summary>
    /// The base command other commands inherit from.
    /// </summary>
    internal abstract class MacroCommand
    {
        private static readonly Random Random = new();

        private readonly float wait;
        private readonly float waitUntil;

        /// <summary>
        /// Initializes a new instance of the <see cref="MacroCommand"/> class.
        /// </summary>
        /// <param name="text">Original line text.</param>
        /// <param name="wait">Wait value.</param>
        /// <param name="waitUntil">WaitUntil value.</param>
        public MacroCommand(string text, float wait, float waitUntil)
        {
            this.Text = text;
            this.wait = wait;
            this.waitUntil = waitUntil;

            if (this.waitUntil > this.wait)
                throw new ArgumentException("WaitUntil may not be larger than the Wait value");
        }

        /// <summary>
        /// Gets the original line text.
        /// </summary>
        public string Text { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Text;
        }

        /// <summary>
        /// Execute a macro command.
        /// </summary>
        /// <param name="token">Async cancellation token.</param>
        public abstract void Execute(CancellationToken token);

        /// <summary>
        /// Perform a wait given the values in Wait and WaitUntil.
        /// May be zero.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected async Task PerformWait(CancellationToken token)
        {
            if (this.wait == 0 && this.waitUntil == 0)
                return;

            TimeSpan sleep;
            if (this.waitUntil == 0)
            {
                sleep = TimeSpan.FromSeconds(this.wait);
            }
            else
            {
                var millis1 = (int)TimeSpan.FromSeconds(this.wait).TotalMilliseconds;
                var millis2 = (int)TimeSpan.FromSeconds(this.waitUntil).TotalMilliseconds;
                var value = Random.Next(millis1, millis2);
                sleep = TimeSpan.FromMilliseconds(value);
            }

            await Task.Delay(sleep, token);
        }
    }
}
