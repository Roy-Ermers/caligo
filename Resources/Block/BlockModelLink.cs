using System.Text.Json.Serialization;

namespace WorldGen.Resources.Block;

public struct BlockModelLink
{
    [JsonPropertyName("name")]
    public string? BlockModelName { get; set; }

    public Dictionary<string, string> Textures { get; set; }
}
