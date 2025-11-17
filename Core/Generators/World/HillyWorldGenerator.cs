using Caligo.Core.ModuleSystem;
using Caligo.Core.Noise;
using Caligo.Core.Resources.Block;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;
using Caligo.Core.Utils;

namespace Caligo.Core.Generators.World;

public class HillyWorldGenerator : IWorldGenerator
{
    private readonly int _seed;
    private readonly Random _random;

    // Blocks
    public Block DirtBlock { get; set; } = null!;
    public Block GrassBlock { get; set; } = null!;

    // Noise generators
    private readonly GradientNoise _continentNoise;      // Large-scale landmass shape
    private readonly GradientNoise _terrainNoise;        // Medium-scale hills and valleys
    private readonly GradientNoise _mountainNoise;       // Sharp mountain peaks
    private readonly GradientNoise _detailNoise;         // Small-scale variation
    private readonly GradientNoise _erosionNoise;        // Erosion patterns
    private readonly GradientNoise _ridgeNoise;          // Ridge lines

    // Terrain parameters
    public float MinGroundLevel = 0;
    public float MaxGroundLevel = 256;
    public float Frequency = 0.01f;
    public int Octaves = 2;

    public HillyWorldGenerator(int seed)
    {
        this._seed = seed;
        this._random = new Random(seed);

        // Initialize noise generators with different seeds
        _continentNoise = new GradientNoise(seed);
        _terrainNoise = new GradientNoise(seed + 3);
        _mountainNoise = new GradientNoise(seed + 7);
        _detailNoise = new GradientNoise(seed + 11);
        _erosionNoise = new GradientNoise(seed + 13);
        _ridgeNoise = new GradientNoise(seed + 17);
    }

    private float EaseInBack(float value)
    {
        const float c1 = 1.70158f;
        const float c3 = 2.70158f;
        
        return c3 * value * value * value - c1 * value * value;
    }
    
    private float EaseOutBack(float value)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        return 1f + c3 * MathF.Pow(value - 1f, 3) + c1 * MathF.Pow(value - 1f, 2);
    }

    /// <summary>
    /// Creates ridge-like features by taking the absolute value and inverting noise
    /// </summary>
    private float RidgeNoise(float x, float z, GradientNoise noise, float frequency)
    {
        float value = noise.Get2D(x * frequency, z * frequency);
        return 1f - MathF.Abs(value);
    }

    /// <summary>
    /// Generates multi-octave noise for more detailed terrain
    /// </summary>
    private float OctaveNoise(float x, float z, GradientNoise noise, int octaves, float frequency, float persistence = 0.5f)
    {
        float total = 0f;
        float amplitude = 1f;
        float maxValue = 0f;
        
        for (int i = 0; i < octaves; i++)
        {
            total += noise.Get2D(x * frequency, z * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2f;
        }
        
        return total / maxValue; // Normalize to -1 to 1
    }

    /// <summary>
    /// Creates plateau-like features with flat tops
    /// </summary>
    private float PlateauNoise(float x, float z, GradientNoise noise, float frequency, float sharpness = 2f)
    {
        float value = noise.Get2D(x * frequency, z * frequency);
        return MathF.Pow(MathF.Max(0, value), sharpness);
    }

    public void GenerateChunk(ref Chunk chunk)
    {
        foreach (WorldPosition position in new CubeIterator(chunk))
        {
            float x = position.X * 2f;
            float z = position.Z * 2f;
            
            // === DOMAIN WARPING ===
            // Distort the sampling coordinates for more organic shapes
            var (warpX, warpZ) = _detailNoise.Get2DVector(x * 0.003f, z * 0.003f);
            x += warpX * 40f;
            z += warpZ * 40f;
            
            // === CONTINENTAL SCALE ===
            // Very large scale height variation (entire continents)
            float continentScale = OctaveNoise(x, z, _continentNoise, 3, 0.0003f, 0.5f);
            continentScale = (continentScale + 1f) * 0.5f; // Normalize to 0-1
            continentScale = MathF.Pow(continentScale, 1.5f); // Make more dramatic
            
            // === BASE TERRAIN ===
            // Rolling hills and valleys
            float baseTerrain = OctaveNoise(x, z, _terrainNoise, 4, 0.002f, 0.5f);
            baseTerrain = (baseTerrain + 1f) * 0.5f; // Normalize to 0-1
            
            // === MOUNTAIN RANGES ===
            // Sharp mountain peaks using ridge noise
            float mountains = RidgeNoise(x, z, _mountainNoise, 0.001f);
            mountains = MathF.Pow(mountains, 2f); // Sharpen peaks
            
            // Add detail to mountains with octave noise
            float mountainDetail = OctaveNoise(x, z, _mountainNoise, 3, 0.004f, 0.4f);
            mountains = mountains * (0.7f + mountainDetail * 0.3f);
            
            // === RIDGE LINES ===
            // Create additional ridge features
            float ridges = RidgeNoise(x, z, _ridgeNoise, 0.0015f);
            ridges = MathF.Pow(ridges, 1.5f) * 0.5f;
            
            // === EROSION ===
            // Add erosion patterns that carve valleys
            float erosion = _erosionNoise.Get2D(x * 0.002f, z * 0.002f);
            erosion = (erosion + 1f) * 0.5f; // Normalize to 0-1
            erosion = MathF.Pow(erosion, 2f); // Make valleys sharper
            
            // === COMBINE ALL LAYERS ===
            // Start with base terrain
            float height = baseTerrain * 0.3f;
            
            // Add continental variation
            height += continentScale * 0.4f;
            
            // Add mountains where continent scale is high
            height += mountains * continentScale * 0.4f;
            
            // Add ridges
            height += ridges * 0.2f;
            
            // Apply erosion (carve valleys)
            height *= (0.5f + erosion * 0.5f);
            
            // Add fine detail noise
            float detail = _detailNoise.Get2D(x * 0.01f, z * 0.01f) * 0.05f;
            height += detail;
            
            // === SCALE TO WORLD HEIGHT ===
            // Use easing function for more dramatic terrain
            height = EaseOutBack(height);
            height = height * (MaxGroundLevel - MinGroundLevel) + MinGroundLevel;
            
            // === PLACE BLOCKS ===
            Block? blockToPlace = null;
            
            // Add 3D caves/overhangs using 3D noise
            float caveNoise = _detailNoise.Get3D(x * 0.05f, position.Y * 0.05f, z * 0.05f);
            bool isCave = caveNoise > 0.6f;
            
            if (position.Y < height && !isCave)
            {
                // Determine depth from surface
                float depthFromSurface = height - position.Y;
                
                if (depthFromSurface < 1f)
                {
                    // Surface layer - grass
                    blockToPlace = GrassBlock;
                }
                else if (depthFromSurface < 4f)
                {
                    // Shallow underground - dirt
                    blockToPlace = DirtBlock;
                }
                else
                {
                    // Deep underground - stone
                    blockToPlace = ModuleRepository.Current.Get<Block>("stone");
                }
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
