using System.Diagnostics.CodeAnalysis;
using Caligo.Core.Spatial;
using Caligo.Core.Spatial.PositionTypes;

namespace Caligo.Core.Generators.Features;

public struct Sector
{
    public const int SectorSize = 64;

    public int X { get; init; }
    public int Z { get; init; }

    public readonly ReaderWriterLockSlim Lock = new();

    public readonly WorldPosition Start => new(X * SectorSize, int.MinValue, Z * SectorSize);
    public readonly WorldPosition End => new((X + 1) * SectorSize - 1, int.MaxValue, (Z + 1) * SectorSize - 1);

    public readonly BoundingBox BoundingBox => new(Start, End);

    public readonly int Key => HashCode.Combine(X, Z);

    public List<Feature> Nodes = [];

    public Sector(int x, int z)
    {
        X = x;
        Z = z;
    }

    public Sector(WorldPosition worldPosition)
    {
        X = (int)MathF.Floor((float)worldPosition.X / SectorSize);
        Z = (int)MathF.Floor((float)worldPosition.Z / SectorSize);
    }

    public static int GetKey(WorldPosition position)
    {
        int x = (int)MathF.Floor((float)position.X / SectorSize);
        int z = (int)MathF.Floor((float)position.Z / SectorSize);
        return HashCode.Combine(x, z);
    }


    public readonly bool GetNodeAt(WorldPosition position, [NotNullWhen(true)] out Feature? matchingNode)
    {
        foreach (var node in Nodes)
        {
            if (node.BoundingBox.Contains(position))
            {
                matchingNode = node;
                return true;
            }
        }

        matchingNode = null;
        return false;
    }

}
