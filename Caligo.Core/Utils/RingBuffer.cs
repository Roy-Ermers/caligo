using System.Collections;
using System.Runtime.CompilerServices;

namespace Caligo.Core.Utils;

/**
 * Fixed-capacity ring buffer with efficient push/pop from both ends.
 * When full, new elements overwrite oldest. Use for chat history, frame buffers, etc.
 */
public class RingBuffer<T> : IEnumerable<T>
{
    private readonly T[] buf;
    private int head; // index of first element
    private int tail; // index after last element (next write position)

    public RingBuffer(int capacity)
    {
        if (capacity < 1) throw new ArgumentException("Capacity must be positive", nameof(capacity));
        buf = new T[capacity];
        head = 0;
        tail = 0;
        Count = 0;
    }

    public int Capacity
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => buf.Length;
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    public bool IsFull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Count == buf.Length;
    }

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Count == 0;
    }

    /**
     * Logical indexer - [0] is oldest element, [Count-1] is newest.
     */
    public ref T this[int idx]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)idx >= (uint)Count) ThrowIndexOutOfRange();
            return ref buf[WrapIndex(head + idx)];
        }
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        for (var i = 0; i < Count; i++) yield return buf[WrapIndex(head + i)];
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable<T>)this).GetEnumerator();
    }

    public bool Contains(T item)
    {
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < Count; i++)
            if (comparer.Equals(this[i], item))
                return true;

        return false;
    }

    /**
     * Add to back (newest). Overwrites oldest if full.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushBack(T item)
    {
        buf[tail] = item;
        tail = WrapIndex(tail + 1);
        if (Count == buf.Length)
            head = tail; // overwrite oldest
        else
            Count++;
    }

    /**
     * Add to front (oldest). Overwrites newest if full.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PushFront(T item)
    {
        head = WrapIndex(head - 1);
        buf[head] = item;
        if (Count == buf.Length)
            tail = head; // overwrite newest
        else
            Count++;
    }

    /**
     * Remove from back (newest). Throws if empty.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T PopBack()
    {
        if (Count == 0) ThrowEmpty();
        tail = WrapIndex(tail - 1);
        var item = buf[tail];
        buf[tail] = default!;
        Count--;
        return item;
    }

    /**
     * Remove from front (oldest). Throws if empty.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T PopFront()
    {
        if (Count == 0) ThrowEmpty();
        var item = buf[head];
        buf[head] = default!;
        head = WrapIndex(head + 1);
        Count--;
        return item;
    }

    /**
     * Peek oldest element without removing.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Front()
    {
        if (Count == 0) ThrowEmpty();
        return ref buf[head];
    }

    /**
     * Peek newest element without removing.
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T Back()
    {
        if (Count == 0) ThrowEmpty();
        return ref buf[WrapIndex(tail - 1)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>()) Array.Clear(buf, 0, buf.Length);
        head = 0;
        tail = 0;
        Count = 0;
    }

    /**
     * Copy to array in logical order (oldest to newest).
     */
    public T[] ToArray()
    {
        if (Count == 0) return [];
        var result = new T[Count];
        if (head < tail)
        {
            // contiguous
            Array.Copy(buf, head, result, 0, Count);
        }
        else
        {
            // wrapped
            var firstLen = buf.Length - head;
            Array.Copy(buf, head, result, 0, firstLen);
            Array.Copy(buf, 0, result, firstLen, tail);
        }

        return result;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int WrapIndex(int idx)
    {
        var cap = buf.Length;
        if (idx >= cap) return idx - cap;
        if (idx < 0) return idx + cap;
        return idx;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowIndexOutOfRange()
    {
        throw new IndexOutOfRangeException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowEmpty()
    {
        throw new InvalidOperationException("Buffer is empty");
    }

    /**
     * Struct enumerator - foreach with no allocations.
     * Iterates from oldest to newest.
     */
    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public struct Enumerator
    {
        private readonly RingBuffer<T> ring;
        private int idx;

        internal Enumerator(RingBuffer<T> ring)
        {
            this.ring = ring;
            idx = -1;
        }

        public bool MoveNext()
        {
            return ++idx < ring.Count;
        }

        public ref T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref ring.buf[ring.WrapIndex(ring.head + idx)];
        }

        public void Reset()
        {
            idx = -1;
        }
    }
}