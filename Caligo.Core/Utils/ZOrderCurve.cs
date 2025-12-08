namespace Caligo.Core.Utils;

public class ZOrderCurve
{
    /// <summary>
    ///     Interleaves the bits of three coordinates to calculate a single index
    /// </summary>
    /// <param name="x">ranges between 0-16</param>
    /// <param name="y">ranges between 0-16</param>
    /// <param name="z">ranges between 0-16</param>
    /// <param name="size">The size to loop through</param>
    /// <returns></returns>
    public static int GetIndex(int x, int y, int z, int size = 16)
    {
        // if (x < 0 || x > size)
        //     throw new ArgumentOutOfRangeException(nameof(x));
        // if (y < 0 || y > size)
        //     throw new ArgumentOutOfRangeException(nameof(y));
        // if (z < 0 || z > size)
        //     throw new ArgumentOutOfRangeException(nameof(z));

        // use morton order to interleave x,y and z components
        var result = (int)Math.Pow(size, 2) * z + size * x + y;
        return result;
    }

    public static int GetIndex(int x, int y, int z, int _, int height = 16, int depth = 16)
    {
        // if (x < 0 || x >= width)
        //     throw new ArgumentOutOfRangeException(nameof(x));
        // if (y < 0 || y >= height)
        //     throw new ArgumentOutOfRangeException(nameof(y));
        // if (z < 0 || z >= depth)
        //     throw new ArgumentOutOfRangeException(nameof(z));

        // use morton order to interleave x,y and z components
        var result = depth * height * z + height * x + y;

        return result;
    }

    public static (int x, int y, int z) GetPosition(int index, int size = 16)
    {
        return GetPosition(index, size, size, size);
    }

    public static (int x, int y, int z) GetPosition(int index, int width = 16, int height = 16, int depth = 16)
    {
        var totalSize = width * height * depth;
        if (index < 0 || index >= totalSize)
            throw new ArgumentOutOfRangeException(nameof(index));

        // use morton order to deinterleave x,y and z components
        var z = index / (width * height);
        var y = index / width % height;
        var x = index % width;

        return (x, y, z);
    }
}