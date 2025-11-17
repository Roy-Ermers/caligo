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

            if (position.Y < height)
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
    }

    public int GetTerrainHeight(int x, int z)
    {
        var _x = x / 10f;
        var _z = z / 10f;

        var height = (8f * terrainNoise.Get2D(1 / 8f * _x, 1 / 8f * _z)
                        + 4f * terrainNoise.Get2D(1 / 4f * _x, 1 / 4f * _z)
                        + 2f * terrainNoise.Get2D(1 / 2f * _x, 1 / 2f * _z)
                        + 1f * terrainNoise.Get2D(1 * _x, 1 * _z)
                        + 0.5f * terrainNoise.Get2D(2 * _x, 2 * _z)
                        + 1 / 4f * terrainNoise.Get2D(4 * _x, 4 * _z)) / (8f + 4f + 2f + 1f + 0.5f + 1 / 4f);


        height = height * (MaxGroundLevel - MinGroundLevel) + MinGroundLevel;
        return (int)Math.Clamp(height, MinGroundLevel, MaxGroundLevel);
    }

    public void Initialize()
    {
        DirtBlock ??= ModuleRepository.Current.Get<Block>("dirt");
        GrassBlock ??= ModuleRepository.Current.Get<Block>("grass_block");
    }
}
