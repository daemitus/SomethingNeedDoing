using ClickLib;
using Dalamud.Hooking;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace SomethingNeedDoing
{
    internal enum LoopState
    {
        NotLoggedIn,
        Waiting,
        Running,
        Paused,
        Stopped,
    }

    internal class MacroManager : IDisposable
    {
        private readonly SomethingNeedDoingPlugin plugin;
        private readonly CancellationTokenSource EventLoopTokenSource = new();
        private readonly List<ActiveMacro> RunningMacros = new();
        private readonly ManualResetEvent PausedWaiter = new(true);
        private readonly ManualResetEvent LoggedInWaiter = new(false);
        private readonly ManualResetEvent DataAvailableWaiter = new(false);
        private readonly List<string> CraftingActionNames = new();
        private CraftingData CraftingData = default;

        private delegate IntPtr EventFrameworkDelegate(IntPtr a1, IntPtr a2, uint a3, ushort a4, IntPtr a5, IntPtr dataPtr, byte dataSize);

        private readonly Hook<EventFrameworkDelegate> EventFrameworkHook;

        public LoopState LoopState { get; private set; } = LoopState.Waiting;

        public MacroManager(SomethingNeedDoingPlugin plugin)
        {
            this.plugin = plugin;
            this.plugin.Interface.ClientState.OnLogin += ClientState_OnLogin;
            this.plugin.Interface.ClientState.OnLogout += ClientState_OnLogout;

            Click.Initialize(plugin.Interface);

            PopulateCraftingActionNames();

            if (plugin.Interface.ClientState.LocalPlayer != null)
                LoggedInWaiter.Set();

            EventFrameworkHook = new Hook<EventFrameworkDelegate>(plugin.Address.EventFrameworkFunctionAddress, new EventFrameworkDelegate(EventFrameworkDetour), this);
            EventFrameworkHook.Enable();

            Task.Run(() => EventLoop(EventLoopTokenSource.Token));
        }

        public void Dispose()
        {
            plugin.Interface.ClientState.OnLogin -= ClientState_OnLogin;
            plugin.Interface.ClientState.OnLogout -= ClientState_OnLogout;

            EventLoopTokenSource.Cancel();
            EventLoopTokenSource.Dispose();
            EventFrameworkHook.Dispose();
            LoggedInWaiter.Dispose();
            PausedWaiter.Dispose();
        }

        private IntPtr EventFrameworkDetour(IntPtr a1, IntPtr a2, uint a3, ushort a4, IntPtr a5, IntPtr dataPtr, byte dataSize)
        {
            try
            {
                if (dataSize >= 4)
                {
                    var dataType = (ActionCategory)Marshal.ReadInt32(dataPtr);
                    if (dataType == ActionCategory.Action || dataType == ActionCategory.CraftAction)
                    {
                        CraftingData = Marshal.PtrToStructure<CraftingData>(dataPtr);
                        DataAvailableWaiter.Set();
                    }
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Don't crash the game");
            }

            return EventFrameworkHook.Original(a1, a2, a3, a4, a5, dataPtr, dataSize);
        }

        private void ClientState_OnLogin(object sender, EventArgs e)
        {
            LoggedInWaiter.Set();
            LoopState = LoopState.Waiting;
        }

        private void ClientState_OnLogout(object sender, EventArgs e)
        {
            LoggedInWaiter.Reset();
            LoopState = LoopState.NotLoggedIn;
        }

        private void EventLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!LoggedInWaiter.WaitOne(0))
                    {
                        LoopState = LoopState.NotLoggedIn;
                    }
                    LoggedInWaiter.WaitOne();

                    if (!PausedWaiter.WaitOne(0))
                    {
                        LoopState = RunningMacros.Count == 0 ? LoopState.Waiting : LoopState.Paused;
                    }

                    PausedWaiter.WaitOne();

                    var macro = RunningMacros.FirstOrDefault();
                    if (macro == default(ActiveMacro))
                    {
                        PausedWaiter.Reset();
                        continue;
                    }

                    LoopState = LoopState.Running;
                    if (ProcessMacro(macro, token))
                    {
                        RunningMacros.Remove(macro);
                    }
                }
                catch (OperationCanceledException)
                {
                    PluginLog.Verbose("Event loop has stopped");
                    LoopState = LoopState.Stopped;
                    break;
                }
                catch (ObjectDisposedException)
                {
                    PluginLog.Verbose($"Event loop has stopped");
                    LoopState = LoopState.Stopped;
                    break;
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, "Unhandled exception occurred");
                    plugin.ChatManager.PrintError($"[SomethingNeedDoing] Peon has died unexpectedly.");
                    RunningMacros.Clear();
                }
            }
        }

        private bool ProcessMacro(ActiveMacro macro, CancellationToken token)
        {
            var step = macro.GetCurrentStep();

            if (step == null)
                return true;

            var wait = ExtractWait(ref step);

            try
            {
                var command = step.ToLower().Split(' ').First();
                switch (command)
                {
                    case "/ac":
                    case "/action":
                        ProcessActionCommand(step, token, ref wait);
                        break;
                    case "/require":
                        ProcessRequireCommand(step, token);
                        break;
                    case "/runmacro":
                        ProcessRunMacroCommand(step);
                        break;
                    case "/wait":
                        ProcessWaitCommand(step, token);
                        break;
                    case "/waitaddon":
                        ProcessWaitAddonCommand(step, token);
                        break;
                    case "/send":
                        ProcessSendCommand(step);
                        break;
                    case "/target":
                        ProcessTargetCommand(step);
                        break;
                    case "/click":
                        ProcessClickCommand(step);
                        break;
                    case "/loop":
                        ProcessLoopCommand(step, macro);
                        break;
                    default:
                        plugin.ChatManager.SendChatBoxMessage(step);
                        break;
                };
            }
            catch (InvalidMacroOperationException ex)
            {
                plugin.ChatManager.PrintError($"{ex.Message}: Failure while running {step} (step {macro.StepIndex + 1})");
                PausedWaiter.Reset();
                return true;
            }

            if (wait.TotalSeconds > 0)
            {
                Task.Delay(wait, token).Wait(token);
            }

            macro.StepIndex++;

            return false;
        }

        private readonly Regex RUNMACRO_COMMAND = new(@"^/runmacro\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex ACTION_COMMAND = new(@"^/(ac|action)\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex WAIT_COMMAND = new(@"^/wait\s+(?<time>\d+(?:\.\d+)?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex WAITADDON_COMMAND = new(@"^/waitaddon\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex REQUIRE_COMMAND = new(@"^/require\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex SEND_COMMAND = new(@"^/send\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex TARGET_COMMAND = new(@"^/target\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex CLICK_COMMAND = new(@"^/click\s+(?<name>.*?)\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex LOOP_COMMAND = new(@"^/loop(?: (?<count>\d+))?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex WAIT_MODIFIER = new(@"(?<modifier>\s*<wait\.(?<time>\d+(?:\.\d+)?)>\s*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex UNSAFE_MODIFIER = new(@"(?<modifier>\s*<unsafe>\s*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly Regex MAXWAIT_MODIFIER = new(@"(?<modifier>\s*<maxwait\.(?<time>\d+(?:\.\d+)?)>\s*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private void ProcessRunMacroCommand(string step)
        {
            var match = RUNMACRO_COMMAND.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var macroName = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' });
            var macroNode = plugin.Configuration.GetAllNodes().FirstOrDefault(macro => macro.Name == macroName) as MacroNode;
            if (macroNode == default(MacroNode))
                throw new InvalidMacroOperationException("Unknown macro");

            RunningMacros.Insert(0, new ActiveMacro(macroNode));
        }

        private void ProcessActionCommand(string step, CancellationToken token, ref TimeSpan wait)
        {
            var unsafeAction = ExtractUnsafe(ref step);

            var match = ACTION_COMMAND.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var actionName = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' }).ToLower();

            if (IsCraftingAction(actionName))
            {
                DataAvailableWaiter.Reset();

                plugin.ChatManager.SendChatBoxMessage(step);

                Task.Delay(wait, token).Wait(token);
                wait = TimeSpan.Zero;

                if (!unsafeAction && !DataAvailableWaiter.WaitOne(5000))
                    throw new InvalidMacroOperationException("Did not receive a response from the game");
            }
            else
            {
                plugin.ChatManager.SendChatBoxMessage(step);
            }
        }

        private void ProcessWaitCommand(string step, CancellationToken token)
        {
            var match = WAIT_COMMAND.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var waitTime = match.Groups["time"].Value;
            if (double.TryParse(waitTime, out double seconds))
            {
                var wait = TimeSpan.FromSeconds(seconds);
                Task.Delay(wait, token).Wait(token);
            }
        }

        private void ProcessWaitAddonCommand(string step, CancellationToken token)
        {
            var maxwait = ExtractMaxWait(ref step, 5000);

            var match = WAITADDON_COMMAND.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var addonPtr = IntPtr.Zero;
            var addonName = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' });

            var isVisible = LinearWaitFor(500, Convert.ToInt32(maxwait.TotalMilliseconds), token, () =>
            {
                addonPtr = plugin.Interface.Framework.Gui.GetUiObjectByName(addonName, 1);
                if (addonPtr != IntPtr.Zero)
                {
                    unsafe
                    {
                        return ((AtkUnitBase*)addonPtr)->IsVisible;
                    }
                }
                return false;
            });

            if (addonPtr == IntPtr.Zero)
                throw new InvalidMacroOperationException("Could not find Addon");

            if (!isVisible)
                throw new InvalidMacroOperationException("Addon not visible");
        }

        private void ProcessRequireCommand(string step, CancellationToken token)
        {
            var maxwait = ExtractMaxWait(ref step, 1000);

            var match = REQUIRE_COMMAND.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var effectName = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' }).ToLower();

            var sheet = plugin.Interface.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Status>();
            var effectIDs = sheet.Where(row => row.Name.RawString.ToLower() == effectName).Select(row => (short)row.RowId).ToList();

            var hasEffect = LinearWaitFor(250, Convert.ToInt32(maxwait.TotalMilliseconds), token,
                () => plugin.Interface.ClientState.LocalPlayer.StatusEffects.Select(se => se.EffectId).ToList().Intersect(effectIDs).Any());

            if (!hasEffect)
                throw new InvalidMacroOperationException("Effect not present");
        }

        private void ProcessSendCommand(string step)
        {
            var match = SEND_COMMAND.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var vkName = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' }).ToLower();

            var InputSimulator = new InputSimulator();
            var vkCode = (VirtualKeyCode)Enum.Parse(typeof(VirtualKeyCode), vkName, true);
            if (!Enum.IsDefined(typeof(VirtualKeyCode), vkCode))
            {
                throw new InvalidMacroOperationException($"Invalid virtual key");
            }
            else
            {
                InputSimulator.Keyboard.KeyPress(vkCode);
            }
        }

        private void ProcessTargetCommand(string step)
        {
            var match = TARGET_COMMAND.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var actorName = match.Groups["name"].Value.Trim(new char[] { ' ', '"', '\'' }).ToLower();
            Dalamud.Game.ClientState.Actors.Types.Actor npc = null;
            try
            {
                npc = plugin.Interface.ClientState.Actors.Where(actor => actor.Name.ToLower() == actorName).First();
            }
            catch (InvalidOperationException)
            {
                throw new InvalidMacroOperationException($"Unknown actor");
            }

            if (npc != null)
            {
                plugin.Interface.ClientState.Targets.SetCurrentTarget(npc);
            }
        }

        private void ProcessClickCommand(string step)
        {
            var match = CLICK_COMMAND.Match(step);
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
            var match = LOOP_COMMAND.Match(step);
            if (!match.Success)
                throw new InvalidMacroOperationException("Syntax error");

            var countMatch = match.Groups["count"];
            if (!countMatch.Success)
            {
                macro.StepIndex = -1;
            }
            else if (countMatch.Success && int.TryParse(countMatch.Value, out var count) && macro.LoopCount > count)
            {
                macro.StepIndex = -1;
                macro.LoopCount++;
            }
            else
            {
                macro.LoopCount = 0;
            }
        }

        private bool LinearWaitFor(int waitInterval, int maxWait, CancellationToken token, Func<bool> action)
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
            var match = WAIT_MODIFIER.Match(command);
            if (match.Success)
            {
                var modifier = match.Groups["modifier"].Value;
                var waitTime = match.Groups["time"].Value;
                command = command.Replace(modifier, " ").Trim();
                if (double.TryParse(waitTime, out double seconds))
                {
                    return TimeSpan.FromSeconds(seconds);
                }
            }
            return TimeSpan.FromSeconds(0);
        }

        private bool ExtractUnsafe(ref string command)
        {
            var match = UNSAFE_MODIFIER.Match(command);
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
            var match = MAXWAIT_MODIFIER.Match(command);
            if (match.Success)
            {
                var modifier = match.Groups["modifier"].Value;
                var waitTime = match.Groups["time"].Value;
                command = command.Replace(modifier, " ").Trim();
                if (double.TryParse(waitTime, out double seconds))
                {
                    return TimeSpan.FromSeconds(seconds);
                }
            }
            return TimeSpan.FromMilliseconds(defaultMillis);
        }

        private void PopulateCraftingActionNames()
        {
            var actions = plugin.Interface.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.Action>();
            foreach (var row in actions)
            {
                var job = row?.ClassJob?.Value?.ClassJobCategory?.Value;
                if (job != null && (job.CRP || job.BSM || job.ARM || job.GSM || job.LTW || job.WVR || job.ALC || job.CUL))
                {
                    var name = row.Name.RawString.Trim(new char[] { ' ', '"', '\'' }).ToLower();
                    if (!CraftingActionNames.Contains(name))
                    {
                        CraftingActionNames.Add(name);
                    }
                }
            }
            var craftActions = plugin.Interface.Data.GetExcelSheet<Lumina.Excel.GeneratedSheets.CraftAction>();
            foreach (var row in craftActions)
            {
                var name = row.Name.RawString.Trim(new char[] { ' ', '"', '\'' }).ToLower();
                if (name.Length > 0 && !CraftingActionNames.Contains(name))
                {
                    CraftingActionNames.Add(name);
                }
            }
        }

        private bool IsCraftingAction(string name) => CraftingActionNames.Contains(name.Trim(new char[] { ' ', '"', '\'' }).ToLower());

        #region public api

        public void RunMacro(MacroNode node)
        {
            RunningMacros.Add(new ActiveMacro(node));
            PausedWaiter.Set();
        }

        public void Pause()
        {
            PausedWaiter.Reset();
        }

        public void Resume()
        {
            PausedWaiter.Set();
        }

        public void Clear()
        {
            RunningMacros.Clear();
        }

        public int MacroCount => RunningMacros.Count;

        public (string, int)[] MacroStatus => RunningMacros.Select(macro => (macro.Node.Name, macro.StepIndex + 1)).ToArray();

        public string[] CurrentMacroContent()
        {
            if (RunningMacros.Count == 0)
                return new string[0];
            return (string[])RunningMacros.First().Steps.Clone();
        }

        public int CurrentMacroStep()
        {
            if (RunningMacros.Count == 0)
                return 0;
            return RunningMacros.First().StepIndex;
        }

        #endregion

        private class ActiveMacro
        {
            public MacroNode Node { get; private set; }

            public ActiveMacro Parent { get; private set; }

            public ActiveMacro(MacroNode node) : this(node, null) { }

            public ActiveMacro(MacroNode node, ActiveMacro parent)
            {
                Node = node;
                Parent = parent;
                Steps = node.Contents.Split(new[] { "\n", "\r", "\n\r" }, StringSplitOptions.RemoveEmptyEntries).Where(line => !line.StartsWith("#")).ToArray();
            }

            public string[] Steps { get; private set; }

            public int StepIndex { get; set; }

            public int LoopCount { get; set; }

            public string GetCurrentStep()
            {
                if (StepIndex >= Steps.Length)
                    return null;

                return Steps[StepIndex];
            }
        }
    }


}
