using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using ClickLib;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace SomethingNeedDoing
{
    /// <summary>
    /// Manager that handles running macros.
    /// </summary>
    internal partial class MacroManager : IDisposable
    {
        private static readonly Regex RunMacroCommand = new(@"^/runmacro\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ActionCommand = new(@"^/(ac|action)\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WaitCommand = new(@"^/wait\s+(?<time>\d+(?:\.\d+)?)(?:-(?<maxtime>\d+(?:\.\d+)?))?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WaitAddonCommand = new(@"^/waitaddon\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RequireCommand = new(@"^/require\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SendCommand = new(@"^/send\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex TargetCommand = new(@"^/target\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ClickCommand = new(@"^/click\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex LoopCommand = new(@"^/loop(?: (?<count>\d+))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex WaitModifier = new(@"(?<modifier>\s*<wait\.(?<time>\d+(?:\.\d+)?)(?:-(?<maxtime>\d+(?:\.\d+)?))?>\s*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex UnsafeModifier = new(@"(?<modifier>\s*<unsafe>\s*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex MaxWaitModifier = new(@"(?<modifier>\s*<maxwait\.(?<time>\d+(?:\.\d+)?)>\s*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly CancellationTokenSource eventLoopTokenSource = new();
        private readonly List<ActiveMacro> runningMacros = new();
        private readonly ManualResetEvent pausedWaiter = new(true);
        private readonly ManualResetEvent loggedInWaiter = new(false);
        private readonly ManualResetEvent dataAvailableWaiter = new(false);
        private readonly List<string> craftingActionNames = new();
        private readonly Hook<EventFrameworkDelegate> eventFrameworkHook;

        private CraftingState craftingData = default;

        /// <summary>
        /// Initializes a new instance of the <see cref="MacroManager"/> class.
        /// </summary>
        public MacroManager()
        {
            Service.ClientState.Login += this.OnLogin;
            Service.ClientState.Logout += this.OnLogout;

            // Click.Initialize();

            this.PopulateCraftingActionNames();

            if (Service.ClientState.LocalPlayer != null)
                this.loggedInWaiter.Set();

            this.eventFrameworkHook = new Hook<EventFrameworkDelegate>(Service.Address.EventFrameworkFunctionAddress, this.EventFrameworkDetour);
            this.eventFrameworkHook.Enable();

            Task.Run(() => this.EventLoop(this.eventLoopTokenSource.Token));
        }

        private delegate IntPtr EventFrameworkDelegate(IntPtr a1, IntPtr a2, uint a3, ushort a4, IntPtr a5, IntPtr dataPtr, byte dataSize);

        /// <summary>
        /// The state of the macro manager.
        /// </summary>
        internal enum LoopState
        {
            /// <summary>
            /// Not logged in.
            /// </summary>
            NotLoggedIn,

            /// <summary>
            /// Waiting.
            /// </summary>
            Waiting,

            /// <summary>
            /// Running.
            /// </summary>
            Running,

            /// <summary>
            /// Paused.
            /// </summary>
            Paused,

            /// <summary>
            /// Stopped.
            /// </summary>
            Stopped,
        }

        /// <summary>
        /// Gets the state of the macro manager.
        /// </summary>
        public LoopState State { get; private set; } = LoopState.Waiting;

        /// <inheritdoc/>
        public void Dispose()
        {
            Service.ClientState.Login -= this.OnLogin;
            Service.ClientState.Logout -= this.OnLogout;

            this.eventLoopTokenSource.Cancel();
            this.eventLoopTokenSource.Dispose();
            this.eventFrameworkHook.Dispose();
            this.loggedInWaiter.Dispose();
            this.pausedWaiter.Dispose();
        }

        private void OnEventFrameworkDetour(IntPtr dataPtr, byte dataSize)
        {
            if (dataSize >= 4)
            {
                var dataType = (ActionCategory)Marshal.ReadInt32(dataPtr);
                if (dataType == ActionCategory.Action || dataType == ActionCategory.CraftAction)
                {
                    this.craftingData = Marshal.PtrToStructure<CraftingState>(dataPtr);
                    this.dataAvailableWaiter.Set();
                }
            }
        }

        private IntPtr EventFrameworkDetour(IntPtr a1, IntPtr a2, uint a3, ushort a4, IntPtr a5, IntPtr dataPtr, byte dataSize)
        {
            try
            {
                this.OnEventFrameworkDetour(dataPtr, dataSize);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Don't crash the game.");
            }

            return this.eventFrameworkHook.Original(a1, a2, a3, a4, a5, dataPtr, dataSize);
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

        private void EventLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!this.loggedInWaiter.WaitOne(0))
                    {
                        this.State = LoopState.NotLoggedIn;
                    }

                    this.loggedInWaiter.WaitOne();

                    if (!this.pausedWaiter.WaitOne(0))
                    {
                        this.State = this.runningMacros.Count == 0 ? LoopState.Waiting : LoopState.Paused;
                    }

                    this.pausedWaiter.WaitOne();

                    var macro = this.runningMacros.FirstOrDefault();
                    if (macro == default(ActiveMacro))
                    {
                        this.pausedWaiter.Reset();
                        continue;
                    }

                    this.State = LoopState.Running;
                    if (this.ProcessMacro(macro, token))
                    {
                        this.runningMacros.Remove(macro);
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
                    this.runningMacros.Clear();
                }
            }
        }

        private bool ProcessMacro(ActiveMacro macro, CancellationToken token)
        {
            var step = macro.GetCurrentStep();

            if (step == null)
                return true;

            var wait = this.ExtractWait(ref step);

            try
            {
                var command = step.ToLower().Split(' ').First();
                switch (command)
                {
                    case "/ac":
                    case "/action":
                        this.ProcessActionCommand(step, token, ref wait);
                        break;
                    case "/require":
                        this.ProcessRequireCommand(step, token);
                        break;
                    case "/runmacro":
                        this.ProcessRunMacroCommand(step);
                        break;
                    case "/wait":
                        this.ProcessWaitCommand(step, token);
                        break;
                    case "/waitaddon":
                        this.ProcessWaitAddonCommand(step, token);
                        break;
                    case "/send":
                        this.ProcessSendCommand(step);
                        break;
                    case "/target":
                        this.ProcessTargetCommand(step);
                        break;
                    case "/click":
                        this.ProcessClickCommand(step);
                        break;
                    case "/loop":
                        this.ProcessLoopCommand(step, macro);
                        break;
                    default:
                        Service.ChatManager.SendMessage(step);
                        break;
                }
            }
            catch (EffectNotPresentError ex)
            {
                Service.ChatManager.PrintError($"{ex.Message}: Failure while running {step} (step {macro.StepIndex + 1})");
                this.pausedWaiter.Reset();
                return false;
            }
            catch (EventFrameworkTimeoutError ex)
            {
                Service.ChatManager.PrintError($"{ex.Message}: Failure while running {step} (step {macro.StepIndex + 1})");
                this.pausedWaiter.Reset();
                return false;
            }
            catch (InvalidMacroOperationException ex)
            {
                Service.ChatManager.PrintError($"{ex.Message}: Failure while running {step} (step {macro.StepIndex + 1})");
                this.pausedWaiter.Reset();
                return true;
            }

            if (wait.TotalSeconds > 0)
            {
                Task.Delay(wait, token).Wait(token);
            }

            macro.StepIndex++;

            return false;
        }

        private void ProcessRunMacroCommand(string step)
        {
            var match = RunMacroCommand.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var macroName = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' });
            var macroNode = Service.Configuration.GetAllNodes().FirstOrDefault(macro => macro.Name == macroName) as MacroNode;
            if (macroNode == default)
                throw new InvalidMacroOperationException("Unknown macro");

            this.runningMacros.Insert(0, new ActiveMacro(macroNode));
        }

        private void ProcessActionCommand(string step, CancellationToken token, ref TimeSpan wait)
        {
            var unsafeAction = this.ExtractUnsafe(ref step);

            var match = ActionCommand.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var actionName = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' }).ToLower();

            if (this.IsCraftingAction(actionName))
            {
                this.dataAvailableWaiter.Reset();

                Service.ChatManager.SendMessage(step);

                Task.Delay(wait, token).Wait(token);
                wait = TimeSpan.Zero;

                if (!unsafeAction && !this.dataAvailableWaiter.WaitOne(5000))
                    throw new EventFrameworkTimeoutError("Did not receive a response from the game");
            }
            else
            {
                Service.ChatManager.SendMessage(step);
            }
        }

        private void ProcessWaitCommand(string step, CancellationToken token)
        {
            var match = WaitCommand.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var waitTime = TimeSpan.Zero;
            var waitMatch = match.Groups["time"];
            if (waitMatch.Success && double.TryParse(waitMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double seconds))
            {
                waitTime = TimeSpan.FromSeconds(seconds);
                // PluginLog.Debug($"Wait is {waitTime.TotalMilliseconds}ms");
            }

            var maxWaitMatch = match.Groups["maxtime"];
            if (maxWaitMatch.Success && double.TryParse(maxWaitMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double maxSeconds))
            {
                var rand = new Random();

                var maxWaitTime = TimeSpan.FromSeconds(maxSeconds);
                var diff = rand.Next((int)maxWaitTime.TotalMilliseconds - (int)waitTime.TotalMilliseconds);

                waitTime = TimeSpan.FromMilliseconds((int)waitTime.TotalMilliseconds + diff);
                // PluginLog.Debug($"Wait (variable) is now {waitTime.TotalMilliseconds}ms");
            }

            Task.Delay(waitTime, token).Wait(token);
        }

        private void ProcessWaitAddonCommand(string step, CancellationToken token)
        {
            var maxwait = this.ExtractMaxWait(ref step, 5000);

            var match = WaitAddonCommand.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var addonPtr = IntPtr.Zero;
            var addonName = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' });

            var isVisible = this.LinearWaitFor(
                500,
                Convert.ToInt32(maxwait.TotalMilliseconds),
                () =>
                {
                    addonPtr = Service.GameGui.GetAddonByName(addonName, 1);
                    if (addonPtr != IntPtr.Zero)
                    {
                        unsafe
                        {
                            var addon = (AtkUnitBase*)addonPtr;
                            return addon->IsVisible && addon->UldManager.LoadedState == 3;
                        }
                    }

                    return false;
                },
                token);

            if (addonPtr == IntPtr.Zero)
                throw new InvalidMacroOperationException("Could not find Addon");

            if (!isVisible)
                throw new InvalidMacroOperationException("Addon not visible");
        }

        private void ProcessRequireCommand(string step, CancellationToken token)
        {
            var maxwait = this.ExtractMaxWait(ref step, 1000);

            var match = RequireCommand.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var effectName = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' }).ToLower();

            var sheet = Service.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Status>()!;
            var effectIDs = sheet.Where(row => row.Name.RawString.ToLower() == effectName).Select(row => row.RowId).ToList();

            var hasEffect = this.LinearWaitFor(
                250,
                Convert.ToInt32(maxwait.TotalMilliseconds),
                () => Service.ClientState.LocalPlayer!.StatusList.Select(se => se.StatusId).ToList().Intersect(effectIDs).Any(),
                token);

            if (!hasEffect)
                throw new EffectNotPresentError("Effect not present");
        }

        private void ProcessSendCommand(string step)
        {
            var match = SendCommand.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var vkName = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' }).ToLower();

            if (Enum.TryParse<VirtualKey>(vkName, true, out var vkCode))
            {
                Keyboard.Send(vkCode);
            }
            else
            {
                throw new InvalidMacroOperationException($"Invalid virtual key");
            }
        }

        private void ProcessTargetCommand(string step)
        {
            var match = TargetCommand.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var actorName = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' }).ToLower();

            GameObject npc;
            try
            {
                npc = Service.ObjectTable.Where(actor => actor.Name.TextValue.ToLower() == actorName).First();
            }
            catch (InvalidOperationException)
            {
                throw new InvalidMacroOperationException($"Unknown actor");
            }

            if (npc != null)
            {
                Service.TargetManager.SetTarget(npc);
            }
        }

        private void ProcessClickCommand(string step)
        {
            var match = ClickCommand.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var name = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' }).ToLower();

            try
            {
                Click.SendClick(name);
            }
            catch (InvalidClickException ex)
            {
                PluginLog.Error(ex, $"Error while performing {name} click");
                throw new InvalidMacroOperationException($"Click error");
            }
        }

        private void ProcessLoopCommand(string step, ActiveMacro macro)
        {
            var match = LoopCommand.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var countMatch = match.Groups["count"];
            if (!countMatch.Success)
            {
                macro.StepIndex = -1;
            }
            else if (countMatch.Success && int.TryParse(countMatch.Value, out var loopMax) && macro.LoopCount < loopMax)
            {
                macro.StepIndex = -1;
                macro.LoopCount++;
            }
            else
            {
                macro.LoopCount = 0;
            }
        }

        private bool LinearWaitFor(int waitInterval, int maxWait, Func<bool> action, CancellationToken token)
        {
            var totalWait = 0;
            while (true)
            {
                if (action())
                    return true;

                totalWait += waitInterval;
                if (totalWait > maxWait)
                    return false;

                Task.Delay(waitInterval, token).Wait(token);
            }
        }

        private TimeSpan ExtractWait(ref string command)
        {
            var match = WaitModifier.Match(command);

            var waitTime = TimeSpan.Zero;

            if (!match.Success)
                return waitTime;

            var modifier = match.Groups["modifier"].Value;
            command = command.Replace(modifier, " ").Trim();

            var waitMatch = match.Groups["time"];
            if (waitMatch.Success && double.TryParse(waitMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double seconds))
            {
                waitTime = TimeSpan.FromSeconds(seconds);
            }

            var maxWaitMatch = match.Groups["maxtime"];
            if (maxWaitMatch.Success && double.TryParse(maxWaitMatch.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double maxSeconds))
            {
                var rand = new Random();

                var maxWaitTime = TimeSpan.FromSeconds(maxSeconds);
                var diff = rand.Next((int)maxWaitTime.TotalMilliseconds - (int)waitTime.TotalMilliseconds);

                waitTime = TimeSpan.FromMilliseconds((int)waitTime.TotalMilliseconds + diff);
            }

            return waitTime;
        }

        private bool ExtractUnsafe(ref string command)
        {
            var match = UnsafeModifier.Match(command);
            if (match.Success)
            {
                var modifier = match.Groups["modifier"].Value;
                command = command.Replace(modifier, " ").Trim();
                return true;
            }

            return false;
        }

        private TimeSpan ExtractMaxWait(ref string command, float defaultMillis)
        {
            var match = MaxWaitModifier.Match(command);
            if (match.Success)
            {
                var modifier = match.Groups["modifier"].Value;
                var waitTime = match.Groups["time"].Value;
                command = command.Replace(modifier, " ").Trim();
                if (double.TryParse(waitTime, NumberStyles.Any, CultureInfo.InvariantCulture, out double seconds))
                {
                    return TimeSpan.FromSeconds(seconds);
                }
            }

            return TimeSpan.FromMilliseconds(defaultMillis);
        }

        private void PopulateCraftingActionNames()
        {
            var actions = Service.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>()!;
            foreach (var row in actions)
            {
                var job = row?.ClassJob?.Value?.ClassJobCategory?.Value;
                if (job != null && (job.CRP || job.BSM || job.ARM || job.GSM || job.LTW || job.WVR || job.ALC || job.CUL))
                {
                    var name = row!.Name.RawString.Trim(new char[] { ' ', '"', '\'' }).ToLower();
                    if (!this.craftingActionNames.Contains(name))
                    {
                        this.craftingActionNames.Add(name);
                    }
                }
            }

            var craftActions = Service.DataManager.GetExcelSheet<Lumina.Excel.GeneratedSheets.CraftAction>()!;
            foreach (var row in craftActions)
            {
                var name = row.Name.RawString.Trim(new char[] { ' ', '"', '\'' }).ToLower();
                if (name.Length > 0 && !this.craftingActionNames.Contains(name))
                {
                    this.craftingActionNames.Add(name);
                }
            }
        }

        private bool IsCraftingAction(string name) => this.craftingActionNames.Contains(name.Trim(new char[] { ' ', '"', '\'' }).ToLower());

        private class ActiveMacro
        {
            public ActiveMacro(MacroNode node)
                : this(node, null)
            {
            }

            public ActiveMacro(MacroNode node, ActiveMacro? parent)
            {
                this.Node = node;
                this.Parent = parent;
                this.Steps = node.Contents.Split(new[] { "\n", "\r", "\n\r" }, StringSplitOptions.RemoveEmptyEntries).Where(line => !line.StartsWith("#")).ToArray();
            }

            public MacroNode Node { get; private set; }

            public ActiveMacro? Parent { get; private set; }

            public string[] Steps { get; private set; }

            public int StepIndex { get; set; }

            public int LoopCount { get; set; }

            public string? GetCurrentStep()
            {
                if (this.StepIndex >= this.Steps.Length)
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
        /// Gets the amount of macros currently executing.
        /// </summary>
        public int MacroCount => this.runningMacros.Count;

        /// <summary>
        /// Gets the name and currently executing line of each active macro.
        /// </summary>
        public (string Name, int StepIndex)[] MacroStatus
            => this.runningMacros.Select(macro => (macro.Node.Name, macro.StepIndex + 1)).ToArray();

        /// <summary>
        /// Run a macro.
        /// </summary>
        /// <param name="node">Macro to run.</param>
        public void RunMacro(MacroNode node)
        {
            this.runningMacros.Add(new ActiveMacro(node));
            this.pausedWaiter.Set();
        }

        /// <summary>
        /// Pause macro execution.
        /// </summary>
        public void Pause()
        {
            this.pausedWaiter.Reset();
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
            this.runningMacros.Clear();
        }

        /// <summary>
        /// Gets the contents of the current macro.
        /// </summary>
        /// <returns>Macro contents.</returns>
        public string[] CurrentMacroContent()
        {
            if (this.runningMacros.Count == 0)
                return Array.Empty<string>();

            return this.runningMacros.First().Steps.ToArray();
        }

        /// <summary>
        /// Gets the executing line number of the current macro.
        /// </summary>
        /// <returns>Macro line number.</returns>
        public int CurrentMacroStep()
        {
            if (this.runningMacros.Count == 0)
                return 0;

            return this.runningMacros.First().StepIndex;
        }
    }
}
