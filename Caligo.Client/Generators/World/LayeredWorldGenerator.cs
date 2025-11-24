using Caligo.Core.Generators.Transport;
using Caligo.Core.Generators.World;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial;
using Caligo.Core.Universe;
using Caligo.Core.Utils;
using Caligo.ModuleSystem;

namespace Caligo.Client.Generators.World;

public class LayeredWorldGenerator : IWorldGenerator
{
	private readonly int Seed;
	private readonly TransportNetwork Network;
	private readonly Block TerrainBlock;
	private readonly Block OddTerrainBlock;
	private readonly Block NodeBlock;

	private readonly Core.Universe.World.World _world;

	public LayeredWorldGenerator(Core.Universe.World.World world, int seed)
	{
		_world = world;
		Seed = seed;
		Network = new TransportNetwork(world, seed);

		TerrainBlock = ModuleRepository.Current.Get<Block>("grass_block");
		OddTerrainBlock = ModuleRepository.Current.Get<Block>("stone");
		NodeBlock = ModuleRepository.Current.Get<Block>("node");
	}

	public void GenerateChunk(ref Chunk chunk)
	{
		Network.GetSector(chunk.Position.ToWorldPosition());
		var features = _world.Features.Query(chunk.BoundingBox);
		
		foreach (var position in new CubeIterator(chunk))
		{
			if (position.Y <= 0)
			{
				chunk.Set(position.ChunkLocalPosition, TerrainBlock);
			}
			
			foreach (var feature in features)
			{
				var blockId = feature.GetBlock(position);

				if (blockId != 0)
				{
					chunk.Set(position.ChunkLocalPosition, blockId);
				}
			}

		}
	}

	public void Initialize()
	{

	}
}
