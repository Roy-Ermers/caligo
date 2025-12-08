using System.Numerics;

namespace Caligo.Client.Renderer.Worlds.Materials;

public record struct Material : IEquatable<Material>
{
    /// <summary>
    ///     The height of the face.
    /// </summary>
    /// <remarks>
    ///     ranges from 0 to 15
    /// </remarks>
    public ushort Height;

    /// <summary>
    ///     Whether the face should be shaded based on its normal.
    /// </summary>
    public bool Shade;

    /// <summary>
    ///     The texture ID of the face.
    ///     <remarks>
    ///         The texture ID is an index into a texture atlas, which contains the textures of the face.
    ///         Max value is 65535 (0xFFFF).
    ///     </remarks>
    /// </summary>
    public int TextureId;

    /// <summary>
    ///     The tint color of the face.
    /// </summary>
    /// <remarks>
    ///     The tint color is a vector with 4 components:
    ///     - X: Red component (0-15)
    ///     - Y: Green component (0-15)
    ///     - Z: Blue component (0-15)
    ///     the values are normalized to 0-1 range on the GPU.
    /// </remarks>
    public Vector3? Tint;

    /// <summary>
    ///     Where is the top-left corner of the texture in the texture atlas?
    /// </summary>
    /// <remarks>
    ///     the UV coordinates are ranged between 0 to 31, but you should use them as 0 - 16.
    /// </remarks>
    public Vector2 UV0;

    /// <summary>
    ///     Where is the bottom-right corner of the texture in the texture atlas?
    /// </summary>
    /// <remarks>
    ///     the UV coordinates are ranged between 0 to 31, but you should use them as 0 - 16.
    /// </remarks>
    public Vector2 UV1;

    /// <summary>
    ///     The width of the face.
    /// </summary>
    /// <remarks>
    ///     ranges from 0 to 15
    /// </remarks>
    public ushort Width;


    public readonly bool Equals(Material other)
    {
        var thisEncoded = Encode();
        var otherEncoded = other.Encode();
        return thisEncoded[0b0] == otherEncoded[0b0] && thisEncoded[0b1] == otherEncoded[0b1];
    }

    private readonly Vector3 EncodeTint()
    {
        if (Tint is not null)
            return new Vector3(
                MathF.Floor(Tint.Value.X / 15.0f),
                MathF.Floor(Tint.Value.Y / 15.0f),
                MathF.Floor(Tint.Value.Z / 15.0f)
            );

        return new Vector3(0b1111, 0b1111, 0b1111);
    }

    private static Vector2 EncodeUV(Vector2 uv)
    {
        return uv;
    }


    public readonly int[] Encode()
    {
        var upper = 0b0;

        var width = (Width - 0b1) & 0b1111;
        var height = (Height - 0b1) & 0b1111;

        var uv0 = EncodeUV(UV0);
        var uv1 = EncodeUV(UV1);
        var uv0u = (int)uv0.X & 0b11111;
        var uv0v = (int)uv0.Y & 0b11111;
        var uv1u = (int)uv1.X & 0b11111;
        var uv1v = (int)uv1.Y & 0b11111;

        upper |= width << 0b0;
        upper |= height << 0b100;
        upper |= uv0u << 0b1000;
        upper |= uv0v << 0b1101;
        upper |= uv1u << 0b10010;
        upper |= uv1v << 0b10111;

        var lower = 0b0;
        var Tint = EncodeTint();
        var tintR = (int)Tint.X & 0b1111;
        var tintG = (int)Tint.Y & 0b1111;
        var tintB = (int)Tint.Z & 0b1111;
        var textureId = TextureId & 0b1111111111111111;
        lower |= textureId << 0b0;
        lower |= tintR << 0b10000;
        lower |= tintG << 0b10100;
        lower |= tintB << 0b11000;
        lower |= (Shade ? 1 : 0) << 28;

        return [upper, lower];
    }

    public readonly override int GetHashCode()
    {
        var encoded = Encode();
        return HashCode.Combine(encoded[0b0], encoded[0b1]);
    }
}