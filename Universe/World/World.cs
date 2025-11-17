using System.Collections.Concurrent;
using WorldGen.Renderer.Shaders;
using WorldGen.Universe.PositionTypes;
using WorldGen.Universe.WorldGenerators;

namespace WorldGen.Universe;

public partial class World : IEnumerable<Chunk>
{

    public IWorldGenerator WorldGenerator { get; init; }
    public WorldRenderer.ChunkRenderer ChunkRenderer { get; init; }

    private readonly BlockingCollection<Chunk> _chunkGenerationQueue = [];
    private readonly BlockingCollection<Chunk> _chunkMeshQueue = [];

    public Dictionary<ChunkPosition, ChunkLoader> chunkLoaders = [];

    public World(IWorldGenerator worldGenerator, WorldRenderer.ChunkRenderer chunkRenderer)
    {
        WorldGenerator = worldGenerator;
        WorldGenerator.Initialize();

        ChunkRenderer = chunkRenderer;
        ChunkRenderer.ChunkMesher.World = this;

        StartThreads();
    }

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

    public void UnloadChunk(ChunkPosition position)
    {
        if (_chunks.TryGetValue(position.Id, out var chunk))
        {
            _chunks.Remove(position.Id);
            ChunkRenderer.RemoveChunk(chunk);
        }
    }

    public void Render(RenderShader shader)
    {
        ChunkRenderer.Draw(shader);
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
                chunkLoaders.Remove(loader.Position);
                loaderIndex--;

                UnloadChunk(loader.Position);
                continue;
            }

            if (!HasChunk(loader.Position))
            {
                // If the chunk does not exist, we can create it
                CreateChunk(loader.Position);
            }

            chunkLoaders[loader.Position] = loader with { Ticks = loader.Ticks - 1 };
        }
    }

    private void StartThreads()
    {
        Task.Run(() => GenerateChunks());
        Task.Run(() => GenerateChunkMeshes());
    }

    private void GenerateChunkMeshes()
    {
        while (!_chunkMeshQueue.IsCompleted)
        {
            var chunk = _chunkMeshQueue.Take();

            // Generate the mesh for the chunk
            ChunkRenderer.AddChunk(chunk);
            chunk.State |= ChunkState.Meshed;

            _chunks[chunk.Id] = chunk;
        }
    }

    private void GenerateChunks()
    {
        while (!_chunkGenerationQueue.IsCompleted)
        {
            var chunk = _chunkGenerationQueue.Take();

            if (!_chunks.ContainsKey(chunk.Id))
            {
                // If the chunk is not found in the dictionary, it means it was not created yet
                // or it was removed. We can skip this iteration.
                continue;
            }

            // Generate the chunk using the world generator
            WorldGenerator.GenerateChunk(ref chunk);
            chunk.State |= ChunkState.Generated;

            _chunkMeshQueue.Add(chunk);
        }
    }
}
