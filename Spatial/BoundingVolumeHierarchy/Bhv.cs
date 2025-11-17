using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.Spatial.BoundingVolumeHierarchy;

/// <summary>
/// High-performance, thread-safe Bounding Volume Hierarchy for spatial queries.
/// Supports both static bulk construction and dynamic insertions/removals.
/// Works with or without explicit item IDs.
/// </summary>
public sealed class Bhv<T> : IDisposable, IEnumerable<T> where T : class, IBvhItem
{
	private const int MaxLeafItems = 8;
	private const int RebalanceThreshold = 1000;
	private const float SAH_TraversalCost = 1.0f;
	private const float SAH_IntersectionCost = 1.0f;

	private volatile BvhNode<T>? _root;
	private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
	private volatile int _count;
	private volatile int _operationsSinceRebalance;
	private volatile bool _disposed;

	/// <summary>
	/// Gets the number of items currently in the BVH
	/// </summary>
	public int Count => _count;

	/// <summary>
	/// Controls whether automatic rebalancing is enabled
	/// </summary>
	public bool AutoRebalance { get; set; } = true;

	/// <summary>
	/// Gets the bounding box of all items in the BVH
	/// </summary>
	public BoundingBox? WorldBounds
	{
		get
		{
			_lock.EnterReadLock();
			try
			{
				return _root?.BoundingBox;
			}
			finally
			{
				_lock.ExitReadLock();
			}
		}
	}

