using System.Numerics;

namespace Caligo.Core.Spatial.PositionTypes;

public readonly record struct WorldPosition(int X, int Y, int Z)
{
    public static readonly WorldPosition Zero = new(0, 0, 0);

    public int X { get; init; } = X;
    public int Y { get; init; } = Y;
    public int Z { get; init; } = Z;

    public readonly int Id => HashCode.Combine(X, Y, Z);

    public ChunkPosition ChunkPosition => ChunkPosition.FromWorldPosition(this);

    public ChunkLocalPosition ChunkLocalPosition => ChunkLocalPosition.FromWorldPosition(this);

    override public readonly int GetHashCode()
    {
        return Id;
    }

    public override readonly string ToString()
    {
        return $"(W;{X}, {Y}, {Z})";
    }

    public static implicit operator WorldPosition((int X, int Y, int Z) position) => new(position.X, position.Y, position.Z);

    public static implicit operator Vector3(WorldPosition position) => new(position.X, position.Y, position.Z);

    public static WorldPosition operator +(WorldPosition left, WorldPosition right)
    {
        return new WorldPosition(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }
    public static WorldPosition operator -(WorldPosition left, WorldPosition right)
    {
        return new WorldPosition(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static WorldPosition operator +(WorldPosition left, int right)
    {
        return new WorldPosition(left.X + right, left.Y + right, left.Z + right);
    }

    public static WorldPosition operator +(WorldPosition left, Vector3 right)
    {
        return new WorldPosition(left.X + (int)right.X, left.Y + (int)right.Y, left.Z + (int)right.Z);
    }

    public static WorldPosition operator -(WorldPosition left, int right)
    {
        return new WorldPosition(left.X - right, left.Y - right, left.Z - right);
    }

    public static WorldPosition operator /(WorldPosition left, int right)
    {
        return new WorldPosition(left.X / right, left.Y / right, left.Z / right);
    }
}
