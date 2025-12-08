using Caligo.Client.Generators.Layers;
using Caligo.Core.Generators.World;
using Caligo.Core.Universe;

namespace Caligo.Client.Generators.World;

public class LayerWorldGenerator : IWorldGenerator
{
    public readonly LayerCollection Layers;
    public readonly int Seed;
    public readonly Core.Universe.Worlds.World World;

    public LayerWorldGenerator(int seed, Core.Universe.Worlds.World world, LayerCollection layers)
    {
        Layers = layers;
        Seed = seed;

        World = world;
    }

    public void GenerateChunk(ref Chunk chunk)
    {
        foreach (var layer in Layers)
            layer.GenerateChunk(chunk);
    }

    public void Initialize()
    {
        long seed = Seed;
        foreach (var layer in Layers)
        {
            layer.Initialize(seed, this);
            seed ^= 0x5DEECE66D;
        }
    }
}