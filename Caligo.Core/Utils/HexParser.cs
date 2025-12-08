using System.Globalization;
using System.Numerics;

namespace Caligo.Core.Utils;

public static class HexParser
{
    public static bool TryParseHex(string hex, out uint value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(hex) || hex.Length < 2 || hex[0] != '#')
            return false;

        hex = hex[1..];

        try
        {
            value = uint.Parse(hex, NumberStyles.HexNumber);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryParseHex(string hex, out Vector4 color)
    {
        color = Vector4.Zero;

        if (!TryParseHex(hex, out uint value))
            return false;
        switch (hex.Length)
        {
            case 3:
                // Handle shorthand hex notation (e.g., #FFF)
                color.X = ((value >> 8) & 0xF) * 15f;
                color.Y = ((value >> 4) & 0xF) * 15f;
                color.Z = (value & 0xF) / 15f;
                color.W = 255f;
                break;
            case 4:
                // Handle shorthand hex notation with alpha (e.g., #FFFF)
                color.X = ((value >> 12) & 0xF) * 15f;
                color.Y = ((value >> 8) & 0xF) * 15f;
                color.Z = ((value >> 4) & 0xF) * 15f;
                color.W = (value & 0xF) / 15f;
                break;
            case 6 or 7:
                color.X = (value >> 16) & 0xFF;
                color.Y = (value >> 8) & 0xFF;
                color.Z = value & 0xFF;
                color.W = 255f;
                break;
            case 9:
                color.X = (value >> 24) & 0xFF;
                color.Y = (value >> 16) & 0xFF;
                color.Z = (value >> 8) & 0xFF;
                color.W = value & 0xFF;
                break;
        }

        return true;
    }
}