using System.ComponentModel;
using Caligo.Core.Generators.Features;
using Caligo.Core.Generators.World;
using Caligo.Core.Noise;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial;
using Caligo.Core.Universe;
using Caligo.Core.Utils;
using Caligo.ModuleSystem;
using Random = Caligo.Core.Utils.Random;

namespace Caligo.Client.Generators.World;

public class ForestWorldGenerator : IWorldGenerator
{
	private readonly int Seed;
	private readonly FeatureNetwork Network;
	private readonly Block TerrainBlock;
	private readonly Block PodzolBlock;
	private readonly Block shortGrass;
	private readonly Block tallGrass;
	private readonly Block pathBlock;
	private readonly Block flower;
	private readonly GradientNoise _noise;
	private readonly CellularNoise _pathNoise;
	private readonly GradientNoise _heightNoise;
	private readonly Heightmap _heightmap;
	private readonly Core.Universe.Worlds.World _world;

	public ForestWorldGenerator(Core.Universe.Worlds.World world, int seed)
	{
		_world = world;
		Seed = seed;

		_heightNoise = new GradientNoise(seed);
		_heightmap = new Heightmap((x,z) =>
		{
            var offset = _heightNoise.Get2DVector(x, z);
			
			return Easings.EaseInCubic(_heightNoise.Get2D(x + offset.X, z + offset.Y) * 0.5f + 0.5f) * 100f;
		});
		
		_noise = new GradientNoise(seed ^ 7777);
		_pathNoise = new CellularNoise(seed);

		Network = new FeatureNetwork(world, seed, _heightmap);
		TerrainBlock = ModuleRepository.Current.Get<Block>("grass_block");
		PodzolBlock = ModuleRepository.Current.Get<Block>("podzol");
		shortGrass = ModuleRepository.Current.Get<Block>("short_grass");
		tallGrass = ModuleRepository.Current.Get<Block>("tall_grass");
		flower = ModuleRepository.Current.Get<Block>("flower");
		pathBlock = ModuleRepository.Current.Get<Block>("cobblestone");
	}

	public void GenerateChunk(ref Chunk chunk)
	{
		Network.GetSector(chunk.Position.ToWorldPosition());
		var random = new Random(Seed ^ chunk.Id);
		var features = _world.Features.Query(chunk.BoundingBox);

		foreach (var position in new CubeIterator(chunk))
		{
			var noiseValue = _noise.Get2D(position.X / 20f, position.Z / 20f);
			
			var height = (int)_heightmap.GetHeightAt(position.X / 50f, position.Z / 50f, 1);
			if (position.Y < height)
			{
				chunk.Set(position.ChunkLocalPosition, TerrainBlock);
			
			}
			
			var path = _pathNoise.Get(position.X / 100f, position.Z / 100f);
			var pathDistance = Math.Abs(path.Distance0 - path.Distance1) - 0.025f;

			if (position.Y == height && pathDistance < 0)
				chunk.Set(position.ChunkLocalPosition, pathBlock);
			else if (position.Y == height) chunk.Set(position.ChunkLocalPosition, noiseValue < 0f ? TerrainBlock : PodzolBlock);

			foreach (var feature in features)
			{
				var blockId = feature.GetBlock(position);
			
				if (blockId != 0)
				{
					chunk.Set(position.ChunkLocalPosition, blockId);
				}
			}

			if(position.Y != height + 1 || chunk.Get(position.ChunkLocalPosition) != 0 || pathDistance < 0.025f) continue;

			Block[] decoration = [flower, shortGrass, tallGrass, Block.Air];

			var decorationIndex = noiseValue + (float)(random.NextDouble() / double.MaxValue);
			chunk.Set(position.ChunkLocalPosition, decoration[(int)(Easings.EaseInOutQuad(decorationIndex / 2f + 0.5f) * decoration.Length)]);
		}
	}

	public void Initialize()
	{

	}
}
