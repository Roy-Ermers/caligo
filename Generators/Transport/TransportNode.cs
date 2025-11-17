using WorldGen.Generators.Features;
using WorldGen.Universe.PositionTypes;
using WorldGen.Utils;

namespace WorldGen.Generators.Transport;

public abstract class TransportNode(Random random) : Feature(random)
{
    public Sector Sector { get; init; }
}
