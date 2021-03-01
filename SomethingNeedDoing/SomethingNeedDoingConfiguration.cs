using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SomethingNeedDoing
{
    public class SomethingNeedDoingConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public FolderNode RootFolder { get; private set; } = new FolderNode { Name = "/" };

        public float CustomFontSize { get; set; } = 15.0f;

        public static SomethingNeedDoingConfiguration Load(DalamudPluginInterface pluginInterface, string pluginName)
        {
            var pluginConfigs = (PluginConfigurations)pluginInterface.GetType()
                .GetField("configs", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(pluginInterface);
            var configDirectory = (DirectoryInfo)pluginConfigs.GetType()
                .GetField("configDirectory", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(pluginConfigs);
            var pluginConfigPath = new FileInfo(Path.Combine(configDirectory.FullName, $"{pluginName}.json"));
            if (!pluginConfigPath.Exists)
                return new SomethingNeedDoingConfiguration();
            else
                return JsonConvert.DeserializeObject<SomethingNeedDoingConfiguration>(File.ReadAllText(pluginConfigPath.FullName));
        }

        public IEnumerable<INode> GetAllNodes()
        {
            return new INode[] { RootFolder }.Concat(GetAllNodes(RootFolder.Children));
        }

        public IEnumerable<INode> GetAllNodes(IEnumerable<INode> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;
                if (node is FolderNode)
                {
                    var children = (node as FolderNode).Children;
                    foreach (var childNode in GetAllNodes(children))
                    {
                        yield return childNode;
                    }
                }
            }
        }

        public bool TryFindParent(INode node, out FolderNode parent)
        {
            foreach (var candidate in GetAllNodes())
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

    public interface INode
    {
        public string Name { get; set; }
    }

    public class MacroNode : INode
    {
        public string Name { get; set; }

        public string Contents { get; set; } = "";
    }

    public class FolderNode : INode
    {
        public string Name { get; set; }

        [JsonProperty(ItemConverterType = typeof(ConcreteNodeConverter))]
        public List<INode> Children { get; } = new List<INode>();
    }

    public class ConcreteNodeConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;
        public override bool CanConvert(Type objectType) => objectType == typeof(INode);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var jType = jObject["$type"].Value<string>();

            if (jType == SimpleName(typeof(MacroNode)))
            {
                var obj = new MacroNode();
                serializer.Populate(jObject.CreateReader(), obj);
                return obj;
            }
            else if (jType == SimpleName(typeof(FolderNode)))
            {
                var obj = new FolderNode();
                serializer.Populate(jObject.CreateReader(), obj);
                return obj;
            }
            else
            {
                throw new NotSupportedException($"Node type \"{jType}\" is not supported.");
            }
        }

        private string SimpleName(Type type)
        {
            return $"{type.FullName}, {type.Assembly.GetName().Name}";
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
    }

    /*
    public class SomethingNeedDoingConfiguration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public Node RootFolder { get; private set; } = Node.CreateFolder($"/##Root-{Guid.NewGuid()}");

        public float CustomFontSize { get; set; } = 13.0f;

        public IEnumerable<Node> GetAllNodes()
        {
            return new Node[] { RootFolder }.Concat(GetAllNodes(RootFolder.Children));
        }

        public IEnumerable<Node> GetAllNodes(IEnumerable<Node> nodes)
        {
            var children = nodes.SelectMany(_ => GetAllNodes(_.Children));
            return nodes.Concat(children);
        }

        public bool TryFindParent(Node node, out Node parent)
        {
            foreach (var candidate in GetAllNodes())
            {
                if (candidate.Children.Contains(node))
                {
                    parent = candidate;
                    return true;
                }
            }
            parent = null;
            return false;
        }
    }

    public class Node
    {
        public string Name { get; set; }

        public string Contents { get; set; } = null;

        public bool IsFolder { get; set; } = false;

        public List<Node> Children { get; private set; } = new List<Node>();

        public Node(string name)
        {
            Name = name;
        }

        public static Node CreateMacro(string name) => new Node(name) { Contents = "" };

        public static Node CreateFolder(string name) => new Node(name) { IsFolder = true };
    }
    */
}
