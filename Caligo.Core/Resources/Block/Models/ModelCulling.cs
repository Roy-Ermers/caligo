using System.Text.Json.Serialization;
using Caligo.Core.Utils;

namespace Caligo.Core.Resources.Block.Models;

public record struct ModelCulling : IEquatable<ModelCulling>
{
    [JsonPropertyName("down")]
    public bool CullDown;
    [JsonPropertyName("up")]
    public bool CullUp;
    [JsonPropertyName("north")]
    public bool CullNorth;
    [JsonPropertyName("south")]
    public bool CullSouth;
    [JsonPropertyName("west")]
    public bool CullWest;
    [JsonPropertyName("east")]
    public bool CullEast;

    public static ModelCulling None => new();
    public static ModelCulling All => new()
    {
        CullDown = true,
        CullUp = true,
        CullNorth = true,
        CullSouth = true,
        CullWest = true,
        CullEast = true
    };

    public readonly bool IsCullingEnabled(Direction direction)
    {
        return direction switch
        {
            Direction.Down => CullDown,
            Direction.Up => CullUp,
            Direction.North => CullNorth,
            Direction.South => CullSouth,
            Direction.West => CullWest,
            Direction.East => CullEast,
            _ => false
        };
    }
}
