using Caligo.Client.Generators.World;
using Caligo.Core.Noise;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;
using Caligo.ModuleSystem;

namespace Caligo.Client.Generators.Layers;

public class SurfaceLayer : ILayer
{
    private GradientNoise _noise;
    private Block[] groundBlocks = [];
    private HeightLayer heightLayer = null!;

    public void Initialize(long seed, LayerWorldGenerator generator)
    {
        heightLayer = generator.Layers.GetLayer<HeightLayer>();
        _noise = new GradientNoise((int)seed);
        groundBlocks =
        [
            ModuleRepository.Current.Get<Block>("grass_block"),
            ModuleRepository.Current.Get<Block>("stone"),
            ModuleRepository.Current.Get<Block>("snow")
        ];
    }


    public void GenerateChunk(Chunk chunk)
    {
        foreach (var worldPosition in new CubeIterator(chunk))
        {
            var noiseValue = _noise.Get2D(worldPosition.X / 10f, worldPosition.Z / 10f);
            var height = (int)(heightLayer.HeightMap.GetHeightAt(worldPosition.X, worldPosition.Z));

            if (worldPosition.Y > height) continue;


            var groundBlock = groundBlocks[0];

            var slope = heightLayer.HeightMap.GetSlope(worldPosition.X, worldPosition.Z);

            if (height + noiseValue * 15f < HeightLayer.MaxHeight * 0.3f)
            {
                groundBlock = groundBlocks[0]; // grass
                if (slope > 0.25f)
                {
                    groundBlock = groundBlocks[1]; // stone
                }
            }
            else if (height + noiseValue * 15f < HeightLayer.MaxHeight * 0.8f)
            {
                groundBlock = groundBlocks[2]; // snow
                if (slope > 0.25f)
                {
                    groundBlock = groundBlocks[1]; // stone
                }
            }

            var localPosition = ChunkLocalPosition.FromWorldPosition(worldPosition);
            chunk.Set(localPosition, groundBlock);
        }
    }
}