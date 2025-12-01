using System.Collections.Concurrent;
using System.Threading.Channels;
using Caligo.Core.Generators.World;
using Caligo.Core.Spatial.PositionTypes;

namespace Caligo.Core.Universe;

public class WorldBuilder
{
	const int MAX_GENERATION_THREADS = 8;
	public World.World World { init; get; }
	public IWorldGenerator Generator { init; get; }

	private Channel<ChunkPosition> _channel;

	// private readonly BlockingCollection<ChunkPosition> _generationQueue = [];

	public WorldBuilder(World.World world, IWorldGenerator generator)
	{
		World = world;
		Generator = generator;

		Generator.Initialize();
		_channel = Channel.CreateUnbounded<ChunkPosition>(new UnboundedChannelOptions
		{
			SingleReader = false,
			SingleWriter = true
		});
		StartProcessing();
	}

	public void StartProcessing()
	{
		var tf = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.AttachedToParent);
		for(var i = 0; i < MAX_GENERATION_THREADS; i++)
			tf.StartNew(WorkGenerationQueue);
		// for(var processor = 0; processor < 1; processor++) {
		// 	var thread = new Thread(WorkGenerationQueue)
		// 	{
		// 		IsBackground = true,
		// 		Name = $"WorldGenerationThread {processor}"
		// 	};
		// 	thread.Start();
		// }
	}

	public void Update()
	{
		foreach (var position in World.LoadedChunks)
		{
			if (World.HasChunk(position))
				continue;


			var chunk = new Chunk(position);
			World.CreateChunk(chunk);

			_channel.Writer.WriteAsync(position);
			// _generationQueue.TryAdd(position);
		}
	}


	public async Task WorkGenerationQueue()
	{
		await foreach (var position in _channel.Reader.ReadAllAsync())
		{
			if (!World.TryGetChunk(position, out var chunk))
				return;

			if ((chunk.State & (ChunkState.Generating | ChunkState.Generated)) != 0)
				return;

			chunk.State |= ChunkState.Generating;

			_ = Task.Run(()=>GenerateChunk(chunk));
		}
	}

	private async Task GenerateChunk(Chunk chunk)
	{
		Generator.GenerateChunk(ref chunk);

		chunk.State &= ~ChunkState.Generating;
		chunk.State |= ChunkState.Generated;

	}
}
