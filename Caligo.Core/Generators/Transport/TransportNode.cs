using Caligo.Core.Generators.Features;
using Random = Caligo.Core.Utils.Random;

namespace Caligo.Core.Generators.Transport;

public abstract class TransportNode(Random random) : Feature(random)
{
    public Sector Sector { get; init; }
}
