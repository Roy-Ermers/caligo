using WorldGen.Universe.PositionTypes;

namespace WorldGen.Universe;

public record struct ChunkLoader(ChunkPosition Position, int Ticks = 1);