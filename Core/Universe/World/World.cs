using System.Collections.Concurrent;
using Caligo.Core.Spatial.PositionTypes;

namespace Caligo.Core.Universe.World;

public partial class World : IEnumerable<Chunk>
{
    private readonly ConcurrentDictionary<int, Chunk> _chunks = new();

    public void RemoveChunk(ChunkPosition position)
    {
        _chunks.TryRemove(position.Id, out _);
        chunkLoaders.Remove(position);
    }
}
