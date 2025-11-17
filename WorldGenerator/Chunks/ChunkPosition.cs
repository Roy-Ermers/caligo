using System.Net.NetworkInformation;
using System.Numerics;
using WorldGen.Utils;

namespace WorldGen.WorldGenerator.Chunks;

public readonly record struct ChunkPosition(int X, int Y, int Z)
{
    public static readonly ChunkPosition Zero = new(0, 0, 0);

    public int X { get; init; } = X;
    public int Y { get; init; } = Y;
    public int Z { get; init; } = Z;

    public readonly int Id => HashCode.Combine(X, Y, Z);

    public int Index => ZOrderCurve.GetIndex(X, Y, Z, Chunk.Size);

    public readonly (int x, int y, int z) ToWorldPosition()
    {
        return (X * Chunk.Size, Y * Chunk.Size, Z * Chunk.Size);
    }

    override public readonly int GetHashCode()
    {
        return Id;
    }

    public override readonly string ToString()
    {
        return $"(C;{X}, {Y}, {Z})";
    }

    public static ChunkPosition FromWorldPosition(WorldPosition position) => FromWorldPosition(position.X, position.Y, position.Z);

    public static ChunkPosition FromWorldPosition(int x, int y, int z)
    {
        return new ChunkPosition
        {
            X = x / Chunk.Size,
            Y = y / Chunk.Size,
            Z = z / Chunk.Size
        };
    }

    public static ChunkPosition FromIndex(int index) => ZOrderCurve.GetPosition(index, Chunk.Size);

    public static implicit operator ChunkPosition((int X, int Y, int Z) position) => new(position.X, position.Y, position.Z);
    public static implicit operator System.Numerics.Vector3(ChunkPosition position) => new(position.X, position.Y, position.Z);

    public static ChunkPosition operator +(ChunkPosition left, ChunkPosition right)
    {
        return new ChunkPosition(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    public static ChunkPosition operator -(ChunkPosition left, ChunkPosition right)
    {
        return new ChunkPosition(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static ChunkPosition operator +(ChunkPosition left, Vector3 right)
    {
        return new ChunkPosition(left.X + (int)right.X, left.Y + (int)right.Y, left.Z + (int)right.Z);
    }

    public static ChunkPosition operator -(ChunkPosition left, Vector3 right)
    {
        return new ChunkPosition(left.X - (int)right.X, left.Y - (int)right.Y, left.Z - (int)right.Z);
    }
}
