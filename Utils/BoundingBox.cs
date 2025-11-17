using WorldGen.Universe.PositionTypes;

namespace WorldGen.Utils;

public readonly struct BoundingBox
{
    public readonly WorldPosition End;
    public readonly WorldPosition Start;

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
    public BoundingBox(WorldPosition start, int width, int height, int depth)
    {
        Start = start;
        End = new WorldPosition(start.X + width, start.Y + height, start.Z + depth);
    }

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

    public override readonly string ToString() => $"BoundingBox(Start: {Start}, End: {End})";
}
