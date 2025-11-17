using System.Runtime.CompilerServices;

namespace Caligo.Core.Utils;

public static class MathExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Mod(int a, int b)
    {
        var r = a % b;
        return (r + b) % b;
    }
}
