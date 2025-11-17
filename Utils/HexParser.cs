namespace WorldGen.Utils;

public class HexParser
{
    public static bool TryParseHex(string hex, out uint value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(hex) || hex.Length < 2 || hex[0] != '#')
            return false;

        hex = hex[1..];

        try
        {
            value = uint.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryParseHex(string hex, out System.Numerics.Vector4 color)
    {
        color = System.Numerics.Vector4.Zero;

        if (!TryParseHex(hex, out uint value))
            return false;
        if (hex.Length == 3)
        {
            // Handle shorthand hex notation (e.g., #FFF)
            color.X = ((value >> 8) & 0xF) / 15f;
            color.Y = ((value >> 4) & 0xF) / 15f;
            color.Z = (value & 0xF) / 15f;
            color.W = 1f;
        }
        else if (hex.Length == 4)
        {
            // Handle shorthand hex notation with alpha (e.g., #FFFF)
            color.X = ((value >> 12) & 0xF) / 15f;
            color.Y = ((value >> 8) & 0xF) / 15f;
            color.Z = ((value >> 4) & 0xF) / 15f;
            color.W = (value & 0xF) / 15f;
        }
        else if (hex.Length == 6)
        {
            color.X = ((value >> 16) & 0xFF) / 255f;
            color.Y = ((value >> 8) & 0xFF) / 255f;
            color.Z = (value & 0xFF) / 255f;
            color.W = 1f;
        }
        else if (hex.Length == 7)
        {
            color.X = ((value >> 16) & 0xFF) / 255f;
            color.Y = ((value >> 8) & 0xFF) / 255f;
            color.Z = (value & 0xFF) / 255f;
            color.W = 1f;
        }
        else if (hex.Length == 9)
        {
            color.X = ((value >> 24) & 0xFF) / 255f;
            color.Y = ((value >> 16) & 0xFF) / 255f;
            color.Z = ((value >> 8) & 0xFF) / 255f;
            color.W = (value & 0xFF) / 255f;
        }

        return true;
    }
}
