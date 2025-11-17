using WorldGen.Universe.PositionTypes;

namespace WorldGen.Universe.WorldGenerators;

public interface IWorldGenerator
{
    /// <summary>
    /// Generates a chunk at the specified position.
    /// </summary>
    /// <param name="chunk">The chunk to generate in.</param>
    /// <returns>The generated chunk.</returns>
    Chunk GenerateChunk(ref Chunk chunk);

    /// <summary>
    /// Initializes the world generator.
    /// </summary>
    void Initialize();
}
