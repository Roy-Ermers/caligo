using WorldGen.Universe.PositionTypes;

namespace WorldGen.Generators.Transport.Ways;

public interface IWay
{
    TransportNode A { get; }
    TransportNode B { get; }

    bool Intersects(WorldPosition position);
}
