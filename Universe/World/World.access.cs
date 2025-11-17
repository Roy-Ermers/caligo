
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.Universe;

public partial class World : IEnumerable<Chunk>
{
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

    public Chunk CreateChunk(Chunk chunk) => CreateChunk(chunk, false);

    public Chunk CreateChunk(Chunk chunk, bool overwrite)
    {
        if (!overwrite && _chunks.ContainsKey(chunk.Id))
            throw new InvalidOperationException($"Chunk at {chunk.Position} already exists.");

        _chunks[chunk.Id] = chunk;

        return chunk;
    }

    public bool HasChunk(ChunkPosition position) => _chunks.ContainsKey(position.Id);

    public void SetBlock(WorldPosition position, ushort blockId)
    {
        var chunkPosition = position.ChunkPosition;
        if (!TryGetChunk(position.ChunkPosition, out var chunk))
        {
            throw new KeyNotFoundException($"Chunk at {chunkPosition} not found.");
        }

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
