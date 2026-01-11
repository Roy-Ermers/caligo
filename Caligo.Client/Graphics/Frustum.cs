using Caligo.Core.Spatial;
using OpenTK.Mathematics;

namespace Caligo.Client.Graphics;

/// <summary>
/// Represents a view frustum for culling objects outside the camera view.
/// Uses the Gribb-Hartmann method to extract planes from the view-projection matrix.
/// </summary>
public class Frustum
{
    private readonly Plane[] _planes = new Plane[6];

    /// <summary>
    /// Extracts frustum planes from the view-projection matrix.
    /// </summary>
    public void Update(Matrix4 viewProjection)
    {
        // Left plane
        _planes[0] = NormalizePlane(new Plane(
            viewProjection.M14 + viewProjection.M11,
            viewProjection.M24 + viewProjection.M21,
            viewProjection.M34 + viewProjection.M31,
            viewProjection.M44 + viewProjection.M41
        ));

        // Right plane
        _planes[1] = NormalizePlane(new Plane(
            viewProjection.M14 - viewProjection.M11,
            viewProjection.M24 - viewProjection.M21,
            viewProjection.M34 - viewProjection.M31,
            viewProjection.M44 - viewProjection.M41
        ));

        // Bottom plane
        _planes[2] = NormalizePlane(new Plane(
            viewProjection.M14 + viewProjection.M12,
            viewProjection.M24 + viewProjection.M22,
            viewProjection.M34 + viewProjection.M32,
            viewProjection.M44 + viewProjection.M42
        ));

        // Top plane
        _planes[3] = NormalizePlane(new Plane(
            viewProjection.M14 - viewProjection.M12,
            viewProjection.M24 - viewProjection.M22,
            viewProjection.M34 - viewProjection.M32,
            viewProjection.M44 - viewProjection.M42
        ));

        // Near plane
        _planes[4] = NormalizePlane(new Plane(
            viewProjection.M14 + viewProjection.M13,
            viewProjection.M24 + viewProjection.M23,
            viewProjection.M34 + viewProjection.M33,
            viewProjection.M44 + viewProjection.M43
        ));

        // Far plane
        _planes[5] = NormalizePlane(new Plane(
            viewProjection.M14 - viewProjection.M13,
            viewProjection.M24 - viewProjection.M23,
            viewProjection.M34 - viewProjection.M33,
            viewProjection.M44 - viewProjection.M43
        ));
    }

    /// <summary>
    /// Normalizes a plane equation.
    /// </summary>
    private static Plane NormalizePlane(Plane plane)
    {
        var length = MathF.Sqrt(plane.Normal.X * plane.Normal.X +
                                plane.Normal.Y * plane.Normal.Y +
                                plane.Normal.Z * plane.Normal.Z);

        return new Plane(
            plane.Normal.X / length,
            plane.Normal.Y / length,
            plane.Normal.Z / length,
            plane.D / length
        );
    }

    /// <summary>
    /// Tests if a bounding box intersects with the frustum.
    /// Returns true if the box is at least partially inside the frustum.
    /// </summary>
    public bool Intersects(BoundingBox boundingBox)
    {
        // Test all 6 planes
        foreach (var plane in _planes)
        {
            // Get the positive vertex (furthest point in direction of plane normal)
            var positiveVertex = new Vector3(
                plane.Normal.X >= 0 ? boundingBox.End.X : boundingBox.Start.X,
                plane.Normal.Y >= 0 ? boundingBox.End.Y : boundingBox.Start.Y,
                plane.Normal.Z >= 0 ? boundingBox.End.Z : boundingBox.Start.Z
            );

            // If the positive vertex is outside this plane, the box is completely outside
            if (DistanceToPlane(plane, positiveVertex) < 0)
                return false;
        }

        // Box intersects or is inside the frustum
        return true;
    }

    /// <summary>
    /// Calculates the signed distance from a point to a plane.
    /// </summary>
    private static float DistanceToPlane(Plane plane, Vector3 point)
    {
        return plane.Normal.X * point.X +
               plane.Normal.Y * point.Y +
               plane.Normal.Z * point.Z +
               plane.D;
    }

    /// <summary>
    /// Represents a plane in 3D space (Ax + By + Cz + D = 0).
    /// </summary>
    private struct Plane
    {
        public readonly Vector3 Normal;
        public readonly float D;

        public Plane(float a, float b, float c, float d)
        {
            Normal = new Vector3(a, b, c);
            D = d;
        }
    }
}