using Caligo.Core.Spatial;
using Caligo.Core.Spatial.BoundingVolumeHierarchy;
using Caligo.Core.Spatial.PositionTypes;
using Random = Caligo.Core.Utils.Random;

namespace Caligo.Core.Generators.Features;

public abstract class Feature(Random random) : IBvhItem
{
    protected Random Random = random;
    public BoundingBox BoundingBox { get; init; }

    public abstract ushort GetBlock(WorldPosition position);
}