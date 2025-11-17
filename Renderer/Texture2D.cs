using OpenTK.Graphics.OpenGL4;
using WorldGen.FileSystem.Images;

namespace WorldGen.Renderer;

public class Texture2D
{
    public readonly int Handle;
    public int Width;
    public int Height;

    private Texture2D(int handle, int width, int height)
    {
        Handle = handle;
        Width = width;
        Height = height;
    }


    public static Texture2D FromFile(string path)
    {
        var image = new Image(path).Load();

        return FromData(image.Width, image.Height, image.Data);
    }

    public static Texture2D FromImage(Image image)
    {
        return FromImage(image.Load());
    }

    public static Texture2D FromImage(ImageData image)
    {
        return FromData(image.Width, image.Height, image.Data);
    }

    public static Texture2D FromData(int width, int height, ReadOnlySpan<byte> data)
    {
        var handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, handle);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, data.ToArray());

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.NearestMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        return new Texture2D(handle, width, height);
    }

    public byte[] GetData()
    {
        GL.BindTexture(TextureTarget.Texture2D, Handle);
        var data = new byte[Width * Height * 4];
        GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
        return data;
    }

    public void SetData(byte[] data) => SetData(data.AsSpan());
    public void SetData(ReadOnlySpan<byte> data) => SetData(data, Width, Height);
    public void SetData(ReadOnlySpan<byte> data, int width, int height)
    {
        if (data.Length != width * height * 4)
            throw new ArgumentException("Data length does not match the specified width and height.");

        Width = width;
        Height = height;

        GL.BindTexture(TextureTarget.Texture2D, Handle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, data.ToArray());
    }

    /// <summary>
    /// Get a part of the texture defined by area
    /// </summary>
    /// <param name="area"></param>
    public ReadOnlySpan<byte> GetData(System.Drawing.Rectangle area)
    {
        var data = GetData();
        Span<byte> result = new byte[area.Width * area.Height * 4];

        for (int y = 0; y < area.Height; y++)
        {
            for (int x = 0; x < area.Width; x++)
            {
                var index = ((y + area.Y) * Width + x + area.X) * 4;
                var dataIndex = (y * area.Width + x) * 4;
                result[dataIndex] = data[index];
                result[dataIndex + 1] = data[index + 1];
                result[dataIndex + 2] = data[index + 2];
                result[dataIndex + 3] = data[index + 3];
            }
        }

        return result;
    }

    public void DumpPng(string path)
    {
        var data = GetData();
        var image = ImageData.Load(Width, Height, data);

        image.Dump(path);
    }

    public void Dispose()
    {
        GL.DeleteTexture(Handle);
    }
}