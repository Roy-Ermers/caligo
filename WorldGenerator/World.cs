using WorldGen.WorldGenerator.Chunks;

namespace WorldGen.WorldGenerator;

public class World
{
    private Dictionary<int, Chunk> _chunks = [];

    public Chunk GetChunk(ChunkPosition position)
    {
        if (_chunks.TryGetValue(position.Id, out var chunk))
        {
            return chunk;
        }

        throw new KeyNotFoundException($"Chunk at {position} not found.");
    }

    public bool TryGetChunk(ChunkPosition position, out Chunk chunk)
    {
        return _chunks.TryGetValue(position.Id, out chunk);
    }

    public Chunk TryGetOrCreateChunk(ChunkPosition position)
    {
        if (_chunks.TryGetValue(position.Id, out var chunk))
            return chunk;

        return CreateChunk(position);
    }

    public bool HasChunk(ChunkPosition position)
    {
        return _chunks.ContainsKey(position.Id);
    }

    public Chunk CreateChunk(ChunkPosition chunkPosition) => CreateChunk(chunkPosition, false);

    public Chunk CreateChunk(ChunkPosition chunkPosition, bool overwrite)
    {
        if (!overwrite && _chunks.ContainsKey(chunkPosition.Id))
            throw new InvalidOperationException($"Chunk at {chunkPosition} already exists.");

        var chunk = new Chunk(chunkPosition);
        _chunks[chunk.Id] = chunk;

        return chunk;
    }

    public void SetBlock(WorldPosition position, ushort blockId)
    {
        var chunkPosition = position.ChunkPosition;
        var chunk = TryGetOrCreateChunk(chunkPosition);

        chunk.Set(position.X % Chunk.Size, position.Y % Chunk.Size, position.Z % Chunk.Size, blockId);
    }

    public ushort GetBlock(WorldPosition position)
    {
        var chunkPosition = position.ChunkPosition;
        if (!TryGetChunk(chunkPosition, out var chunk))
            throw new KeyNotFoundException($"Chunk at {chunkPosition} not found.");

        return chunk.Get(position.X % Chunk.Size, position.Y % Chunk.Size, position.Z % Chunk.Size);
    }
}
