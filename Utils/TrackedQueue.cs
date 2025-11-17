namespace WorldGen.Utils;


public readonly struct ChangedIndex<T> where T : IEquatable<T>
{
    public int Index { get; init; }
    public T Value { get; init; }
}
public class TrackedQueue<T> where T : IEquatable<T>
{
    private List<T> items = [];

    private HashSet<int> _changedIndices = [];
    private int _head = 0;
    private bool _isDirty = true;

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
        _isDirty = true;
    }

    public void EnqueueRange(IEnumerable<T> newItems)
    {
        foreach (var item in newItems)
        {
            Enqueue(item);
        }
    }


    public bool IsDirty
    {
        get => _isDirty;
        set => _isDirty = value;
    }

    public void Reset()
    {
        _head = 0;
        _isDirty = false;
        _changedIndices.Clear();
        
    }

    public bool TryUpdate(out IEnumerable<T> updatedItems)
    {
        if (_isDirty)
        {
            updatedItems = items;
            _head = 0;
            _isDirty = false;
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
            changedIndices = _changedIndices.Select<int, ChangedIndex<T>>(index => new()
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
