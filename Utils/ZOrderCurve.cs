namespace WorldGen.Utils;

public class ZOrderCurve
{
    /// <summary>
    /// Interleaves the bits of three coordinates to calculate a single index
    /// </summary>
    /// <param name="x">ranges between 0-16</param>
    /// <param name="y">ranges between 0-16</param>
    /// <param name="z">ranges between 0-16</param>
    /// <param name="size">The size to loop through</param>
    /// <returns></returns>
    public static int GetIndex(int x, int y, int z, int size = 16)
    {
        if (x < 0 || x > size)
            throw new ArgumentOutOfRangeException(nameof(x));
        if (y < 0 || y > size)
            throw new ArgumentOutOfRangeException(nameof(y));
        if (z < 0 || z > size)
            throw new ArgumentOutOfRangeException(nameof(z));

        // use morton order to interleave x,y and z components
        var result = (int)Math.Pow(size, 2) * z + size * x + y;
        return result;
    }

    public static (int x, int y, int z) GetPosition(int index, int size = 16)
    {
        if (index < 0 || index >= Math.Pow(size, 3))
            throw new ArgumentOutOfRangeException(nameof(index));

        // use morton order to deinterleave x,y and z components
        var z = index / (size * size);
        var y = (index / size) % size;
        var x = index % size;

        return (x, y, z);
    }
}
