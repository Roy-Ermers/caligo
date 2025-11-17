namespace WorldGen.Spatial.BoundingVolumeHierarchy;

/// <summary>
/// Interface for items that can be stored in a BVH
/// </summary>
public interface IBvhItem
{
    /// <summary>
    /// The bounding box of this item in world space
    /// </summary>
    BoundingBox BoundingBox { get; }
}
