using System.Collections.Concurrent;
using Caligo.Core.Generators.World;
using Caligo.Core.Spatial.PositionTypes;

namespace Caligo.Core.Universe;

public class WorldBuilder
{
	public World.World World { init; get; }
	public IWorldGenerator Generator { init; get; }

	private readonly BlockingCollection<ChunkPosition> _generationQueue = [];

	public WorldBuilder(World.World world, IWorldGenerator generator)
	{
		World = world;
		Generator = generator;

		Generator.Initialize();



		StartProcessing();
	}

	public void StartProcessing()
	{
		for(var processor = 0; processor < 4; processor++) {
			var thread = new Thread(WorkGenerationQueue)
			{
				IsBackground = true,
				Name = $"WorldGenerationThread {processor}"
			};
			thread.Start();
		}
	}

	public void Update()
	{
		foreach (var position in World.LoadedChunks)
		{
			if (World.HasChunk(position))
				continue;


			var chunk = new Chunk(position);
			World.CreateChunk(chunk);

			_generationQueue.TryAdd(position);
		}
	}


	public void WorkGenerationQueue()
	{
		while(!_generationQueue.IsCompleted)
		{
			var position = _generationQueue.Take();
			if (!World.TryGetChunk(position, out var chunk))
				return;

			if (chunk.State.HasFlag(ChunkState.Generating) || chunk.State.HasFlag(ChunkState.Generated))
				return;

			chunk.State |= ChunkState.Generating;

			Generator.GenerateChunk(ref chunk);

			chunk.State &= ~ChunkState.Generating;
			chunk.State |= ChunkState.Generated;
		}
	}
}
