namespace Caligo.Core.Utils;

public readonly struct ChangedIndex<T> where T : IEquatable<T>
{
    public int Index { get; init; }
    public T Value { get; init; }
}

public class TrackedQueue<T> where T : IEquatable<T>
{
    private readonly HashSet<int> _changedIndices = [];
    private readonly List<T> items = [];
    private int _head;

    public bool IsDirty { get; set; } = true;

    public void Enqueue(T item)
    {
        if (_head < items.Count && items[_head].Equals(item))
        {
            _head++;
            return;
        }

        if (_head < items.Count)
            items[_head] = item;
        else
            items.Add(item);

        _changedIndices.Add(_head);
        _head++;
        IsDirty = true;
    }

    public void EnqueueRange(IEnumerable<T> newItems)
    {
        foreach (var item in newItems) Enqueue(item);
    }

    public void Reset()
    {
        _head = 0;
        IsDirty = false;
        _changedIndices.Clear();
    }

    public bool TryUpdate(out IEnumerable<T> updatedItems)
    {
        if (IsDirty)
        {
            updatedItems = items;
            _head = 0;
            IsDirty = false;
            return true;
        }

        updatedItems = [];
        Reset();
        return false;
    }

    public bool TryGetChangedIndices(out IEnumerable<ChangedIndex<T>> changedIndices)
    {
        if (_changedIndices.Count > 0)
        {
            changedIndices = _changedIndices.Select<int, ChangedIndex<T>>(index => new ChangedIndex<T>
            {
                Value = items[index],
                Index = index
            });

            _changedIndices.Clear();
            return true;
        }

        changedIndices = [];
        return false;
    }
}