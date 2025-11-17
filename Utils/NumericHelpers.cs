using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

namespace WorldGen.Utils;

public class MathExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Mod(int a, int b)
    {
        int r = a % b;
        return r < 0 ? r + b : r;
    }
}
