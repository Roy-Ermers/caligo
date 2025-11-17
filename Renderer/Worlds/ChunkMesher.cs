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
using WorldGen.Utils;

namespace WorldGen.Renderer.Worlds;

public class ChunkMesher(ResourceTypeStorage<Block> blockStorage, Atlas blockTextureAtlas, MaterialBuffer materialBuffer)
{
	public const string AtlasIdentifier = $"{Identifier.MainModule}:block_atlas";

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
		for(var processor = 0; processor < 4; processor++) {
			var thread = new Thread(Process)
			{
				IsBackground = true,
				Name = $"ChunkMesherThread {processor}"
			};
			thread.Start();
		}
	}

	private void Process()
	{
		while(!_chunkQueue.IsCompleted) {
			var chunk = _chunkQueue.Take();
			try
			{
				var mesh = GenerateMesh(chunk);
				Meshes.Enqueue(mesh);
			}
			catch (Exception error)
			{
				Console.WriteLine($"Failed to mesh chunk at {chunk.Position}, {error.Message}");
			}
		}
	}

	private ChunkMesh GenerateMesh(Chunk chunk)
	{
		Random random = new(chunk.Id);
		var world = Game.Instance.world;
		if (chunk.BlockCount == 0)
		{
			return ChunkMesh.Empty with {
				Position = chunk.Position
			};
		}

		var faces = new Dictionary<Direction, List<BlockFaceRenderData>>();

		for (short i = 0; i < Math.Pow(Chunk.Size, 3); i++)
		{
			var position = ChunkLocalPosition.FromIndex(i);
			var worldPosition = position.ToWorldPosition(chunk.Position);
			// tryGet skips air blocks, so we only process non-air blocks
			if (!world.TryGetBlock(worldPosition, out var blockId))
				continue;

			if (!blockStorage.TryGetValue(blockId, out var block))
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
				if (world.TryGetBlock(worldPosition + direction.ToVector3(), out var neighborBlockId))
				{
					var neighborBlock = blockStorage[neighborBlockId];
					var neighborModel = neighborBlock?.GetRandomVariant(random);

					if (neighborModel is not null && (neighborModel.Value.Model!.Culling?.IsCullingEnabled(direction.Opposite()) ?? false))
					{
						// Don't render face if culling is enabled and neighbor block is not air
						continue;
					}
				}

				foreach (var element in variant.Value.Model.Elements.Reverse())
				{
					var newFace = element.ToRenderData(direction, position, variant.Value.Textures ?? [], materialBuffer, blockTextureAtlas);
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
		
		return new ChunkMesh(
			faces,
			chunk.Position
		);
	}
}
