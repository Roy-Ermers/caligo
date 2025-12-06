using Caligo.Core.Spatial.PositionTypes;

namespace Caligo.Core.Universe.Worlds;

public partial class World : IEnumerable<Chunk>
{
    private readonly Dictionary<int, Chunk> _chunks = new();

    public void RemoveChunk(ChunkPosition position)
    {
        _chunks.Remove(position.Id);
        chunkLoaders.Remove(position);
    }

    public void Clear()
    {
        _chunks.Clear();
        Features.Clear();
        chunkLoaders.Clear();
        loadedChunks.Clear();
    }
}
