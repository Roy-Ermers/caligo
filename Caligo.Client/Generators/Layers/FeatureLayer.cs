using Caligo.Client.Generators.World;
using Caligo.Core.Generators.Features;
using Caligo.Core.Spatial;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;

namespace Caligo.Client.Generators.Layers;

public class FeatureLayer : ILayer
{
    public FeatureNetwork FeatureNetwork { get; private set; } = null!;
    private Core.Universe.Worlds.World _world;
    
    public void Initialize(long seed, LayerWorldGenerator generator)
    {
        var heightMap = generator.Layers.GetLayer<HeightLayer>();
        FeatureNetwork = new FeatureNetwork(generator.World, (int)seed, heightMap.HeightMap);
        _world = generator.World;
    }

    public void GenerateChunk(Chunk chunk)
    {
        FeatureNetwork.GetSector(chunk.Position.ToWorldPosition());
		var features = _world.Features.Query(chunk.BoundingBox);
        
        if(features.Count == 0) return;
        
        foreach (var position in new CubeIterator(chunk))
        {
            foreach (var feature in features)
            {
				var blockId = feature.GetBlock(position);

                if (blockId == 0) continue;
                var localPosition = ChunkLocalPosition.FromWorldPosition(position);
                chunk.Set(localPosition, blockId);
            }
                
        }
    }
    
}