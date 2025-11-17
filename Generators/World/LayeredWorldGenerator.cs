using WorldGen.Generators.Transport;
using WorldGen.ModuleSystem;
using WorldGen.Resources.Block;
using WorldGen.Universe;
using WorldGen.Utils;

namespace WorldGen.Generators.World;

public class LayeredWorldGenerator : IWorldGenerator
{
	private readonly int Seed;
	private readonly TransportNetwork Network;
	private readonly Block TerrainBlock;
	private readonly Block OddTerrainBlock;
	private readonly Block NodeBlock;

	public LayeredWorldGenerator(int seed)
	{
		Seed = seed;
		Network = new TransportNetwork(seed);

		TerrainBlock = ModuleRepository.Current.Get<Block>("grass_block");
		OddTerrainBlock = ModuleRepository.Current.Get<Block>("stone");
		NodeBlock = ModuleRepository.Current.Get<Block>("node");
	}

	public void GenerateChunk(ref Chunk chunk)
	{
		Network.GetSector(chunk.Position.ToWorldPosition());
		var features = Game.Instance.world.Features.Query(chunk.BoundingBox);
		
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
					continue;
				}
			}

		}
	}

	public void Initialize()
	{

	}
}
