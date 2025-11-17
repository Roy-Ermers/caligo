using System.Numerics;
using WorldGen.ModuleSystem;
using WorldGen.Noise;
using WorldGen.Resources.Block;
using WorldGen.Universe;
using WorldGen.Universe.PositionTypes;
using WorldGen.Utils;

namespace WorldGen.Generators.World;

public class HillyWorldGenerator : IWorldGenerator
{
    private readonly int seed;
    private readonly Random random;

    // Blocks
    public Block DirtBlock { get; set; } = null!;
    public Block GrassBlock { get; set; } = null!;

    // Noise generators
    private readonly GradientNoise terrainNoise;
    private readonly GradientNoise detailNoise;

    // Terrain parameters
    public float MinGroundLevel = 0;
    public float MaxGroundLevel = 96;
    public float Frequency = 0.01f;
    public int Octaves = 2;

    public HillyWorldGenerator(int seed)
    {
        this.seed = seed;
        this.random = new Random(seed);

        // Initialize noise generators with different seeds
        terrainNoise = new GradientNoise(seed);
        detailNoise = new GradientNoise(seed + 2);
    }

    public void GenerateChunk(ref Chunk chunk)
    {
        foreach (WorldPosition position in new CubeIterator(chunk))
        {
            var scaled = ((Vector3)position) / 100f;
            var (X, Y) = detailNoise.Get2DVector(scaled.X, scaled.Z);
            scaled.X += X * 5f;
            scaled.Z += Y * 5f;
            var density = terrainNoise.Get2D(1 / 8f * scaled.X, 1 / 8f * scaled.Z) * (MaxGroundLevel - MinGroundLevel) + MinGroundLevel;


            // Determine block type at this position
            Block? blockToPlace = null;

            if (Math.Floor(density) >= position.Y && detailNoise.Get3D(scaled.X, Y, scaled.Z) > .25)
            {
                blockToPlace = ModuleRepository.Current.Get<Block>("stone");
            }
            else if (Math.Floor(density) == position.Y)
            {
                blockToPlace = GrassBlock;
            }
            else if (density > position.Y)
            {
                blockToPlace = DirtBlock;
            }


            // Place the block if one was selected
            if (blockToPlace != null)
            {
                chunk.Set(position.ChunkLocalPosition, blockToPlace);
            }
        }
    }

    public void Initialize()
    {
        DirtBlock ??= ModuleRepository.Current.Get<Block>("dirt");
        GrassBlock ??= ModuleRepository.Current.Get<Block>("grass_block");
    }
}
