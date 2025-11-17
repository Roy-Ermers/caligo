using WorldGen.Spatial;
using WorldGen.Spatial.BoundingVolumeHierarchy;
using WorldGen.Universe.PositionTypes;
namespace WorldGen.Generators.Features;

public abstract class Feature(Random random) : IBvhItem
{
    public BoundingBox BoundingBox { get; init; }
    protected Random Random = random;

    public abstract ushort GetBlock(WorldPosition position);
}
