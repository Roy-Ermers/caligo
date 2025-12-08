using Caligo.Core.Noise;
using Caligo.Core.Spatial;
using Caligo.Core.Spatial.PositionTypes;
using Random = Caligo.Core.Utils.Random;

namespace Caligo.Core.Generators.Features;

public class FeatureNetwork
{
    private readonly Heightmap _heightmap;
    private readonly Universe.Worlds.World _world;

    private readonly Mutex _writeLock = new();
    private readonly GradientNoise Noise;
    private readonly Dictionary<int, Sector> Sectors = [];
    private readonly int Seed;

    public FeatureNetwork(Universe.Worlds.World world, int seed, Heightmap heightmap)
    {
        Seed = seed;
        _world = world;
        _heightmap = heightmap;
        Noise = new GradientNoise(seed);
    }

    public Sector GetSector(WorldPosition position)
    {
        if (Sectors.TryGetValue(Sector.GetKey(position), out var sector)) return sector;

        sector = new Sector(position);
        _writeLock.WaitOne();
        Sectors[sector.Key] = sector;
        _writeLock.ReleaseMutex();

        // Use prime number mixing for better random distribution
        var seed = (sector.X * 73856093) ^ (sector.Z * 19349663) ^ (Seed * 83492791);

        var random = new Random(seed);
        var amount = random.Next(25, 100);

        for (var i = 0; i < amount; i++)
        {
            var newNode = GenerateNode(sector, random);
            sector.Nodes.Add(newNode);
        }

        return sector;
    }

    private Feature GenerateNode(Sector sector, Random random)
    {
        sector.Lock.EnterWriteLock();
        var offsetX = random.Next(Sector.SectorSize);
        var offsetZ = random.Next(Sector.SectorSize);
        var nodeX = sector.Start.X + offsetX;
        var nodeZ = sector.Start.Z + offsetZ;

        var nodeY = _heightmap.GetHeightAt(nodeX, nodeZ);
        var position = new WorldPosition(nodeX, (int)nodeY, nodeZ);

        var node = new Tree(random, position);
        _world.Features.Insert(node);
        sector.Lock.ExitWriteLock();

        return node;
    }
}