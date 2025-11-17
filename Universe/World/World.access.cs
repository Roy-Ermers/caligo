
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.Universe;

public partial class World : IEnumerable<Chunk>
{
    private readonly Dictionary<int, Chunk> _chunks = new(64);

    public Chunk? GetChunk(ChunkPosition position)
    {
        if (_chunks.TryGetValue(position.Id, out var chunk))
        {
            return chunk;
        }

        return null;
    }

    public bool TryGetChunk(ChunkPosition position, [MaybeNullWhen(false)] out Chunk chunk)
    {
        return _chunks.TryGetValue(position.Id, out chunk);
    }

    public Chunk TryGetOrCreateChunk(ChunkPosition position)
    {
        if (_chunks.TryGetValue(position.Id, out var chunk))
            return chunk;

        return CreateChunk(position);
    }

    public Chunk CreateChunk(ChunkPosition chunkPosition) => CreateChunk(chunkPosition, false);

    public Chunk CreateChunk(ChunkPosition chunkPosition, bool overwrite)
    {
        if (!overwrite && _chunks.ContainsKey(chunkPosition.Id))
            throw new InvalidOperationException($"Chunk at {chunkPosition} already exists.");

        var chunk = new Chunk(chunkPosition);
        _chunks[chunk.Id] = chunk;
        _chunkGenerationQueue.Add(chunk);

        return chunk;
    }

    public bool HasChunk(ChunkPosition position) => _chunks.ContainsKey(position.Id);

    public void SetBlock(WorldPosition position, ushort blockId)
    {
        var chunkPosition = position.ChunkPosition;
        var chunk = TryGetOrCreateChunk(chunkPosition);

        chunk.Set(position.ChunkLocalPosition, blockId);
    }

    public ushort GetBlock(WorldPosition position)
    {
        if (!TryGetChunk(position.ChunkPosition, out var chunk))
            throw new KeyNotFoundException($"Chunk at {position.ChunkPosition} not found.");

        return chunk.Get(position.ChunkLocalPosition);
    }


    public IEnumerator<Chunk> GetEnumerator()
    {
        return _chunks.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
