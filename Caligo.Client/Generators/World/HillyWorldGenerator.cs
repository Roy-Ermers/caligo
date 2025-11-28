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
    private readonly GradientNoise _noise1;
    private readonly GradientNoise _noise2;

    private readonly float _roughness = 64f;
    private readonly float _frequency = 25f;


    public HillyWorldGenerator(int seed)
    {
        this._seed = seed;
        this._random = new Random(seed);

        _noise1 = new GradientNoise(seed);
        _noise2 = new GradientNoise(seed + 7);
    }


    public void GenerateChunk(ref Chunk chunk)
    {
        var iterator = new CubeIterator(chunk);
        foreach (var position in iterator)
        {
            float x = position.X;
            float z = position.Z;

            var offset = _noise2.Get2DVector(x * 0.01f, z * 0.01f);

            x += offset.X * _frequency;
            z += offset.Y * _frequency;

            var height = _noise1.Get2D(x / _frequency, z / _frequency) * _roughness;
            var cliff = MathF.Pow(2.0f, 10.0f * _noise2.Get2D(x / _frequency, z / _frequency) - 10f) * _roughness;


            height = Math.Max(height, cliff);

            var roundedHeight = (int)MathF.Floor(height);

            if (position.Y < roundedHeight)
            {
                chunk.Set(position.ChunkLocalPosition, DirtBlock);
            }
            else if (position.Y == roundedHeight)
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