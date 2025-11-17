using System.Collections.Concurrent;
using System.Collections.Frozen;
using WorldGen.Renderer.Worlds.Materials;
using WorldGen.Renderer.Worlds.Mesh;
using WorldGen.ModuleSystem;
using WorldGen.ModuleSystem.Storage;
using WorldGen.Resources.Atlas;
using WorldGen.Resources.Block;
using WorldGen.Universe;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.Renderer.Worlds;

public class ChunkMesher(ResourceTypeStorage<Block> blockStorage, Atlas blockTextureAtlas, MaterialBuffer materialBuffer)
{
	public const string AtlasIdentifier = $"{Identifier.MainModule}:block_atlas";

	private readonly MaterialBuffer _materialBuffer = materialBuffer;
	private readonly ResourceTypeStorage<Block> _blockStorage = blockStorage;
	private readonly Atlas BlockTextureAtlas = blockTextureAtlas;

	private readonly BlockingCollection<Chunk> _chunkQueue = [];
	public readonly ConcurrentQueue<ChunkMesh> Meshes = [];

	public void EnqueueChunk(Chunk chunk)
	{
		if (chunk.BlockCount == 0)
		{
			chunk.State |= ChunkState.Meshed;
			return; // No need to mesh empty chunks
		}

		chunk.State |= ChunkState.Meshing;
		_chunkQueue.Add(chunk);
	}

	public bool TryDequeue(out ChunkMesh mesh)
	{
		if (Meshes.TryDequeue(out mesh))
			return true;

		mesh = default;
		return false;
	}

	public void StartProcessing()
	{
		var thread = new Thread(Process);
		thread.IsBackground = true;
		thread.Name = "ChunkMesherThread";
		thread.Start();
	}

	private void Process()
	{
		Parallel.ForEach(_chunkQueue.GetConsumingEnumerable(), new ParallelOptions() { MaxDegreeOfParallelism = 4 }, (chunk, cancellationToken) =>
		{
			try
			{
				var mesh = GenerateMesh(chunk);
				Meshes.Enqueue(mesh);
			}
			catch (Exception error)
			{
				Console.WriteLine($"Failed to mesh chunk at {chunk.Position}, {error.Message}");
			}
		});
	}

	private ChunkMesh GenerateMesh(Chunk chunk)
	{
		Random random = new(chunk.Id);
		if (chunk.BlockCount == 0)
		{
			return new ChunkMesh
			{
				Faces = ChunkMesh.Empty.Faces,
				Position = chunk.Position
			};
		}

		var faces = new Dictionary<Direction, List<BlockFaceRenderData>>();

		for (short i = 0; i < Math.Pow(Chunk.Size, 3); i++)
		{
			var position = ChunkLocalPosition.FromIndex(i);
			// tryGet skips air blocks, so we only process non-air blocks
			if (!chunk.TryGet(position, out var blockId))
			{
				continue;
			}

			var block = _blockStorage[blockId];
			if (block is null)
			{
				Console.WriteLine($"Block with ID {blockId} not found in storage.");
				continue;
			}

			var variant = block.GetRandomVariant(random);

			// nothing to render.
			if (variant is null)
				continue;

			for (var direction = (Direction)0; direction <= (Direction)5; direction++)
			{
				if (chunk.TryGet(position + direction.ToVector3(), out ushort neighborBlockId))
				{
					var neighborBlock = _blockStorage[neighborBlockId];
					var neighborModel = neighborBlock?.GetRandomVariant(random);

					if (neighborModel is not null && (neighborModel.Value.Model!.Culling?.IsCullingEnabled(direction.Opposite()) ?? false))
					{
						// Don't render face if culling is enabled and neighbor block is not air
						continue;
					}
				}

				foreach (var element in variant.Value.Model.Elements.Reverse())
				{
					var newFace = element.ToRenderData(direction, position, variant.Value.Textures ?? [], _materialBuffer, BlockTextureAtlas);
					if (newFace is null)
						continue;

					if (faces.TryGetValue(direction, out var faceList))
						faceList.Add(newFace.Value);
					else
					{
						faceList = [newFace.Value];
						faces.Add(direction, faceList);
					}
				}
			}
		}

		chunk.State |= ChunkState.Meshed;
		chunk.State &= ~ChunkState.Meshing;

		return new ChunkMesh
		{
			Faces = faces.ToFrozenDictionary(),
			Position = chunk.Position
		};
	}
}
