namespace WorldGen.Spatial.BoundingVolumeHierarchy;


/// <summary>
/// Internal node structure for the BVH tree. Optimized for cache efficiency.
/// </summary>
internal sealed class BvhNode<T>(BoundingBox boundingBox) where T : IBvhItem
{
    public BoundingBox BoundingBox = boundingBox;
    public BvhNode<T>? Left;
    public BvhNode<T>? Right;
    public T[]? Items; // Leaf nodes store items directly
    public int ItemCount;

    public bool IsLeaf => Items != null;

    /// <summary>
    /// Creates a leaf node with the given items
    /// </summary>
    public static BvhNode<T> CreateLeaf(Span<T> items)
    {
        if (items.Length == 0)
            throw new ArgumentException("Leaf node must have at least one item");

        var boundingBox = items[0].BoundingBox;
        for (int i = 1; i < items.Length; i++)
        {
            boundingBox = BoundingBox.Union(boundingBox, items[i].BoundingBox);
        }

        var itemArray = new T[items.Length];
        items.CopyTo(itemArray);

        return new BvhNode<T>(boundingBox)
        {
            Items = itemArray,
            ItemCount = items.Length
        };
    }

    /// <summary>
    /// Creates an internal node with left and right children
    /// </summary>
    public static BvhNode<T> CreateInternal(BvhNode<T> left, BvhNode<T> right)
    {
        var boundingBox = left.BoundingBox.Union(right.BoundingBox);
        return new BvhNode<T>(boundingBox)
        {
            Left = left,
            Right = right
        };
    }
}
