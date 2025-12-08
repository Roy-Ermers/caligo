using System.Numerics;
using Caligo.Core.Universe;
using Caligo.Core.Utils;

namespace Caligo.Core.Spatial.PositionTypes;

public readonly record struct ChunkLocalPosition
{
    public static readonly ChunkLocalPosition Zero = new(0, 0, 0);

    public ChunkLocalPosition(int X, int Y, int Z)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }

    public readonly int Id => HashCode.Combine(X, Y, Z);

    public int Index => ZOrderCurve.GetIndex(X, Y, Z);
    public int X { get; init; }
    public int Y { get; init; }
    public int Z { get; init; }

    public readonly WorldPosition ToWorldPosition(ChunkPosition chunkPosition)
    {
        var (chunkX, chunkY, chunkZ) = chunkPosition.ToWorldPosition();
        return new WorldPosition(chunkX + X, chunkY + Y, chunkZ + Z);
    }

    public readonly override int GetHashCode()
    {
        return Id;
    }

    public readonly override string ToString()
    {
        return $"(L;{X}, {Y}, {Z})";
    }

    public static ChunkLocalPosition FromWorldPosition(WorldPosition position)
    {
        return FromWorldPosition(position.X, position.Y, position.Z);
    }

    public static ChunkLocalPosition FromWorldPosition(int x, int y, int z)
    {
        return new ChunkLocalPosition
        {
            X = MathExtensions.Mod(x, Chunk.Size),
            Y = MathExtensions.Mod(y, Chunk.Size),
            Z = MathExtensions.Mod(z, Chunk.Size)
        };
    }

    public static ChunkLocalPosition FromIndex(int index)
    {
        return ZOrderCurve.GetPosition(index, Chunk.Size);
    }

    public static implicit operator ChunkLocalPosition((int X, int Y, int Z) position)
    {
        return new ChunkLocalPosition(position.X, position.Y, position.Z);
    }

    public static implicit operator Vector3(ChunkLocalPosition position)
    {
        return new Vector3(position.X, position.Y, position.Z);
    }

    public static ChunkLocalPosition operator +(ChunkLocalPosition left, ChunkLocalPosition right)
    {
        return new ChunkLocalPosition(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    }

    public static ChunkLocalPosition operator -(ChunkLocalPosition left, ChunkLocalPosition right)
    {
        return new ChunkLocalPosition(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    }

    public static ChunkLocalPosition operator +(ChunkLocalPosition left, Vector3 right)
    {
        return new ChunkLocalPosition(left.X + (int)right.X, left.Y + (int)right.Y, left.Z + (int)right.Z);
    }

    public static ChunkLocalPosition operator -(ChunkLocalPosition left, Vector3 right)
    {
        return new ChunkLocalPosition(left.X - (int)right.X, left.Y - (int)right.Y, left.Z - (int)right.Z);
    }

    public void Deconstruct(out int X, out int Y, out int Z)
    {
        X = this.X;
        Y = this.Y;
        Z = this.Z;
    }
}