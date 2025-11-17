using System;
using System.Collections.Generic;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
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
    public Block StoneBlock { get; set; } = null!;
    public Block DirtBlock { get; set; } = null!;
    public Block GrassBlock { get; set; } = null!;
    public Block SandBlock { get; set; } = null!;
    public Block WaterBlock { get; set; } = null!;
    public Block WoodBlock { get; set; } = null!;
    public Block LeavesBlock { get; set; } = null!;

    // Noise generators
    private readonly GradientNoise terrainNoise;
    private readonly GradientNoise detailNoise;

    // Terrain parameters
    public float MinGroundLevel = 0;
    public float MaxGroundLevel = 96;
    public float Frequency = 0.005f;
    public int Octaves = 6;

    public HillyWorldGenerator(int seed)
    {
        this.seed = seed;
        this.random = new Random(seed);

        // Initialize noise generators with different seeds
        terrainNoise = new GradientNoise(seed);
        detailNoise = new GradientNoise(seed + 2);
    }

    public Chunk GenerateChunk(ref Chunk chunk)
    {
        Dictionary<Vector2i, float> heightCache = [];

        foreach (WorldPosition position in new CubeIterator(chunk))
        {
            Vector2i columnPos = new(position.X, position.Z);

            // Get or calculate terrain height
            if (!heightCache.TryGetValue(columnPos, out float height))
            {
                height = GetTerrainHeight(position.X, position.Z);
                heightCache[columnPos] = height;
            }

            // Determine block type at this position
            Block? blockToPlace = null;

            if (position.Y < height - 3)
            {
                blockToPlace = DirtBlock;
            }
            // Dirt layer
            else if (position.Y < height)
            {
                blockToPlace = DirtBlock;
            }
            // Surface block
            else if (position.Y == Math.Floor(height))
            {
                blockToPlace = GrassBlock;
            }

            // Place the block if one was selected
            if (blockToPlace != null)
            {
                chunk.Set(position.ChunkLocalPosition, blockToPlace);
            }
        }

        return chunk;
    }

    public int GetTerrainHeight(int x, int z)
    {
        float height = 0f;
        float amplitude = 1f;
        float frequency = Frequency;

        for (int i = 0; i < Octaves; i++)
        {
            height += terrainNoise.Get2D(x * frequency, z * frequency) * amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }

        // Add detail noise
        height += detailNoise.Get2D(x * Frequency * 2, z * Frequency * 2) * 2f;

        // Normalize height to [MinGroundLevel, MaxGroundLevel]
        height = (height + 1) / 2; // Normalize to [0,1]
        height = MinGroundLevel + height * (MaxGroundLevel - MinGroundLevel);



        return (int)Math.Clamp(height, MinGroundLevel, MaxGroundLevel);
    }

    public void Initialize()
    {
        DirtBlock ??= ModuleRepository.Current.Get<Block>("dirt");
        GrassBlock ??= ModuleRepository.Current.Get<Block>("grass_block");
    }
}
