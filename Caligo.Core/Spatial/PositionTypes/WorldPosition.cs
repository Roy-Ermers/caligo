using System.Numerics;
using Caligo.ModuleSystem.Runtime.Attributes;

namespace Caligo.Core.Spatial.PositionTypes;

[JsModule]
public readonly record struct WorldPosition
{
    public static readonly WorldPosition Zero = new(0, 0, 0);

    public WorldPosition(int X, int Y, int Z)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }

    public int X { get; init; }
    public int Y { get; init; }
    public int Z { get; init; }

    public readonly int Id => HashCode.Combine(X, Y, Z);

    public ChunkPosition ChunkPosition => ChunkPosition.FromWorldPosition(this);

    public ChunkLocalPosition ChunkLocalPosition => ChunkLocalPosition.FromWorldPosition(this);

    public readonly override int GetHashCode()
    {
        return Id;
    }

    public readonly override string ToString()
    {
        return $"(W;{X}, {Y}, {Z})";
    }

    public static implicit operator WorldPosition((int X, int Y, int Z) position)
    {
        return new WorldPosition(position.X, position.Y, position.Z);
    }

    public static implicit operator Vector3(WorldPosition position)
    {
        return new Vector3(position.X, position.Y, position.Z);
    }

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

    public void Deconstruct(out int X, out int Y, out int Z)
    {
        X = this.X;
        Y = this.Y;
        Z = this.Z;
    }
}