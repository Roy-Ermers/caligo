using Caligo.Client.Generators.World;
using Caligo.Core.Noise;
using Caligo.Core.Spatial;
using Caligo.Core.Universe;
using Caligo.Core.Utils;

namespace Caligo.Client.Generators.Layers;

public class HeightLayer : ILayer
{
    private GradientNoise noise;
    public Heightmap HeightMap { get; private set; }

    public void Initialize(long seed, LayerWorldGenerator _)
    {
        noise = new GradientNoise((int)seed);
        const int octaves = 4;
        const float frequency = 0.005f;
        HeightMap = new Heightmap((x, z) =>
        {
            var offset = noise.Get2DVector(x * frequency, z * frequency);
            var value = 0f;
            for (var octave = 0; octave < octaves; octave++)
            {
                var freq = frequency * (1 << octave);
                value += noise.Get2D(x * freq + offset.X, z * freq + offset.Y) / (1 << octave);
            }

            return Easings.EaseInCubic(value * 0.5f + 0.5f) * 150f;
        });
    }

    public void GenerateChunk(Chunk chunk)
    {
    }
}