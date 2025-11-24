using Caligo.Core.ModuleSystem;
using Caligo.Core.Resources.Block;
using Caligo.Core.Universe;
using Caligo.ModuleSystem;

namespace Caligo.Core.Generators.World;

public class VoidGenerator : IWorldGenerator
{
    public Block Spawn { get; init; } = ModuleRepository.Current.Get<Block>("grass_block");


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
