using Caligo.Client.Generators.World;
using Caligo.Core.Noise;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;
using Caligo.Core.Utils;
using Caligo.ModuleSystem;

namespace Caligo.Client.Generators.Layers;

public class GroundLayer : ILayer
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
            var height = (int)heightLayer.HeightMap.GetHeightAt(worldPosition.X, worldPosition.Z);

            if (worldPosition.Y > height) continue;

            var noiseValue = _noise.Get2D(worldPosition.X / 50f, worldPosition.Z / 50f);
            var groundBlock = groundBlocks[(int)MathF.Min(groundBlocks.Length - 1, Easings.EaseInExpo((height + noiseValue * 100f) /
                (heightLayer.MaxHeight / 3f)))];

            var localPosition = ChunkLocalPosition.FromWorldPosition(worldPosition);
            chunk.Set(localPosition, groundBlock);
        }
    }
}