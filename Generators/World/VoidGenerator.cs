using WorldGen.ModuleSystem;
using WorldGen.Resources.Block;
using WorldGen.Universe;
using WorldGen.Universe.PositionTypes;
using WorldGen.Utils;

namespace WorldGen.Generators.World;

public class VoidGenerator : IWorldGenerator
{
    public Block Spawn { get; init; } = ModuleRepository.Current.Get<Block>("air");


    public void GenerateChunk(ref Chunk chunk)
    {
        if (chunk.Position.X != 0 || chunk.Position.Y != 0 || chunk.Position.Z != 0)
        {
            return;
        }

        chunk.Set(0, 0, 0, Spawn);
    }

    public void Initialize()
    {
    }
}
