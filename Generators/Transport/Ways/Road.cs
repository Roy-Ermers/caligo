using WorldGen.Universe.PositionTypes;
using System.Numerics;
namespace WorldGen.Generators.Transport.Ways;

public record struct Road(TransportNode A, TransportNode B) : IWay
{
    public TransportNode A { get; init; } = A;
    public TransportNode B { get; init; } = B;

    public readonly float Length => (((Vector3)A.Position - (Vector3)B.Position)).LengthSquared();

    public int Width = 3;

    public readonly bool Intersects(WorldPosition position)
    {
        // find distance to line AB
        Vector3 a = (Vector3)A.Position;
        Vector3 b = (Vector3)B.Position;

        Vector3 p = (Vector3)position;

        Vector3 ab = b - a;
        Vector3 ap = p - a;

        float abLengthSquared = ab.LengthSquared();
        if (abLengthSquared == 0)
            return (p - a).Length() < 2.0f; // A and B are the same point
        float t = Vector3.Dot(ap, ab) / abLengthSquared;
        t = Math.Clamp(t, 0, 1);

        Vector3 closestPoint = a + t * ab;
        return (p - closestPoint).Length() < Width; // within 2 units of the road
    }
}
