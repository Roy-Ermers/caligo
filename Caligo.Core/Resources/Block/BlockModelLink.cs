using System.Text.Json.Serialization;

namespace Caligo.Core.Resources.Block;

public struct BlockModelLink
{
    [JsonPropertyName("name")] public string? BlockModelName { get; set; }

    public Dictionary<string, object> Textures { get; set; }
}