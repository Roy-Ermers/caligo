using System.Numerics;
using Caligo.Core.Universe;

namespace Caligo.Core.Spatial.PositionTypes;

public readonly record struct ChunkPosition
{
    public static readonly ChunkPosition Zero = new(0, 0, 0);

    public ChunkPosition(int X, int Y, int Z)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }

    public int X { get; init; }
    public int Y { get; init; }
    public int Z { get; init; }

    public readonly int Id => HashCode.Combine(X, Y, Z);

    public readonly WorldPosition ToWorldPosition()
    {
        return new WorldPosition(X * Chunk.Size, Y * Chunk.Size, Z * Chunk.Size);
    }

    public readonly override int GetHashCode()
    {
        return Id;
    }

    public readonly override string ToString()
    {
        return $"(C;{X}, {Y}, {Z})";
    }

    public static ChunkPosition FromWorldPosition(WorldPosition position)
    {
        return FromWorldPosition(position.X, position.Y, position.Z);
    }

    public static ChunkPosition FromWorldPosition(int x, int y, int z)
    {
        return new ChunkPosition
        {
            X = (int)Math.Floor((float)x / Chunk.Size),
            Y = (int)Math.Floor((float)y / Chunk.Size),
            Z = (int)Math.Floor((float)z / Chunk.Size)
        };
    }

    public static implicit operator ChunkPosition((int X, int Y, int Z) position)
    {
        return new ChunkPosition(position.X * Chunk.Size, position.Y * Chunk.Size, position.Z * Chunk.Size);
    }

    public static implicit operator Vector3(ChunkPosition position)
    {
        return new Vector3(position.X * Chunk.Size, position.Y * Chunk.Size, position.Z * Chunk.Size);
    }

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

    public void Deconstruct(out int X, out int Y, out int Z)
    {
        X = this.X;
        Y = this.Y;
        Z = this.Z;
    }
}