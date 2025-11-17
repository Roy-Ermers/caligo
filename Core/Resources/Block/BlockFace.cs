using System.Numerics;
using System.Text.Json.Serialization;

namespace Caligo.Core.Resources.Block;

public struct BlockFace
{
    public string? Texture;

    public Vector3? Tint;

    [JsonIgnore]
    public readonly string? TextureVariable => Texture is not null && Texture.StartsWith('#') ? Texture[1..] : null;

    [JsonPropertyName("uv")]
    public Vector4 UV;

    [JsonConstructor]
    public BlockFace(string? texture)
    {

        Texture = texture;
        UV = new Vector4(0, 0, 16, 16);
    }

    public BlockFace Clone()
    {
        return new BlockFace(Texture)
        {
            UV = UV,
            Tint = Tint
        };
    }
}
