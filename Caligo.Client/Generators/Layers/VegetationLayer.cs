using Caligo.Client.Generators.World;
using Caligo.Core.Noise;
using Caligo.Core.Resources.Block;
using Caligo.Core.Universe;
using Caligo.ModuleSystem;

namespace Caligo.Client.Generators.Layers;

public class VegetationLayer : ILayer
{
    private HeightLayer _heightLayer = null!;
    private GradientNoise _noise = null!;
    private long Seed;
    private Block[] VegetationBlocks = [];

    public void Initialize(long seed, LayerWorldGenerator generator)
    {
        _heightLayer = generator.Layers.GetLayer<HeightLayer>();
        Seed = seed;
        _noise = new GradientNoise((int)seed);
        VegetationBlocks =
        [
            ModuleRepository.Current.Get<Block>("flower"),
            ModuleRepository.Current.Get<Block>("short_grass"),
            ModuleRepository.Current.Get<Block>("tall_grass"),
            ModuleRepository.Current.Get<Block>("sapling")
        ];
    }

    public void GenerateChunk(Chunk chunk)
    {
        for (var x = 0; x < Chunk.Size; x++)
        for (var z = 0; z < Chunk.Size; z++)
        {
            var worldPos = chunk.Position.ToWorldPosition() + (x, 0, z);
            var height = _heightLayer.HeightMap.GetHeightAt(worldPos.X, worldPos.Z) + 1;
            // check if height is out of bounds of the current chunk
            if (height > worldPos.Y + Chunk.Size || height < worldPos.Y)
                continue;
            
            if(height > _heightLayer.MaxHeight / 3f) continue;

            worldPos = worldPos with { Y = (int)height };

            if (chunk.Get(worldPos.ChunkLocalPosition) != 0) continue;

            var offset = _noise.Get2DVector(worldPos.X / 10f, worldPos.Z / 10f);

            var blockChance =
                (int)((_noise.Get2D(worldPos.X / 20f + offset.X, worldPos.Z / 20f + offset.Y) * 0.5f + 0.5f) *
                    VegetationBlocks.Length + 1);

            if (blockChance == 0)
                continue;

            var vegetationBlock = VegetationBlocks[blockChance - 1];
            chunk.Set(worldPos.ChunkLocalPosition, vegetationBlock);
        }
    }
}