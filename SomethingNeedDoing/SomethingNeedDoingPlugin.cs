using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using System.IO;
using System.Reflection;

namespace SomethingNeedDoing
{
    public class SomethingNeedDoingPlugin : IDalamudPlugin
    {
        public string Name => "SomethingNeedDoing";
        public string Command => "/pcraft";

        internal SomethingNeedDoingConfiguration Configuration;
        internal PluginAddressResolver Address;
        internal DalamudPluginInterface Interface;
        internal ChatManager ChatManager;
        internal MacroManager MacroManager;

        private PluginUI PluginUi;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            Interface = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface), "DalamudPluginInterface cannot be null");
            Configuration = SomethingNeedDoingConfiguration.Load(pluginInterface, Name);

            Interface.CommandManager.AddHandler(Command, new CommandInfo(OnChatCommand)
            {
                HelpMessage = "Open a window to edit various settings.",
                ShowInHelp = true
            });

            Address = new PluginAddressResolver();
            Address.Setup(pluginInterface.TargetModuleScanner);
            ChatManager = new ChatManager(this);
            MacroManager = new MacroManager(this);
            PluginUi = new PluginUI(this);
        }

        public void Dispose()
        {
            Interface.CommandManager.RemoveHandler(Command);
            ChatManager.Dispose();
            MacroManager.Dispose();
            PluginUi.Dispose();
        }

        private string _AssemblyLocation;
        public string AssemblyLocation
        {
            get { return _AssemblyLocation ??= Assembly.GetExecutingAssembly().Location; }
            set { _AssemblyLocation = value; }
        }

        internal void SaveConfiguration() => Interface.SavePluginConfig(Configuration);

        private void OnChatCommand(string command, string arguments)
        {
            PluginUi.Open();
        }

        internal byte[] ReadResourceFile(params string[] filePathSegments)
        {
            var assemblyFolder = Path.GetDirectoryName(AssemblyLocation);
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
