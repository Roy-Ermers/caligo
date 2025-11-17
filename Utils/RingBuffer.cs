using System.Collections;

namespace WorldGen.Utils;

public class RingBuffer<T> : IEnumerable<T>
{
    public int Size { get; }
    private T?[] Buffer { get; }

    private int _head = 0;
    private int _tail = 0;

    public event Action<T>? OnItemRemoved;

    public RingBuffer(int size)
    {
        if (size <= 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be greater than zero.");
        Size = size;
        Buffer = new T[size];
        _head = 0;
        _tail = 0;
    }

    public RingBuffer(IEnumerable<T> items) : this(items.Count())
    {
        foreach (var item in items)
        {
            Enqueue(item);
        }
    }

    public void Enqueue(T item)
    {
        if (Buffer[_head] is not null)
        {
            OnItemRemoved?.Invoke(Buffer[_head]!);
        }

        Buffer[_head] = item;
        _head = (_head + 1) % Size;

        if (_head == _tail) // Buffer is full, move tail forward
        {
            _tail = (_tail + 1) % Size;
        }
    }

    public void Dequeue()
    {
        if (_tail == _head) // Buffer is empty
            throw new InvalidOperationException("Buffer is empty.");

        var item = Buffer[_tail];
        OnItemRemoved?.Invoke(item!);
        Buffer[_tail] = default; // Clear the slot
        _tail = (_tail + 1) % Size;
    }

    public void Remove(T item)
    {
        for (int i = 0; i < Size; i++)
        {
            if (Buffer[i]?.Equals(item) ?? false)
            {
                Buffer[i] = default; // Clear the slot
                OnItemRemoved?.Invoke(item);
                if (i == _tail) // If we removed the tail, move it forward
                {
                    _tail = (_tail + 1) % Size;
                }
                return;
            }
        }
        throw new InvalidOperationException("Item not found in the buffer.");
    }

    public bool Has(T item)
    {
        for (int i = 0; i < Size; i++)
        {
            if (Buffer[i]?.Equals(item) ?? false)
                return true;
        }
        return false;
    }

    public bool Has(Func<T, bool> predicate)
    {
        for (int i = 0; i < Size; i++)
        {
            if (Buffer[i] is not null && predicate(Buffer[i]!))
                return true;
        }
        return false;
    }

    public IEnumerator<T> GetEnumerator()
    {
        int count = (_head - _tail + Size) % Size;
        for (int i = 0; i < count; i++)
        {
            yield return Buffer[(_tail + i) % Size]!;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
