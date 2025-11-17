using WorldGen.Universe.PositionTypes;

namespace WorldGen.Universe;

public partial class World : IEnumerable<Chunk>
{
    private readonly Dictionary<ChunkPosition, ChunkLoader> chunkLoaders = [];

    public IEnumerable<ChunkLoader> ChunkLoaders => chunkLoaders.Values;

    public void EnqueueChunk(ChunkLoader loader, bool force = false)
    {
        if (loader.Ticks <= 0)
            return;

        // If the chunk already exists, we can just update the loader
        if (chunkLoaders.TryGetValue(loader.Position, out var existingLoader) && !force)
        {
            if (existingLoader.Ticks < loader.Ticks)
            {
                // If the existing loader has fewer ticks, we replace it
                chunkLoaders[loader.Position] = loader;
                return;
            }
        }

        // If the chunk does not exist, we create a new loader
        chunkLoaders[loader.Position] = loader;
    }

    public void Update()
    {
        // Update the chunk loaders
        for (var loaderIndex = 0; loaderIndex < chunkLoaders.Count; loaderIndex++)
        {
            var loader = chunkLoaders.ElementAt(loaderIndex).Value;

            // If the loader has no ticks left, we can remove it
            if (loader.Ticks <= 0)
            {
                loaderIndex--;

                chunkLoaders.Remove(loader.Position);
                continue;
            }

            chunkLoaders[loader.Position] = loader with { Ticks = loader.Ticks - 1 };
        }
    }
}
