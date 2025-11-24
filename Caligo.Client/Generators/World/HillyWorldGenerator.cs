using Caligo.Core.Generators.World;
using Caligo.Core.Noise;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;
using Caligo.Core.Utils;
using Caligo.ModuleSystem;
using Random = Caligo.Core.Utils.Random;

namespace Caligo.Client.Generators.World;

public class HillyWorldGenerator : IWorldGenerator
{
    private readonly int _seed;
    private readonly Random _random;

    // Blocks
    public Block DirtBlock { get; set; } = null!;
    public Block GrassBlock { get; set; } = null!;

    // Noise generators
    private readonly GradientNoise _noise;

    private readonly float _roughness = 20;


    public HillyWorldGenerator(int seed)
    {
        this._seed = seed;
        this._random = new Random(seed);

        _noise = new GradientNoise(seed);
    }
    

    public void GenerateChunk(ref Chunk chunk)
    {
        var iterator = new CubeIterator(chunk);
        foreach (var position in iterator)
        {
            float x = position.X;
            float z = position.Z;
            
            var height = (int)Math.Floor(_noise.Get2D(x * 0.01f, z * 0.01f) * _roughness);
            
            if(position.Y < height)
            {
                chunk.Set(position.ChunkLocalPosition, DirtBlock);
            }
            else if (position.Y == height)
            {
                chunk.Set(position.ChunkLocalPosition, GrassBlock);
            }
        }
    }

    public void Initialize()
    {
        DirtBlock = ModuleRepository.Current.Get<Block>("dirt");
        GrassBlock = ModuleRepository.Current.Get<Block>("grass_block");
    }
}
