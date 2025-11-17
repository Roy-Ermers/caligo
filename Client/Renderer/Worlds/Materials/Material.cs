using System.Numerics;

namespace Caligo.Client.Renderer.Worlds.Materials;

public record struct Material : IEquatable<Material>
{
    /// <summary>
    /// The width of the face.
    /// </summary>
    /// <remarks>
    /// ranges from 0 to 15
    /// </remarks>
    public ushort Width;
    /// <summary>
    /// The height of the face.
    /// </summary>
    /// <remarks>
    /// ranges from 0 to 15
    /// </remarks>
    public ushort Height;

    /// <summary>
    /// Where is the top-left corner of the texture in the texture atlas?
    /// </summary>
    /// <remarks>
    /// the UV coordinates are ranged between 0 to 31, but you should use them as 0 - 16.
    /// </remarks>
    public Vector2 UV0;

    /// <summary>
    /// Where is the bottom-right corner of the texture in the texture atlas?
    /// </summary>
    /// <remarks>
    /// the UV coordinates are ranged between 0 to 31, but you should use them as 0 - 16.
    /// </remarks>
    public Vector2 UV1;

    /// <summary>
    /// The tint color of the face.
    /// </summary>
    /// <remarks>
    /// The tint color is a vector with 4 components:
    /// - X: Red component (0-15)
    /// - Y: Green component (0-15)
    /// - Z: Blue component (0-15)
    /// the values are normalized to 0-1 range on the GPU.
    /// </remarks>
    public Vector3? Tint;

    /// <summary>
    /// The texture ID of the face.
    /// <remarks>
    /// The texture ID is an index into a texture atlas, which contains the textures of the face.
    /// Max value is 65535 (0xFFFF).
    /// </remarks>
    /// </summary>
    public int TextureId;


    public readonly bool Equals(Material other)
    {
        var thisEncoded = Encode();
        var otherEncoded = other.Encode();
        return thisEncoded[0] == otherEncoded[0] && thisEncoded[1] == otherEncoded[1];
    }

    private readonly Vector3 EncodeTint()
    {
        if (Tint is not null)
        {
            return new Vector3(
                MathF.Floor(Tint.Value.X / 15.0f),
                MathF.Floor(Tint.Value.Y / 15.0f),
                MathF.Floor(Tint.Value.Z / 15.0f)
            );
        }

        return new Vector3(15, 15, 15);
    }

    private static Vector2 EncodeUV(Vector2 uv)
    {
        return uv;
    }


    public readonly int[] Encode()
    {
        int upper = 0;

        int width = (Width - 1) & 0x0F;
        int height = (Height - 1) & 0x0F;

        Vector2 uv0 = EncodeUV(UV0);
        Vector2 uv1 = EncodeUV(UV1);
        int uv0u = (int)uv0.X & 0x1F;
        int uv0v = (int)uv0.Y & 0x1F;
        int uv1u = (int)uv1.X & 0x1F;
        int uv1v = (int)uv1.Y & 0x1F;

        upper |= width << 0;
        upper |= height << 4;
        upper |= uv0u << 8;
        upper |= uv0v << 13;
        upper |= uv1u << 18;
        upper |= uv1v << 23;

        int lower = 0;
        Vector3 Tint = EncodeTint();
        int tintR = (int)Tint.X & 0x0F;
        int tintG = (int)Tint.Y & 0x0F;
        int tintB = (int)Tint.Z & 0x0F;
        int textureId = TextureId & 0xFFFF;
        lower |= textureId << 0;
        lower |= tintR << 16;
        lower |= tintG << 20;
        lower |= tintB << 24;

        return [upper, lower];
    }

    public readonly override int GetHashCode()
    {
        var encoded = Encode();
        return HashCode.Combine(encoded[0], encoded[1]);
    }
}
