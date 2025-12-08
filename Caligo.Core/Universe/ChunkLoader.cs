using Caligo.Core.Spatial.PositionTypes;

namespace Caligo.Core.Universe;

public record struct ChunkLoader(
    /// <summary>
    /// Position of the chunk to load.
    /// </summary>
    ChunkPosition Position,
    /// <summary>
    /// Number of update ticks to process this chunk for.
    /// </summary>
    int Ticks = 1
);