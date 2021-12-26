using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar;
using SomethingNeedDoing.Grammar.Commands;

namespace SomethingNeedDoing.Managers
{
    /// <summary>
    /// Manager that handles running macros.
    /// </summary>
    internal partial class MacroManager : IDisposable
    {
        private readonly Stack<ActiveMacro> macroStack = new();
        private readonly CancellationTokenSource eventLoopTokenSource = new();

        private readonly ManualResetEvent loggedInWaiter = new(false);
        private readonly ManualResetEvent pausedWaiter = new(true);

        /// <summary>
        /// Initializes a new instance of the <see cref="MacroManager"/> class.
        /// </summary>
        public MacroManager()
        {
            Service.ClientState.Login += this.OnLogin;
            Service.ClientState.Logout += this.OnLogout;

            // If we're already logged in, toggle the waiter.
            if (Service.ClientState.LocalPlayer != null)
                this.loggedInWaiter.Set();

            // Start the loop.
            Task.Factory.StartNew(this.EventLoop, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Gets the state of the macro manager.
        /// </summary>
        public LoopState State { get; private set; } = LoopState.Waiting;

        /// <summary>
        /// Gets a value indicating whether the manager should pause at the next /loop command.
        /// </summary>
        public bool PauseAtLoop { get; private set; } = false;

        /// <inheritdoc/>
        public void Dispose()
        {
            Service.ClientState.Login -= this.OnLogin;
            Service.ClientState.Logout -= this.OnLogout;

            this.eventLoopTokenSource.Cancel();
            this.eventLoopTokenSource.Dispose();

            this.loggedInWaiter.Dispose();
            this.pausedWaiter.Dispose();
        }

        private void OnLogin(object? sender, EventArgs e)
        {
            this.loggedInWaiter.Set();
            this.State = LoopState.Waiting;
        }

        private void OnLogout(object? sender, EventArgs e)
        {
            this.loggedInWaiter.Reset();
            this.State = LoopState.NotLoggedIn;
        }

        private async void EventLoop()
        {
            var token = this.eventLoopTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Check if the logged in waiter is set
                    if (!this.loggedInWaiter.WaitOne(0))
                    {
                        this.State = LoopState.NotLoggedIn;
                    }

                    // Wait to be logged in
                    this.loggedInWaiter.WaitOne();

                    // Check if the paused waiter has been set
                    if (!this.pausedWaiter.WaitOne(0))
                    {
                        this.State = this.macroStack.Count == 0
                            ? LoopState.Waiting
                            : LoopState.Paused;
                    }

                    // Wait for the un-pause button
                    this.pausedWaiter.WaitOne();

                    // Grab the first, or go back to being paused
                    if (!this.macroStack.TryPeek(out var macro))
                    {
                        this.pausedWaiter.Reset();
                        continue;
                    }

                    this.State = LoopState.Running;
                    if (await this.ProcessMacro(macro, token))
                    {
                        this.macroStack.Pop();
                    }
                }
                catch (OperationCanceledException)
                {
                    PluginLog.Verbose("Event loop has stopped");
                    this.State = LoopState.Stopped;
                    break;
                }
                catch (ObjectDisposedException)
                {
                    PluginLog.Verbose($"Event loop has stopped");
                    this.State = LoopState.Stopped;
                    break;
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Unhandled exception occurred");
                    Service.ChatManager.PrintError($"[SomethingNeedDoing] Peon has died unexpectedly.");
                    this.macroStack.Clear();
                }
            }
        }

        private async Task<bool> ProcessMacro(ActiveMacro macro, CancellationToken token)
        {
            var step = macro.GetCurrentStep();

            if (step == null)
                return true;

            try
            {
                await step.Execute(token);
            }
            catch (MacroCommandError ex)
            {
                Service.ChatManager.PrintError($"{ex.Message}: Failure while running {step} (step {macro.StepIndex + 1})");
                this.pausedWaiter.Reset();
                return false;
            }

            macro.NextStep();

            return false;
        }

        private class ActiveMacro
        {
            public ActiveMacro(MacroNode node)
            {
                this.Node = node;
                this.Steps = MacroParser.Parse(node.Contents).ToArray();
            }

            public MacroNode Node { get; private set; }

            public MacroCommand[] Steps { get; private set; }

            public int StepIndex { get; private set; }

            public void NextStep()
            {
                this.StepIndex++;
            }

            public void Loop()
            {
                this.StepIndex = -1;
            }

            public MacroCommand? GetCurrentStep()
            {
                if (this.StepIndex < 0 || this.StepIndex >= this.Steps.Length)
                    return null;

                return this.Steps[this.StepIndex];
            }
        }
    }

    /// <summary>
    /// Public API.
    /// </summary>
    internal sealed partial class MacroManager
    {
        /// <summary>
        /// Gets the name and currently executing line of each active macro.
        /// </summary>
        public (string Name, int StepIndex)[] MacroStatus
            => this.macroStack.Select(macro => (macro.Node.Name, macro.StepIndex + 1)).ToArray();

        /// <summary>
        /// Run a macro.
        /// </summary>
        /// <param name="node">Macro to run.</param>
        public void EnqueueMacro(MacroNode node)
        {
            this.macroStack.Push(new ActiveMacro(node));
            this.pausedWaiter.Set();
        }

        /// <summary>
        /// Pause macro execution.
        /// </summary>
        /// <param name="pauseAtLoop">Pause at the next loop instead.</param>
        public void Pause(bool pauseAtLoop = false)
        {
            if (pauseAtLoop)
                this.PauseAtLoop ^= pauseAtLoop;
            else
                this.pausedWaiter.Reset();
        }

        /// <summary>
        /// Pause at the next /loop.
        /// </summary>
        public void LoopCheckForPause()
        {
            if (this.PauseAtLoop)
            {
                this.PauseAtLoop = false;
                this.pausedWaiter.Reset();
            }
        }

        /// <summary>
        /// Resume macro execution.
        /// </summary>
        public void Resume()
        {
            this.pausedWaiter.Set();
        }

        /// <summary>
        /// Clear the executing macro list.
        /// </summary>
        public void Clear()
        {
            this.macroStack.Clear();
            this.pausedWaiter.Set();
        }

        /// <summary>
        /// Loop the currently executing macro.
        /// </summary>
        public void Loop()
        {
            this.macroStack.Peek().Loop();
        }

        /// <summary>
        /// Gets the contents of the current macro.
        /// </summary>
        /// <returns>Macro contents.</returns>
        public string[] CurrentMacroContent()
        {
            if (this.macroStack.Count == 0)
                return Array.Empty<string>();

            return this.macroStack.Peek().Steps.Select(s => s.ToString()).ToArray();
        }

        /// <summary>
        /// Gets the executing line number of the current macro.
        /// </summary>
        /// <returns>Macro line number.</returns>
        public int CurrentMacroStep()
        {
            if (this.macroStack.Count == 0)
                return 0;

            return this.macroStack.First().StepIndex;
        }
    }
}
