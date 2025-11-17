using Caligo.Core.Generators.Features;
using Caligo.Core.Spatial.BoundingVolumeHierarchy;

namespace Caligo.Core.Universe.World;

public partial class World
{
    public readonly Bhv<Feature> Features = new();
}