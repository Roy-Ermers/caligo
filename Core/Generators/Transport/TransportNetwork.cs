using Caligo.Core.Generators.Features;
using Caligo.Core.Noise;
using Caligo.Core.Spatial.PositionTypes;

namespace Caligo.Core.Generators.Transport;

public class TransportNetwork
{
    readonly GradientNoise Noise;
    readonly int Seed;
    private readonly Universe.World.World _world;
    private readonly Dictionary<int, Sector> Sectors = [];

    readonly Mutex _writeLock = new();

    public TransportNetwork(Universe.World.World world, int seed)
    {
        Seed = seed;
        Noise = new GradientNoise(seed);
    }

    public Sector GetSector(WorldPosition position)
    {
        if (Sectors.TryGetValue(Sector.GetKey(position), out var sector))
        {
            return sector;
        }

        sector = new Sector(position);
        _writeLock.WaitOne();
        Sectors[sector.Key] = sector;
        _writeLock.ReleaseMutex();

        // Use prime number mixing for better random distribution
        var seed = (sector.X * 73856093) ^ (sector.Z * 19349663) ^ (Seed * 83492791);

        var random = new Random(seed);

        var newNode = GenerateNode(sector, random);
        sector.Nodes.Add(newNode);

        return sector;
    }

    private Tree GenerateNode(Sector sector, Random random)
    {
        
        sector.Lock.EnterWriteLock();
        var offsetX = random.NextSingle();
        var offsetZ = random.NextSingle();
        var nodeX = (int)(sector.Start.X + offsetX * Sector.SectorSize);
        var nodeZ = (int)(sector.Start.Z + offsetZ * Sector.SectorSize);

        var position = new WorldPosition(nodeX, 1, nodeZ);

        var node = new Tree(random, position);
        _world.Features.Insert(node);
        sector.Lock.ExitWriteLock();

        return node;
    }
}
