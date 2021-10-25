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

        /// <summary>
        /// Initializes a new instance of the <see cref="SomethingNeedDoingPlugin"/> class.
        /// </summary>
        /// <param name="pluginInterface">Dalamud plugin interface.</param>
        public SomethingNeedDoingPlugin(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();

            ClickLib.Click.Initialize();

            Service.Configuration = SomethingNeedDoingConfiguration.Load(pluginInterface.ConfigDirectory);
            Service.Address = new PluginAddressResolver();
            Service.Address.Setup();

            Service.ChatManager = new ChatManager();
            Service.MacroManager = new MacroManager();

            this.macroWindow = new();
            this.windowSystem = new("SomethingNeedDoing");
            this.windowSystem.AddWindow(this.macroWindow);

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

            this.windowSystem.RemoveAllWindows();

            Service.MacroManager.Dispose();
            Service.ChatManager.Dispose();
        }

        /// <summary>
        /// Save the plugin configuration.
        /// </summary>
        internal void SaveConfiguration() => Service.Interface.SavePluginConfig(Service.Configuration);

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
