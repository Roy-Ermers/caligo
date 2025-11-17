namespace WorldGen.Utils;

public class ZOrderCurve
{
    /// <summary>
    /// Interleaves the bits of three coordinates to calculate a single index
    /// </summary>
    /// <param name="x">ranges between 0-16</param>
    /// <param name="y">ranges between 0-16</param>
    /// <param name="z">ranges between 0-16</param>
    /// <returns></returns>
    public static int GetIndex(int x, int y, int z, int Size = 16)
    {
        if (x < 0 || x > Size)
            throw new ArgumentOutOfRangeException(nameof(x));
        if (y < 0 || y > Size)
            throw new ArgumentOutOfRangeException(nameof(y));
        if (z < 0 || z > Size)
            throw new ArgumentOutOfRangeException(nameof(z));

        // use morton order to interleave x,y and z components
        int result = (int)Math.Pow(Size, 2) * z + Size * x + y;
        return result;
    }

    public static (int x, int y, int z) GetPosition(int index, int Size = 16)
    {
        if (index < 0 || index >= Math.Pow(Size, 3))
            throw new ArgumentOutOfRangeException(nameof(index));

        // use morton order to deinterleave x,y and z components
        int z = index / (Size * Size);
        int y = (index / Size) % Size;
        int x = index % Size;

        return (x, y, z);
    }
}
