using System.Collections.Concurrent;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.Universe.WorldGenerators;

public class WorldBuilder
{
    public World World { init; get; }
    public IWorldGenerator Generator { init; get; }

    private readonly BlockingCollection<ChunkPosition> GenerationQueue = [];

    public WorldBuilder(World world, IWorldGenerator generator)
    {
        World = world;
        Generator = generator;

        Generator.Initialize();

        Task.Run(() => WorkGenerationQueue());
    }

    public void Update()
    {
        foreach (var loader in World.ChunkLoaders)
        {
            if (World.HasChunk(loader.Position))
                continue;


            var chunk = new Chunk(loader.Position);
            World.CreateChunk(chunk);

            GenerationQueue.TryAdd(loader.Position);
        }
    }


    public void WorkGenerationQueue()
    {
        Parallel.ForEach(GenerationQueue.GetConsumingEnumerable(), new ParallelOptions() { MaxDegreeOfParallelism = 4 }, (position, cancellationToken) =>
        {
            if (!World.TryGetChunk(position, out var chunk))
                return;

            if (chunk.State.HasFlag(ChunkState.Generating) || chunk.State.HasFlag(ChunkState.Generated))
                return;

            chunk.State |= ChunkState.Generating;

            Generator.GenerateChunk(ref chunk);

            chunk.State &= ~ChunkState.Generating;
            chunk.State |= ChunkState.Generated;

        });
    }
}
