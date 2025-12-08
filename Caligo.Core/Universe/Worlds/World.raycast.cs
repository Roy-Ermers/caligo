using System.Numerics;
using Caligo.Core.Spatial;
using Caligo.Core.Spatial.PositionTypes;

namespace Caligo.Core.Universe.Worlds;

public partial class World
{
    /// <summary>
    ///     Cast a ray into the world and returns the first block hit within the max distance.
    ///     uses DDA algorithm for raycasting.
    /// </summary>
    /// <param name="ray">The ray to cast</param>
    /// <param name="maxDistance">The max distance to travel until to give up.</param>
    /// <param name="hitInfo"></param>
    /// <returns></returns>
    public bool Raycast(Ray ray, float maxDistance, out RaycastHit hitInfo)
    {
        var distanceTraveled = 0f;

        var positionX = (int)MathF.Round(ray.Origin.X);
        var positionY = (int)MathF.Round(ray.Origin.Y);
        var positionZ = (int)MathF.Round(ray.Origin.Z);

        var stepX = ray.Direction.X >= 0 ? 1 : -1;
        var stepY = ray.Direction.Y >= 0 ? 1 : -1;
        var stepZ = ray.Direction.Z >= 0 ? 1 : -1;

        var deltaX = ray.Direction.X == 0 ? float.MaxValue : MathF.Abs(1.0f / ray.Direction.X);
        var deltaY = ray.Direction.Y == 0 ? float.MaxValue : MathF.Abs(1.0f / ray.Direction.Y);
        var deltaZ = ray.Direction.Z == 0 ? float.MaxValue : MathF.Abs(1.0f / ray.Direction.Z);

        var firstBoundaryX = stepX >= 0 ? ray.Origin.X + 1.0f : ray.Origin.X;
        var firstBoundaryY = stepY >= 0 ? ray.Origin.Y + 1.0f : ray.Origin.Y;
        var firstBoundaryZ = stepZ >= 0 ? ray.Origin.Z + 1.0f : ray.Origin.Z;

        var maxX = Math.Abs(firstBoundaryX - ray.Origin.X) * deltaX;
        var maxY = Math.Abs(firstBoundaryY - ray.Origin.Y) * deltaY;
        var maxZ = Math.Abs(firstBoundaryZ - ray.Origin.Z) * deltaZ;
        var position = new WorldPosition(
            (int)MathF.Floor(positionX),
            (int)MathF.Floor(positionY),
            (int)MathF.Floor(positionZ)
        );

        if (TryGetBlock(position, out var blockId))
        {
            hitInfo = new RaycastHit
            {
                HitPoint = position,
                BlockId = blockId,
                Position = position,
                Normal = Vector3.Zero,
                Distance = 0f
            };
            return true;
        }

        while (maxX < maxDistance || maxY < maxDistance || maxZ < maxDistance)
        {
            float currentT; // The distance to the next intersection point
            int normalX = 0, normalY = 0, normalZ = 0; // The normal of the face hit

            // Find the shortest distance to the next boundary plane (X, Y, or Z)
            if (maxX < maxY && maxX < maxZ)
            {
                // Step across the YZ plane (X face)
                currentT = maxX;
                maxX += deltaX;
                positionX += stepX;
                normalX = -stepX; // Normal points opposite to the step direction (out of the hit face)
            }
            else if (maxY < maxZ)
            {
                // Step across the XZ plane (Y face)
                currentT = maxY;
                maxY += deltaY;
                positionY += stepY;
                normalY = -stepY;
            }
            else
            {
                // Step across the XY plane (Z face)
                currentT = maxZ;
                maxZ += deltaZ;
                positionZ += stepZ;
                normalZ = -stepZ;
            }

            if (currentT >= maxDistance) break;

            position = new WorldPosition(
                (int)MathF.Floor(positionX),
                (int)MathF.Floor(positionY),
                (int)MathF.Floor(positionZ)
            );

            if (!TryGetBlock(position, out var block)) continue;

            hitInfo = new RaycastHit
            {
                BlockId = block,
                Position = position,
                HitPoint = ray.GetPoint(currentT),
                Normal = new Vector3(normalX, normalY, normalZ),
                Distance = currentT
            };

            return true;
        }

        hitInfo = default;
        return false;
    }
}