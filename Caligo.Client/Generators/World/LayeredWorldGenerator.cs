using Caligo.Core.Generators.Transport;
using Caligo.Core.Generators.World;
using Caligo.Core.Noise;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial;
using Caligo.Core.Universe;
using Caligo.Core.Utils;
using Caligo.ModuleSystem;
using Random = Caligo.Core.Utils.Random;

namespace Caligo.Client.Generators.World;

public class LayeredWorldGenerator : IWorldGenerator
{
	private readonly int Seed;
	private readonly TransportNetwork Network;
	private readonly Block TerrainBlock;
	private readonly Block PodzolBlock;
	private readonly Block shortGrass;
	private readonly Block tallGrass;
	private readonly Block flower;
	private readonly GradientNoise _noise;

	private readonly Core.Universe.World.World _world;

	public LayeredWorldGenerator(Core.Universe.World.World world, int seed)
	{
		_world = world;
		Seed = seed;
		Network = new TransportNetwork(world, seed);

		_noise = new GradientNoise(seed);

		TerrainBlock = ModuleRepository.Current.Get<Block>("grass_block");
		PodzolBlock = ModuleRepository.Current.Get<Block>("podzol");
		shortGrass = ModuleRepository.Current.Get<Block>("short_grass");
		tallGrass = ModuleRepository.Current.Get<Block>("tall_grass");
		flower = ModuleRepository.Current.Get<Block>("flower");
	}

	public void GenerateChunk(ref Chunk chunk)
	{
		Network.GetSector(chunk.Position.ToWorldPosition());
		var random = new Random(Seed ^ chunk.Id);
		var features = _world.Features.Query(chunk.BoundingBox);
		
		foreach (var position in new CubeIterator(chunk))
		{
			var noiseValue = _noise.Get2D(position.X / 20f, position.Z / 20f);
			if (position.Y <= 0)
			{
				chunk.Set(position.ChunkLocalPosition, noiseValue < 0f ? TerrainBlock : PodzolBlock);
			}
			
			foreach (var feature in features)
			{
				var blockId = feature.GetBlock(position);

				if (blockId != 0)
				{
					chunk.Set(position.ChunkLocalPosition, blockId);
				}
			}
			if(position.Y != 1 || chunk.Get(position.ChunkLocalPosition) != 0) continue;

			Block[] decoration = [flower, tallGrass, shortGrass, Block.Air];

			var index = noiseValue + (float)(random.NextDouble() / double.MaxValue);
			chunk.Set(position.ChunkLocalPosition, decoration[(int)(MathF.Pow(index / 2f + 0.5f, 2) * decoration.Length)]);
		}
	}

	public void Initialize()
	{

	}
}
