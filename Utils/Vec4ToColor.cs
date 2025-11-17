using System.Drawing;
using System.Numerics;

namespace WorldGen.Utils;

public static class Vec4ToColor
{
    public static Color ToColor(Vector4 vec)
    {
        return Color.FromArgb(
                (int)(vec.W * 255),
                (int)(vec.X * 255),
                (int)(vec.Y * 255),
                (int)(vec.Z * 255)
        );
    }

    public static Vector4 ToVector4(this Color color)
    {
        return new Vector4(
                        color.R,
                        color.G,
                        color.B,
                        color.A
        );
    }
}
