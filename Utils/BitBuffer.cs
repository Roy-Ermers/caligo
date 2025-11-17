namespace WorldGen.Utils;

public class BitBuffer(int initialSize)
{
    private byte[] _buffer = new byte[(initialSize / 8 + 1)];
    private int _length = initialSize;
    public int Length => _length;

    public bool this[int index]
    {
        get => Get(index);
        set => Set(index, value);
    }

    public static BitBuffer Create(int size)
    {
        if (size < 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size must be non-negative.");
        return new BitBuffer(size);
    }

    public BitBuffer Slice(int start, int length)
    {
        if (start < 0 || start + length > _length)
            throw new ArgumentOutOfRangeException($"{nameof(start)} is out of range.");

        var sliceBuffer = new BitBuffer(length);
        for (int i = 0; i < length; i++)
        {
            sliceBuffer.Set(i, Get(start + i));
        }
        return sliceBuffer;
    }

    public void Set(int index, bool value)
    {
        if (index < 0 || index >= _length)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

        int byteIndex = index / 8;
        int bitIndex = index % 8;

        if (value)
            _buffer[byteIndex] |= (byte)(1 << bitIndex);
        else
            _buffer[byteIndex] &= (byte)~(1 << bitIndex);
    }

    public bool Get(int index)
    {
        if (index < 0 || index >= _length)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

        int byteIndex = index / 8;
        int bitIndex = index % 8;

        return (_buffer[byteIndex] & (1 << bitIndex)) != 0;
    }

    /// <summary>
    /// Set a byte in the buffer at the specified index.
    /// Doesn't care about the internal byte alignment, just sets the byte at the index.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="value"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public void Set(int index, byte value)
    {
        if (index < 0 || index >= _length)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

        int byteIndex = index / 8;
        int bitIndex = index % 8;

        // Ensure that the byte is set correctly in the buffer
        if (bitIndex != 0)
        {
            // If the bit index is not zero, we need to clear the bits before the byte
            _buffer[byteIndex] &= value;
            _buffer[byteIndex + 1] = (byte)(value >> (8 - bitIndex));
        }
        else
        {
            // If the bit index is zero, we can just set the byte directly
            _buffer[byteIndex] = value;
        }
    }

    public void SetByte(int index, byte value)
    {
        if (index < 0 || index >= _length - 8)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

        int byteIndex = index / 8;
        int bitIndex = index % 8;

        _buffer[byteIndex] = (byte)((value >> (8 - bitIndex)) & 0xFF);

        if (bitIndex != 0)
        {
            var oldValue = _buffer[byteIndex + 1];
            // keep old bits after the byte
            _buffer[byteIndex + 1] = (byte)((oldValue & ((1 << bitIndex) - 1)) | (value << (8 - bitIndex)));
        }
    }

    public byte GetByte(int index)
    {
        if (index < 0 || index >= _length - 8)
            throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range.");

        int byteIndex = index / 8;
        int bitIndex = index % 8;

        return (byte)((_buffer[byteIndex] << bitIndex) | (_buffer[byteIndex + 1] >> (8 - bitIndex)));
    }

    public void Clear()
    {
        Array.Clear(_buffer, 0, _buffer.Length);
    }

    public byte[] ToArray()
    {
        var result = new byte[_buffer.Length];
        Array.Copy(_buffer, result, _buffer.Length);
        return result;
    }

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _length; i++)
        {
            sb.Append(Get(i) ? '1' : '0');
        }
        return sb.ToString();
    }

    public void Resize(int newSize)
    {
        if (newSize < 0)
            throw new ArgumentOutOfRangeException(nameof(newSize), "New size must be non-negative.");

        var newBuffer = new byte[(newSize / 8 + 1)];
        Array.Copy(_buffer, newBuffer, Math.Min(_buffer.Length, newBuffer.Length));
        _buffer = newBuffer;
        _length = newSize;
    }
}
