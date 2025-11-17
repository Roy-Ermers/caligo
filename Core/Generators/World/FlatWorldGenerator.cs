using Caligo.Core.ModuleSystem;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;
using Caligo.Core.Utils;

namespace Caligo.Core.Generators.World;

public class FlatWorldGenerator : IWorldGenerator
{
    public int GroundLevel { get; init; } = 0;
    public Block GroundBlock { get; set; } = null!;
    public Block SoilBlock { get; set; } = null!;


    public void GenerateChunk(ref Chunk chunk)
    {
        if (chunk.Position.Y > GroundLevel)
        {
            return;
        }

        foreach (WorldPosition position in new CubeIterator(chunk))
        {
            if (position.Y < GroundLevel)
            {
                chunk.Set(position.ChunkLocalPosition, GroundBlock);
            }
            else if (position.Y == GroundLevel)
            {
                chunk.Set(position.ChunkLocalPosition, SoilBlock);
            }
        }
    }

    public void Initialize()
    {
        GroundBlock = ModuleRepository.Current.Get<Block>("dirt"); ;
        SoilBlock = ModuleRepository.Current.Get<Block>("grass_block");
    }
}
