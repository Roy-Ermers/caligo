
using WorldGen.Universe;

namespace WorldGen.Generators.World;

public interface IWorldGenerator
{
    /// <summary>
    /// Generates a chunk at the specified position.
    /// </summary>
    /// <param name="chunk">The chunk to generate in.</param>
    /// <returns>The generated chunk.</returns>
    void GenerateChunk(ref Chunk chunk);

    /// <summary>
    /// Initializes the world generator.
    /// </summary>
    void Initialize();
}
