using WorldGen.Noise;
using WorldGen.Universe.PositionTypes;
using WorldGen.Generators.Transport.Ways;

namespace WorldGen.Generators.Transport;

public class TransportNetwork
{
    readonly GradientNoise Noise;
    readonly int Spacing = 64;
    readonly int Seed;

    public Dictionary<int, TransportNode> Nodes = [];

    public TransportNetwork(int seed)
    {
        Seed = seed;
        Noise = new GradientNoise(seed);
    }

    public TransportNode GetNode(WorldPosition position)
    {
        int sectorX = position.X / Spacing * Spacing;
        int sectorZ = position.Z / Spacing * Spacing;
        int key = HashCode.Combine(sectorX, sectorZ);

        if (Nodes.TryGetValue(key, out var node))
        {
            return node;
        }

        float offsetX = Noise.Get2D(1f / (sectorX * 1f / Spacing), 1f / (sectorZ * 1f / Spacing)) * 0.5f + 0.5f;
        float offsetZ = Noise.Get2D(1f / (sectorZ * 1f / Spacing), 1f / (sectorX * 1f / Spacing)) * 0.5f + 0.5f;

        int nodeX = sectorX + (int)(offsetX * Spacing);
        int nodeZ = sectorZ + (int)(offsetZ * Spacing);

        var newNode = GenerateNode((sectorX, sectorZ), (nodeX, nodeZ));

        Nodes[key] = newNode;

        return newNode;
    }

    private static TransportNode GenerateNode((int X, int Z) sector, (int X, int Z) node)
    {
        var position = new WorldPosition(node.X, 0, node.Z);

        return new TransportNode
        {
            Position = position,
            Sector = new WorldPosition(sector.X, 0, sector.Z),
            BoundingBox = new(position, 1, 1, 1)
        };
    }
}
