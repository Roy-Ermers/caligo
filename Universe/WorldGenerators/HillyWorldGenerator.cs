using System.Numerics;
using WorldGen.ModuleSystem;
using WorldGen.Noise;
using WorldGen.Resources.Block;
using WorldGen.Universe.PositionTypes;
using WorldGen.Utils;

namespace WorldGen.Universe.WorldGenerators;

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
            var offset = detailNoise.Get2DVector(scaled.X, scaled.Z);
            scaled.X += offset.X * 5f;
            scaled.Z += offset.Y * 5f;
            var density = terrainNoise.Get2D(1 / 8f * scaled.X, 1 / 8f * scaled.Z) * (MaxGroundLevel - MinGroundLevel) + MinGroundLevel;

            // Determine block type at this position
            Block? blockToPlace = null;

            if (Math.Floor(density) == position.Y)
            {
                blockToPlace = GrassBlock;
            }
            else if (density > position.Y)
            {
                blockToPlace = DirtBlock;
            }
            // Surface block

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
