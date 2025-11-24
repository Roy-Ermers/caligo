using System.Runtime.CompilerServices;
using Caligo.Core.Spatial.PositionTypes;

namespace Caligo.Core.Spatial;

public readonly struct BoundingBox
{
    public readonly WorldPosition Start;
    public readonly WorldPosition End;

    public readonly int Width => End.X - Start.X;
    public readonly int Height => End.Y - Start.Y;
    public readonly int Depth => End.Z - Start.Z;

    public readonly int SurfaceArea => 2 * (Width * Height + Width * Depth + Height * Depth);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BoundingBox(WorldPosition start, WorldPosition end)
    {
        Start = new WorldPosition(
          Math.Min(start.X, end.X),
          Math.Min(start.Y, end.Y),
          Math.Min(start.Z, end.Z)
        );
        End = new WorldPosition(
          Math.Max(start.X, end.X),
          Math.Max(start.Y, end.Y),
          Math.Max(start.Z, end.Z)
        );
    }

    public BoundingBox(int x1, int y1, int z1, int x2, int y2, int z2)
    {
        Start = new WorldPosition(
          Math.Min(x1, x2),
          Math.Min(y1, y2),
          Math.Min(z1, z2)
        );

        End = new WorldPosition(
          Math.Max(x1, x2),
          Math.Max(y1, y2),
          Math.Max(z1, z2)
        );
    }
    public BoundingBox(WorldPosition start, int width, int height, int depth)
    {
        Start = start;
        End = new WorldPosition(start.X + width, start.Y + height, start.Z + depth);
    }    
    
    public BoundingBox(WorldPosition start, int size) : this(start, size, size, size) { }
    

    /// <summary>
    /// Checks if a position is contained within this bounding box.
    /// </summary>
    /// <param name="position">The position to check for</param>
    /// <returns></returns>
    public readonly bool Contains(WorldPosition position)
    {
        return position.X >= Start.X && position.X <= End.X &&
               position.Y >= Start.Y && position.Y <= End.Y &&
               position.Z >= Start.Z && position.Z <= End.Z;
    }

    /// <summary>
    /// Checks if this bounding box intersects with another bounding box.
    /// </summary>
    /// <param name="other">Other bounding box.</param>
    /// <returns>Whether this box overlaps with another.</returns>
    public readonly bool Intersects(BoundingBox other)
    {
        return !(other.End.X < Start.X || other.Start.X > End.X ||
                 other.End.Y < Start.Y || other.Start.Y > End.Y ||
                 other.End.Z < Start.Z || other.Start.Z > End.Z);
    }

    public readonly BoundingBox MoveInto(BoundingBox other)
    {
        int newX = Start.X;
        int newY = Start.Y;
        int newZ = Start.Z;

        if (End.X > other.End.X)
        {
            newX -= End.X - other.End.X;
        }
        else if (Start.X < other.Start.X)
        {
            newX += other.Start.X - Start.X;
        }

        if (End.Y > other.End.Y)
        {
            newY -= End.Y - other.End.Y;
        }
        else if (Start.Y < other.Start.Y)
        {
            newY += other.Start.Y - Start.Y;
        }

        if (End.Z > other.End.Z)
        {
            newZ -= End.Z - other.End.Z;
        }
        else if (Start.Z < other.Start.Z)
        {
            newZ += other.Start.Z - Start.Z;
        }

        return new BoundingBox(new WorldPosition(newX, newY, newZ), Width, Height, Depth);
    }

    public BoundingBox Union(BoundingBox other) => Union(this, other);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BoundingBox Union(BoundingBox a, BoundingBox b)
    {
        var start = new WorldPosition(
          Math.Min(a.Start.X, b.Start.X),
          Math.Min(a.Start.Y, b.Start.Y),
          Math.Min(a.Start.Z, b.Start.Z)
        );

        var end = new WorldPosition(
                Math.Max(a.End.X, b.End.X),
                Math.Max(a.End.Y, b.End.Y),
                Math.Max(a.End.Z, b.End.Z)
        );
        return new BoundingBox(start, end);
    }

    public override readonly string ToString() => $"BoundingBox(Start: {Start}, End: {End})";
}
