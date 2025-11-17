using System.Collections.Concurrent;
using Caligo.Core.Spatial.PositionTypes;

namespace Caligo.Core.Universe.World;

public partial class World : IEnumerable<Chunk>
{
    private readonly Dictionary<int, Chunk> _chunks = new();

    public void RemoveChunk(ChunkPosition position)
    {
        _chunks.Remove(position.Id);
        chunkLoaders.Remove(position);
    }
}
