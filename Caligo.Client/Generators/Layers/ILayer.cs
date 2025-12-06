using Caligo.Client.Generators.World;
using Caligo.Core.Universe;

namespace Caligo.Client.Generators.Layers;

public interface ILayer
{
    void Initialize(long seed, LayerWorldGenerator generator);
    void GenerateChunk(Chunk chunk);
}