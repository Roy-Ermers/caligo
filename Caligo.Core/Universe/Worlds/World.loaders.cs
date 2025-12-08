using Caligo.Core.Spatial.PositionTypes;

namespace Caligo.Core.Universe.Worlds;

public partial class World : IEnumerable<Chunk>
{
    private readonly Dictionary<ChunkPosition, int> chunkLoaders = [];
    private readonly HashSet<ChunkPosition> loadedChunks = [];

    public IReadOnlySet<ChunkPosition> LoadedChunks => loadedChunks;

    public void EnqueueChunk(ChunkLoader loader, bool force = false)
    {
        if (loader.Ticks <= 0)
            return;

        // If the chunk does not exist, we create a new loader
        if (!loadedChunks.Contains(loader.Position) || force)
        {
            loadedChunks.Add(loader.Position);
            chunkLoaders[loader.Position] = loader.Ticks;
            return;
        }

        // If the chunk already exists, we can just update the loader

        var existingLoader = chunkLoaders[loader.Position];

        if (existingLoader >= loader.Ticks) return;
        // If the existing loader has fewer ticks, we replace it
        chunkLoaders[loader.Position] = loader.Ticks;
    }

    public void Update()
    {
        foreach (var position in LoadedChunks)
        {
            var ticks = chunkLoaders[position];

            if (ticks <= 0)
            {
                chunkLoaders.Remove(position);
                loadedChunks.Remove(position);
                continue;
            }

            chunkLoaders[position] = ticks - 1;
        }
    }
}