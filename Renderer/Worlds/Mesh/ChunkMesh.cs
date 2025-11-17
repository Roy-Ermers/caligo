using System.Collections.Frozen;
using WorldGen.Resources.Block;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.Renderer.Worlds.Mesh;

public readonly struct ChunkMesh
{
    public static readonly ChunkMesh Empty = new()
    {
        Faces = FrozenDictionary<Direction, List<BlockFaceRenderData>>.Empty,
        Position = ChunkPosition.Zero
    };
    internal static readonly int MaxFacesPerChunk = 16 * 16 * 16 * 6 / 2;

    public FrozenDictionary<Direction, List<BlockFaceRenderData>> Faces { get; init; }
    public ChunkPosition Position { get; init; }
}
