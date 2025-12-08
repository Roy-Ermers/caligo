using System.Numerics;
using System.Text.Json.Serialization;

namespace Caligo.Core.Resources.Block.Models;

public struct BlockModelCube
{
    public Vector3 From;
    public Vector3 To;

    [JsonPropertyName("faces")] public TextureFaces TextureFaces;

    [JsonIgnore]
    public readonly bool IsFullCube =>
        From.X + From.Y + From.Z == 0 && To.X == 16 && To.Y == 16 && To.Z == 16;
}