	/// <summary>
	/// Builds the BVH from a collection of items (most efficient for bulk loading)
	/// </summary>
	public void Build(IEnumerable<T> items)
	{
		ArgumentNullException.ThrowIfNull(items);
		ObjectDisposedException.ThrowIf(_disposed, this);

		var itemArray = items.ToArray();
		if (itemArray.Length == 0)
		{
			Clear();
			return;
		}

		_lock.EnterWriteLock();
		try
		{
			_root = BuildRecursive(itemArray.AsSpan());
			_count = itemArray.Length;
			_operationsSinceRebalance = 0;
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Inserts a single item into the BVH
	/// </summary>
	public void Insert(T item)
	{
		ArgumentNullException.ThrowIfNull(item);
		ObjectDisposedException.ThrowIf(_disposed, this);

		_lock.EnterWriteLock();
		try
		{
			if (!ContainsItem(item))
			{
				if (_root == null)
				{
					_root = BvhNode<T>.CreateLeaf([item]);
				}
				else
				{
					_root = InsertRecursive(_root, item);
				}

				Interlocked.Increment(ref _count);
				CheckForRebalance();
			}
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Inserts multiple items efficiently
	/// </summary>
	public void InsertRange(IEnumerable<T> items)
	{
		ArgumentNullException.ThrowIfNull(items);
		ObjectDisposedException.ThrowIf(_disposed, this);

		var newItems = items.Where(item => !ContainsItem(item)).ToArray();
		if (newItems.Length == 0) return;

		_lock.EnterWriteLock();
		try
		{
			if (_root == null)
			{
				_root = BuildRecursive(newItems.AsSpan());
			}
			else
			{
				foreach (var item in newItems)
				{
					_root = InsertRecursive(_root, item);
				}
			}

			Interlocked.Add(ref _count, newItems.Length);
			_operationsSinceRebalance += newItems.Length;
			CheckForRebalance();
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Removes an item from the BVH
	/// </summary>
	public bool Remove(T item)
	{
		ArgumentNullException.ThrowIfNull(item);
		ObjectDisposedException.ThrowIf(_disposed, this);

		_lock.EnterWriteLock();
		try
		{
			if (_root == null || !ContainsItem(item))
				return false;

			_root = RemoveRecursive(_root, item);
			if (_root != null && _root.IsLeaf && _root.ItemCount == 0)
			{
				_root = null;
			}

			Interlocked.Decrement(ref _count);
			CheckForRebalance();
			return true;
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Updates an item's position in the BVH (item's BoundingBox should already be updated)
	/// </summary>
	public bool Update(T item)
	{
		ArgumentNullException.ThrowIfNull(item);
		ObjectDisposedException.ThrowIf(_disposed, this);

		_lock.EnterWriteLock();
		try
		{
			if (_root == null || !ContainsItem(item))
				return false;

			// Remove and re-insert with updated bounding box
			_root = RemoveRecursive(_root, item);
			if (_root == null)
			{
				_root = BvhNode<T>.CreateLeaf([item]);
			}
			else
			{
				_root = InsertRecursive(_root, item);
			}

			_operationsSinceRebalance += 2;
			CheckForRebalance();
			return true;
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Forces a complete rebuild of the tree for optimal performance
	/// </summary>
	public void Rebalance()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		_lock.EnterWriteLock();
		try
		{
			var allItems = GetAllItems().ToArray();
			if (allItems.Length > 0)
			{
				_root = BuildRecursive(allItems.AsSpan());
			}
			else
			{
				_root = null;
			}
			_operationsSinceRebalance = 0;
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	/// <summary>
	/// Finds all items that intersect with the given bounding box
	/// </summary>
	public void Query(BoundingBox queryBox, List<T> results)
	{
		ArgumentNullException.ThrowIfNull(results);
		ObjectDisposedException.ThrowIf(_disposed, this);

		_lock.EnterReadLock();
		try
		{
			if (_root != null)
			{
				QueryRecursive(_root, queryBox, results);
			}
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}

	/// <summary>
	/// Finds all items that intersect with the given bounding box
	/// </summary>
	public List<T> Query(BoundingBox queryBox)
	{
		var results = new List<T>();
		Query(queryBox, results);
		return results;
	}

	/// <summary>
	/// Finds all items that contain the given point
	/// </summary>
	public void QueryPoint(WorldPosition point, List<T> results)
	{
		ArgumentNullException.ThrowIfNull(results);
		ObjectDisposedException.ThrowIf(_disposed, this);

		_lock.EnterReadLock();
		try
		{
			if (_root != null)
			{
				QueryPointRecursive(_root, point, results);
			}
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}

	/// <summary>
	/// Finds all items that contain the given point
	/// </summary>
	public List<T> QueryPoint(WorldPosition point)
	{
		var results = new List<T>();
		QueryPoint(point, results);
		return results;
	}

	/// <summary>
	/// Finds all items within the specified radius of a point
	/// </summary>
	public List<T> QueryRadius(WorldPosition center, int radius)
	{
		var queryBox = new BoundingBox(
				new WorldPosition(center.X - radius, center.Y - radius, center.Z - radius),
				new WorldPosition(center.X + radius, center.Y + radius, center.Z + radius)
		);

		var candidates = Query(queryBox);
		var results = new List<T>();
		long radiusSquared = (long)radius * radius;

		foreach (var item in candidates)
		{
			if (MinDistanceSquaredToBox(center, item.BoundingBox) <= radiusSquared)
			{
				results.Add(item);
			}
		}

		return results;
	}

	/// <summary>
	/// Finds the closest item to the given point
	/// </summary>
	public T? FindClosest(WorldPosition point, int maxDistance = int.MaxValue)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		_lock.EnterReadLock();
		try
		{
			if (_root == null) return null;

			T? closest = null;
			long closestDistanceSquared = (long)maxDistance * maxDistance;

			FindClosestRecursive(_root, point, ref closest, ref closestDistanceSquared);
			return closest;
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}

	/// <summary>
	/// Clears all items from the BVH
	/// </summary>
	public void Clear()
	{
		_lock.EnterWriteLock();
		try
		{
			_root = null;
			_count = 0;
			_operationsSinceRebalance = 0;
		}
		finally
		{
			_lock.ExitWriteLock();
		}
	}

	#region Private Implementation

	/// <summary>
	/// Gets all items by traversing the tree
	/// </summary>
	private IEnumerable<T> GetAllItems()
	{
		if (_root == null) yield break;

		var stack = new Stack<BvhNode<T>>();
		stack.Push(_root);

		while (stack.Count > 0)
		{
			var node = stack.Pop();

			if (node.IsLeaf)
			{
				var items = node.Items!;
				for (int i = 0; i < node.ItemCount; i++)
				{
					yield return items[i];
				}
			}
			else
			{
				if (node.Right != null) stack.Push(node.Right);
				if (node.Left != null) stack.Push(node.Left);
			}
		}
	}

	/// <summary>
	/// Checks if an item exists in the tree
	/// </summary>
	private bool ContainsItem(T item)
	{
		if (_root == null) return false;

		var stack = new Stack<BvhNode<T>>();
		stack.Push(_root);

		while (stack.Count > 0)
		{
			var node = stack.Pop();

			if (node.IsLeaf)
			{
				var items = node.Items!;
				for (int i = 0; i < node.ItemCount; i++)
				{
					if (ReferenceEquals(items[i], item))
						return true;
				}
			}
			else
			{
				// Only search children that could contain the item
				if (node.Right != null && node.Right.BoundingBox.Intersects(item.BoundingBox))
					stack.Push(node.Right);
				if (node.Left != null && node.Left.BoundingBox.Intersects(item.BoundingBox))
					stack.Push(node.Left);
			}
		}

		return false;
	}

	private static BvhNode<T> BuildRecursive(Span<T> items)
	{
		if (items.Length <= MaxLeafItems)
		{
			return BvhNode<T>.CreateLeaf(items);
		}

		var splitPos = FindBestSplit(items);
		int leftCount = Math.Max(1, Math.Min(items.Length - 1, splitPos));

		var leftItems = items[..leftCount];
		var rightItems = items[leftCount..];

		var leftChild = BuildRecursive(leftItems);
		var rightChild = BuildRecursive(rightItems);

		return BvhNode<T>.CreateInternal(leftChild, rightChild);
	}

	private static BvhNode<T> InsertRecursive(BvhNode<T> node, T item)
	{
		var expandedBounds = node.BoundingBox.Union(item.BoundingBox);

		if (node.IsLeaf)
		{
			if (node.ItemCount < MaxLeafItems)
			{
				var newItems = new T[node.ItemCount + 1];
				Array.Copy(node.Items!, newItems, node.ItemCount);
				newItems[node.ItemCount] = item;

				return new BvhNode<T>(expandedBounds)
				{
					Items = newItems,
					ItemCount = node.ItemCount + 1
				};
			}
			else
			{
				var allItems = new T[node.ItemCount + 1];
				Array.Copy(node.Items!, allItems, node.ItemCount);
				allItems[node.ItemCount] = item;
				return BuildRecursive(allItems.AsSpan());
			}
		}
		else
		{
			var leftCost = CalculateInsertionCost(node.Left!, item);
			var rightCost = CalculateInsertionCost(node.Right!, item);

			if (leftCost < rightCost)
			{
				return new BvhNode<T>(expandedBounds)
				{
					Left = InsertRecursive(node.Left!, item),
					Right = node.Right
				};
			}
			else
			{
				return new BvhNode<T>(expandedBounds)
				{
					Left = node.Left,
					Right = InsertRecursive(node.Right!, item)
				};
			}
		}
	}

	private static BvhNode<T>? RemoveRecursive(BvhNode<T> node, T item)
	{
		if (node.IsLeaf)
		{
			var newItems = new List<T>();
			var items = node.Items!;

			for (int i = 0; i < node.ItemCount; i++)
			{
				if (!ReferenceEquals(items[i], item))
				{
					newItems.Add(items[i]);
				}
			}

			return newItems.Count == 0 ? null : BvhNode<T>.CreateLeaf([.. newItems]);
		}
		else
		{
			BvhNode<T>? newLeft = node.Left;
			BvhNode<T>? newRight = node.Right;

			if (node.Left!.BoundingBox.Intersects(item.BoundingBox))
			{
				newLeft = RemoveRecursive(node.Left, item);
			}

			if (node.Right!.BoundingBox.Intersects(item.BoundingBox))
			{
				newRight = RemoveRecursive(node.Right, item);
			}

			if (newLeft == null && newRight == null) return null;
			if (newLeft == null) return newRight;
			if (newRight == null) return newLeft;

			return BvhNode<T>.CreateInternal(newLeft, newRight);
		}
	}

	private static int FindBestSplit(Span<T> items)
	{
		int bestAxis = 0;
		int bestPos = items.Length / 2;
		float bestCost = float.MaxValue;

		for (int axis = 0; axis < 3; axis++)
		{
			SortByAxis(items, axis);

			for (int i = 1; i < items.Length; i++)
			{
				var cost = CalculateSAHCost(items, i);
				if (cost < bestCost)
				{
					bestCost = cost;
					bestAxis = axis;
					bestPos = i;
				}
			}
		}

		if (bestAxis != 2)
		{
			SortByAxis(items, bestAxis);
		}

		return bestPos;
	}

	private static void SortByAxis(Span<T> items, int axis)
	{
		items.Sort((a, b) => GetCentroid(a.BoundingBox, axis).CompareTo(GetCentroid(b.BoundingBox, axis)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int GetCentroid(BoundingBox box, int axis) => axis switch
	{
		0 => (box.Start.X + box.End.X) / 2,
		1 => (box.Start.Y + box.End.Y) / 2,
		2 => (box.Start.Z + box.End.Z) / 2,
		_ => throw new ArgumentOutOfRangeException(nameof(axis))
	};

	private static float CalculateSAHCost(Span<T> items, int splitPos)
	{
		var leftBox = CalculateBoundingBox(items[..splitPos]);
		var rightBox = CalculateBoundingBox(items[splitPos..]);
		var parentBox = CalculateBoundingBox(items);

		long parentSA = parentBox.SurfaceArea;
		if (parentSA == 0) return float.MaxValue;

		long leftSA = leftBox.SurfaceArea;
		long rightSA = rightBox.SurfaceArea;

		float leftProb = (float)leftSA / parentSA;
		float rightProb = (float)rightSA / parentSA;

		return SAH_TraversalCost + SAH_IntersectionCost * (leftProb * splitPos + rightProb * (items.Length - splitPos));
	}

	private static BoundingBox CalculateBoundingBox(Span<T> items)
	{
		if (items.Length == 0)
			throw new ArgumentException("Cannot calculate bounding box for empty span");

		var result = items[0].BoundingBox;
		for (int i = 1; i < items.Length; i++)
		{
			result = BoundingBox.Union(result, items[i].BoundingBox);
		}
		return result;
	}


	private static float CalculateInsertionCost(BvhNode<T> node, T item)
	{
		var originalArea = node.BoundingBox.SurfaceArea;
		var expandedBox = node.BoundingBox.Union(item.BoundingBox);
		var expandedArea = expandedBox.SurfaceArea;
		return expandedArea - originalArea;
	}

	private void CheckForRebalance()
	{
		if (AutoRebalance && _operationsSinceRebalance >= RebalanceThreshold)
		{
			Task.Run(Rebalance);
		}
	}



	private static void QueryRecursive(BvhNode<T> node, BoundingBox queryBox, List<T> results)
	{
		if (!node.BoundingBox.Intersects(queryBox)) return;

		if (node.IsLeaf)
		{
			var items = node.Items!;
			for (int i = 0; i < node.ItemCount; i++)
			{
				if (items[i].BoundingBox.Intersects(queryBox))
				{
					results.Add(items[i]);
				}
			}
		}
		else
		{
			QueryRecursive(node.Left!, queryBox, results);
			QueryRecursive(node.Right!, queryBox, results);
		}
	}

	public IEnumerable<T> IteratePoint(WorldPosition position)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		_lock.EnterReadLock();
		try
		{
			if (_root != null)
			{
				foreach (var item in IteratePoint(position, _root))
					yield return item;
			}
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}

	private IEnumerable<T> IteratePoint(WorldPosition position, BvhNode<T> node)
	{
		if (!node.BoundingBox.Contains(position)) yield break;

		if (node.IsLeaf)
		{
			var items = node.Items!;
			for (int i = 0; i < node.ItemCount; i++)
			{
				if (items[i].BoundingBox.Contains(position))
				{
					yield return items[i];
				}
			}
		}
		else
		{
			foreach (var item in IteratePoint(position, node.Left!))
				yield return item;
			foreach (var item in IteratePoint(position, node.Right!))
				yield return item;
		}
	}

	private static void QueryPointRecursive(BvhNode<T> node, WorldPosition point, List<T> results)
	{
		if (!node.BoundingBox.Contains(point)) return;

		if (node.IsLeaf)
		{
			var items = node.Items!;
			for (int i = 0; i < node.ItemCount; i++)
			{
				if (items[i].BoundingBox.Contains(point))
				{
					results.Add(items[i]);
				}
			}
		}
		else
		{
			QueryPointRecursive(node.Left!, point, results);
			QueryPointRecursive(node.Right!, point, results);
		}
	}

	private static void FindClosestRecursive(BvhNode<T> node, WorldPosition point, ref T? closest, ref long closestDistanceSquared)
	{
		long minDistance = MinDistanceSquaredToBox(point, node.BoundingBox);
		if (minDistance > closestDistanceSquared) return;

		if (node.IsLeaf)
		{
			var items = node.Items!;
			for (int i = 0; i < node.ItemCount; i++)
			{
				long distance = MinDistanceSquaredToBox(point, items[i].BoundingBox);
				if (distance < closestDistanceSquared)
				{
					closestDistanceSquared = distance;
					closest = items[i];
				}
			}
		}
		else
		{
			long leftDist = MinDistanceSquaredToBox(point, node.Left!.BoundingBox);
			long rightDist = MinDistanceSquaredToBox(point, node.Right!.BoundingBox);

			if (leftDist < rightDist)
			{
				FindClosestRecursive(node.Left!, point, ref closest, ref closestDistanceSquared);
				FindClosestRecursive(node.Right!, point, ref closest, ref closestDistanceSquared);
			}
			else
			{
				FindClosestRecursive(node.Right!, point, ref closest, ref closestDistanceSquared);
				FindClosestRecursive(node.Left!, point, ref closest, ref closestDistanceSquared);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static long MinDistanceSquaredToBox(WorldPosition point, BoundingBox box)
	{
		long dx = Math.Max(0, Math.Max(box.Start.X - point.X, point.X - box.End.X));
		long dy = Math.Max(0, Math.Max(box.Start.Y - point.Y, point.Y - box.End.Y));
		long dz = Math.Max(0, Math.Max(box.Start.Z - point.Z, point.Z - box.End.Z));
		return dx * dx + dy * dy + dz * dz;
	}
	#endregion

	#region IEnumerable Implementation

	/// <summary>
	/// Returns an enumerator that iterates through all items in the BVH
	/// </summary>
	public IEnumerator<T> GetEnumerator()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		_lock.EnterReadLock();
		try
		{
			foreach (var item in GetAllItems())
			{
				yield return (T)item;
			}
		}
		finally
		{
			_lock.ExitReadLock();
		}
	}

	/// <summary>
	/// Returns an enumerator that iterates through all items in the BVH
	/// </summary>
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	#endregion

	public void Dispose()
	{
		if (!_disposed)
		{
			_disposed = true;
			_lock.Dispose();
		}
	}
}
