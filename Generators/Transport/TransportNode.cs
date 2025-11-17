using WorldGen.Universe.PositionTypes;
using WorldGen.Utils;

namespace WorldGen.Generators.Transport;

public class TransportNode
{
    public WorldPosition Position { get; init; }

    public WorldPosition Sector { get; init; }

    public BoundingBox BoundingBox { get; init; }
}
