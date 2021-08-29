using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using System.IO;
using System.Reflection;

namespace SomethingNeedDoing
{
    public sealed class SomethingNeedDoingPlugin : IDalamudPlugin
    {
        public string Name => "Something Need Doing";
        public string Command => "/pcraft";

        internal SomethingNeedDoingConfiguration Configuration;
        internal PluginAddressResolver Address;
        internal ChatManager ChatManager;
        internal MacroManager MacroManager;

        private readonly PluginUI PluginUi;

        internal DalamudPluginInterface Interface { get; private set; }
        internal ChatGui ChatGui { get; private set; }
        internal ClientState ClientState { get; private set; }
        internal CommandManager CommandManager { get; private set; }
        internal DataManager DataManager { get; private set; }
        internal Framework Framework { get; private set; }
        internal GameGui GameGui { get; private set; }
        internal ObjectTable ObjectTable { get; private set; }
        internal TargetManager TargetManager { get; private set; }

        public SomethingNeedDoingPlugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ChatGui chatGui,
            [RequiredVersion("1.0")] ClientState clientState,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] DataManager dataManager,
            [RequiredVersion("1.0")] Framework framework,
            [RequiredVersion("1.0")] GameGui gameGui,
            [RequiredVersion("1.0")] ObjectTable objectTable,
            [RequiredVersion("1.0")] TargetManager targetManager)
        {
            Interface = pluginInterface;
            ChatGui = chatGui;
            ClientState = clientState;
            CommandManager = commandManager;
            DataManager = dataManager;
            Framework = framework;
            GameGui = gameGui;
            ObjectTable = objectTable;
            TargetManager = targetManager;

            Configuration = SomethingNeedDoingConfiguration.Load(pluginInterface, Name.Replace(" ", ""));

            CommandManager.AddHandler(Command, new CommandInfo(OnChatCommand)
            {
                HelpMessage = "Open a window to edit various settings.",
                ShowInHelp = true
            });

            Address = new PluginAddressResolver();
            Address.Setup();

            ChatManager = new ChatManager(this);
            MacroManager = new MacroManager(this);
            PluginUi = new PluginUI(this);
        }

        public void Dispose()
        {
            CommandManager.RemoveHandler(Command);
            ChatManager.Dispose();
            MacroManager.Dispose();
            PluginUi.Dispose();
        }

        internal void SaveConfiguration() => Interface.SavePluginConfig(Configuration);

        private void OnChatCommand(string command, string arguments)
        {
            PluginUi.Open();
        }

        internal byte[] ReadResourceFile(params string[] filePathSegments)
        {
            var assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var resourceFilePath = Path.Combine(assemblyFolder, Path.Combine(filePathSegments));
            return File.ReadAllBytes(resourceFilePath);
        }

        internal byte[] ReadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using BinaryReader reader = new(stream);
            return reader.ReadBytes((int)stream.Length);
        }
    }
}
