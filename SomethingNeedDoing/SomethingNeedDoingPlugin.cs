using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using SomethingNeedDoing.Interface;
using SomethingNeedDoing.Managers;

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
            this.macroWindow.Toggle();
        }
    }
}
