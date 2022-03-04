using System;
using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SomethingNeedDoing;

/// <summary>
/// Base node interface type.
/// </summary>
public interface INode
{
    /// <summary>
    /// Gets or sets the name of the node.
    /// </summary>
    public string Name { get; set; }
}

/// <summary>
/// Macro node type.
/// </summary>
public class MacroNode : INode
{
    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contents of the macro.
    /// </summary>
    public string Contents { get; set; } = string.Empty;
}

/// <summary>
/// Folder node type.
/// </summary>
public class FolderNode : INode
{
    /// <inheritdoc/>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the children inside this folder.
    /// </summary>
    [JsonProperty(ItemConverterType = typeof(ConcreteNodeConverter))]
    public List<INode> Children { get; } = new List<INode>();
}

/// <summary>
/// Converts INodes to MacroNodes or FolderNodes.
/// </summary>
public class ConcreteNodeConverter : JsonConverter
{
    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override bool CanConvert(Type objectType) => objectType == typeof(INode);

    /// <inheritdoc/>
    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var jObject = JObject.Load(reader);
        var jType = jObject["$type"]?.Value<string>();

        if (jType == this.SimpleName(typeof(MacroNode)))
        {
            var obj = new MacroNode();
            serializer.Populate(jObject.CreateReader(), obj);
            return obj;
        }
        else if (jType == this.SimpleName(typeof(FolderNode)))
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

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    private string SimpleName(Type type)
    {
        return $"{type.FullName}, {type.Assembly.GetName().Name}";
    }
}
