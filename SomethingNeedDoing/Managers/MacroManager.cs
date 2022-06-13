using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Logging;
using NLua.Exceptions;
using SomethingNeedDoing.Exceptions;
using SomethingNeedDoing.Grammar.Commands;
using SomethingNeedDoing.Misc;

namespace SomethingNeedDoing.Managers;

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
    /// Gets a value indicating whether the manager should pause at the next loop.
    /// </summary>
    public bool PauseAtLoop { get; private set; } = false;

    /// <summary>
    /// Gets a value indicating whether the manager should stop at the next loop.
    /// </summary>
    public bool StopAtLoop { get; private set; } = false;

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
                    this.macroStack.Clear();
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

                // Grab from the stack, or go back to being paused
                if (!this.macroStack.TryPeek(out var macro))
                {
                    this.pausedWaiter.Reset();
                    continue;
                }

                this.State = LoopState.Running;
                if (await this.ProcessMacro(macro, token))
                {
                    this.macroStack.Pop().Dispose();
                }
            }
            catch (OperationCanceledException)
            {
                PluginLog.Verbose("Event loop has been cancelled");
                this.State = LoopState.Stopped;
                break;
            }
            catch (ObjectDisposedException)
            {
                PluginLog.Verbose("Event loop has been disposed");
                this.State = LoopState.Stopped;
                break;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Unhandled exception occurred");
                Service.ChatManager.PrintError("Peon has died unexpectedly.");
                this.macroStack.Clear();
                this.PlayErrorSound();
            }
        }
    }

    private async Task<bool> ProcessMacro(ActiveMacro macro, CancellationToken token, int attempt = 0)
    {
        MacroCommand? step = null;

        try
        {
            step = macro.GetCurrentStep();

            if (step == null)
                return true;

            await step.Execute(macro, token);
        }
        catch (GateComplete)
        {
            return true;
        }
        catch (MacroPause ex)
        {
            Service.ChatManager.PrintColor($"{ex.Message}", ex.Color);
            this.pausedWaiter.Reset();
            this.PlayErrorSound();
            return false;
        }
        catch (MacroActionTimeoutError ex)
        {
            var maxRetries = Service.Configuration.MaxTimeoutRetries;
            var message = $"Failure while running {step} (step {macro.StepIndex + 1}): {ex.Message}";
            if (attempt < maxRetries)
            {
                message += $", retrying ({attempt}/{maxRetries})";
                Service.ChatManager.PrintError(message);
                attempt++;
                return await this.ProcessMacro(macro, token, attempt);
            }
            else
            {
                Service.ChatManager.PrintError(message);
                this.pausedWaiter.Reset();
                this.PlayErrorSound();
                return false;
            }
        }
        catch (LuaScriptException ex)
        {
            Service.ChatManager.PrintError($"Failure while running script: {ex.Message}");
            this.pausedWaiter.Reset();
            this.PlayErrorSound();
            return false;
        }
        catch (MacroCommandError ex)
        {
            Service.ChatManager.PrintError($"Failure while running {step} (step {macro.StepIndex + 1}): {ex.Message}");
            this.pausedWaiter.Reset();
            this.PlayErrorSound();
            return false;
        }

        macro.NextStep();

        return false;
    }

    private void PlayErrorSound()
    {
        if (!Service.Configuration.NoisyErrors)
            return;

        var count = Service.Configuration.BeepCount;
        var frequency = Service.Configuration.BeepFrequency;
        var duration = Service.Configuration.BeepDuration;

        for (var i = 0; i < count; i++)
            Console.Beep(frequency, duration);
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
        => this.macroStack
            .Select(macro => (macro.Node.Name, macro.StepIndex + 1))
            .ToArray();

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
        {
            this.PauseAtLoop ^= true;
            this.StopAtLoop = false;
        }
        else
        {
            this.PauseAtLoop = false;
            this.StopAtLoop = false;
            this.pausedWaiter.Reset();
            Service.ChatManager.Clear();
        }
    }

    /// <summary>
    /// Pause at the next /loop.
    /// </summary>
    public void LoopCheckForPause()
    {
        if (this.PauseAtLoop)
        {
            this.Pause(false);
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
    /// Stop macro execution.
    /// </summary>
    /// <param name="stopAtLoop">Stop at the next loop instead.</param>
    public void Stop(bool stopAtLoop = false)
    {
        if (stopAtLoop)
        {
            this.PauseAtLoop = false;
            this.StopAtLoop ^= true;
        }
        else
        {
            this.PauseAtLoop = false;
            this.StopAtLoop = false;
            this.pausedWaiter.Set();
            this.macroStack.Clear();
            Service.ChatManager.Clear();
        }
    }

    /// <summary>
    /// Stop at the next /loop.
    /// </summary>
    public void LoopCheckForStop()
    {
        if (this.StopAtLoop)
        {
            this.Stop(false);
        }
    }

    /// <summary>
    /// Proceed to the next step.
    /// </summary>
    public void NextStep()
    {
        if (this.macroStack.TryPeek(out var macro))
            macro.NextStep();
    }

    /// <summary>
    /// Gets the contents of the current macro.
    /// </summary>
    /// <returns>Macro contents.</returns>
    public string[] CurrentMacroContent()
    {
        if (this.macroStack.TryPeek(out var result))
            return result.Steps.Select(s => s.ToString()).ToArray();

        return Array.Empty<string>();
    }

    /// <summary>
    /// Gets the executing line number of the current macro.
    /// </summary>
    /// <returns>Macro line number.</returns>
    public int CurrentMacroStep()
    {
        if (this.macroStack.TryPeek(out var result))
            return result.StepIndex;

        return 0;
    }
}
