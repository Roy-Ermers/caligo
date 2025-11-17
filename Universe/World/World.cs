using System.Collections.Concurrent;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.Universe;

public partial class World : IEnumerable<Chunk>
{
    private readonly ConcurrentDictionary<int, Chunk> _chunks = new();

    public void RemoveChunk(ChunkPosition position)
    {
        _chunks.TryRemove(position.Id, out _);
        chunkLoaders.Remove(position);
    }
}
