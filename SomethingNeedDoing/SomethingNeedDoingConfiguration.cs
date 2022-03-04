using System.Collections.Generic;
using System.IO;
using System.Linq;

using Dalamud.Configuration;
using Newtonsoft.Json;

namespace SomethingNeedDoing;

/// <summary>
/// Plugin configuration.
/// </summary>
public class SomethingNeedDoingConfiguration : IPluginConfiguration
{
    /// <summary>
    /// Gets or sets the configuration version.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets the root folder.
    /// </summary>
    public FolderNode RootFolder { get; private set; } = new FolderNode { Name = "/" };

    /// <summary>
    /// Gets or sets a value indicating whether to skip craft actions when not crafting.
    /// </summary>
    public bool CraftSkip { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to intelligently wait for crafting actions to complete instead of using wait modifiers.
    /// </summary>
    public bool SmartWait { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to skip quality increasing actions when at 100% HQ chance.
    /// </summary>
    public bool QualitySkip { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to count the /loop number as the total iterations, rather than the amount to loop.
    /// </summary>
    public bool LoopTotal { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to always echo /loop commands.
    /// </summary>
    public bool LoopEcho { get; set; } = false;

    /// <summary>
    /// Loads the configuration.
    /// </summary>
    /// <param name="configDirectory">Configuration directory.</param>
    /// <returns>A configuration.</returns>
    internal static SomethingNeedDoingConfiguration Load(DirectoryInfo configDirectory)
    {
        var pluginConfigPath = new FileInfo(Path.Combine(configDirectory.Parent!.FullName, $"SomethingNeedDoing.json"));

        if (!pluginConfigPath.Exists)
            return new SomethingNeedDoingConfiguration();

        var data = File.ReadAllText(pluginConfigPath.FullName);
        var conf = JsonConvert.DeserializeObject<SomethingNeedDoingConfiguration>(data);
        return conf ?? new SomethingNeedDoingConfiguration();
    }

    /// <summary>
    /// Save the plugin configuration.
    /// </summary>
    internal void Save() => Service.Interface.SavePluginConfig(this);

    /// <summary>
    /// Get all nodes in the tree.
    /// </summary>
    /// <returns>All the nodes.</returns>
    internal IEnumerable<INode> GetAllNodes()
    {
        return new INode[] { this.RootFolder }.Concat(this.GetAllNodes(this.RootFolder.Children));
    }

    /// <summary>
    /// Gets all the nodes in this subset of the tree.
    /// </summary>
    /// <param name="nodes">Nodes to search.</param>
    /// <returns>The nodes in the tree.</returns>
    internal IEnumerable<INode> GetAllNodes(IEnumerable<INode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            if (node is FolderNode folder)
            {
                var childNodes = this.GetAllNodes(folder.Children);
                foreach (var childNode in childNodes)
                {
                    yield return childNode;
                }
            }
        }
    }

    /// <summary>
    /// Tries to find the parent of a node.
    /// </summary>
    /// <param name="node">Node to check.</param>
    /// <param name="parent">Parent of the node or null.</param>
    /// <returns>A value indicating whether the parent was found.</returns>
    internal bool TryFindParent(INode node, out FolderNode? parent)
    {
        foreach (var candidate in this.GetAllNodes())
        {
            if (candidate is FolderNode folder && folder.Children.Contains(node))
            {
                parent = folder;
                return true;
            }
        }

        parent = null;
        return false;
    }
}
