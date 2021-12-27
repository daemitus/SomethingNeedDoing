using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using SomethingNeedDoing.Interface;
using SomethingNeedDoing.Managers;
using System.Linq;

namespace SomethingNeedDoing
{
    /// <summary>
    /// Main plugin implementation.
    /// </summary>
    public sealed class SomethingNeedDoingPlugin : IDalamudPlugin
    {
        private const string Command = "/pcraft";

        private readonly WindowSystem windowSystem;
        private readonly MacroWindow macroWindow;
        private readonly HelpWindow helpWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="SomethingNeedDoingPlugin"/> class.
        /// </summary>
        /// <param name="pluginInterface">Dalamud plugin interface.</param>
        public SomethingNeedDoingPlugin(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();

            ClickLib.Click.Initialize();

            Service.Plugin = this;
            Service.Configuration = SomethingNeedDoingConfiguration.Load(pluginInterface.ConfigDirectory);
            Service.Address = new PluginAddressResolver();
            Service.Address.Setup();

            Service.ChatManager = new ChatManager();
            Service.EventFrameworkManager = new EventFrameworkManager();
            Service.MacroManager = new MacroManager();

            this.macroWindow = new();
            this.helpWindow = new();
            this.windowSystem = new("SomethingNeedDoing");
            this.windowSystem.AddWindow(this.macroWindow);
            this.windowSystem.AddWindow(this.helpWindow);

            Service.Interface.UiBuilder.Draw += this.windowSystem.Draw;
            Service.Interface.UiBuilder.OpenConfigUi += this.OnOpenConfigUi;
            Service.CommandManager.AddHandler(Command, new CommandInfo(this.OnChatCommand)
            {
                HelpMessage = "Open a window to edit various settings.",
                ShowInHelp = true,
            });
        }

        /// <inheritdoc/>
        public string Name => "Something Need Doing";

        /// <inheritdoc/>
        public void Dispose()
        {
            Service.CommandManager.RemoveHandler(Command);
            Service.Interface.UiBuilder.OpenConfigUi -= this.OnOpenConfigUi;
            Service.Interface.UiBuilder.Draw -= this.windowSystem.Draw;

            this.windowSystem?.RemoveAllWindows();

            Service.MacroManager?.Dispose();
            Service.EventFrameworkManager?.Dispose();
            Service.ChatManager?.Dispose();
        }

        /// <summary>
        /// Open the help menu.
        /// </summary>
        internal void OpenHelpWindow()
        {
            this.helpWindow.IsOpen = true;
        }

        private void OnOpenConfigUi()
        {
            this.macroWindow.Toggle();
        }

        private void OnChatCommand(string command, string arguments)
        {
            arguments = arguments.Trim();

            if (arguments == string.Empty)
            {
                this.macroWindow.Toggle();
                return;
            }
            else if (arguments.StartsWith("run "))
            {
                var macroName = arguments[4..].Trim().Trim('"');
                var nodes = Service.Configuration.GetAllNodes()
                    .OfType<MacroNode>()
                    .Where(node => node.Name.Trim() == macroName)
                    .ToArray();

                if (nodes.Length == 0)
                {
                    Service.ChatManager.PrintError("No macros match that name");
                    return;
                }
                else if (nodes.Length > 1)
                {
                    Service.ChatManager.PrintError("More than one macro matches that name");
                    return;
                }
                else
                {
                    var node = nodes[0];
                    Service.ChatManager.PrintMessage($"Running macro \"{macroName}\"");
                    Service.MacroManager.EnqueueMacro(node);
                    return;
                }
            }
            else if (arguments.StartsWith("pause"))
            {
                Service.ChatManager.PrintMessage("Pausing");
                Service.MacroManager.Pause();
                return;
            }
            else if (arguments.StartsWith("resume"))
            {
                Service.ChatManager.PrintMessage("Resuming");
                Service.MacroManager.Resume();
                return;
            }
            else if (arguments.StartsWith("stop"))
            {
                Service.ChatManager.PrintMessage($"Stopping");
                Service.MacroManager.Clear();
                return;
            }
            else if (arguments.StartsWith("help"))
            {
                this.OpenHelpWindow();
                return;
            }
        }
    }
}
