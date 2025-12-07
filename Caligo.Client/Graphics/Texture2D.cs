using System.Drawing;
using Caligo.Core.FileSystem.Images;
using OpenTK.Graphics.OpenGL;

namespace Caligo.Client.Graphics;

public class Texture2D
{
    public readonly int Handle;
    public int Width;
    public int Height;

    private TextureMinFilter _minFilter = TextureMinFilter.NearestMipmapLinear;

    public TextureMinFilter MinFilter
    {
        get => _minFilter;
        set
        {
            _minFilter = value;
            GL.BindTexture(TextureTarget.Texture2D, Handle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)value);
        }
    }

    private TextureMagFilter _magFilter = TextureMagFilter.Nearest;

    public TextureMagFilter MagFilter
    {
        get => _magFilter;
        set
        {
            _magFilter = value;
            GL.BindTexture(TextureTarget.Texture2D, Handle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)value);
        }
    }

    private TextureWrapMode _wrapS = TextureWrapMode.Repeat;
    public TextureWrapMode WrapS
    {
        get => _wrapS;
        set
        {
            _wrapS = value;
            GL.BindTexture(TextureTarget.Texture2D, Handle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)value);
        }
    }

    private TextureWrapMode _wrapT = TextureWrapMode.Repeat;
    public TextureWrapMode WrapT
    {
        get => _wrapT;
        set
        {
            _wrapT = value;
            GL.BindTexture(TextureTarget.Texture2D, Handle);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)value);
        }
    }

    private Texture2D(int handle, int width, int height, bool generateMipmaps = true)
    {
        Handle = handle;
        Width = width;
        Height = height;

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)_minFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)_magFilter);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)_wrapS);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)_wrapT);

        if (generateMipmaps)
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    }

    public Texture2D(int width, int height, PixelInternalFormat internalFormat, PixelFormat format, PixelType type)
    {
        Handle = GL.GenTexture();
        Width = width;
        Height = height;

        GL.BindTexture(TextureTarget.Texture2D, Handle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, internalFormat, width, height, 0, format, type, IntPtr.Zero);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)_minFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)_magFilter);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)_wrapS);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)_wrapT);
    }

    public void SetFiltering(TextureMinFilter minFilter, TextureMagFilter magFilter)
    {
        _minFilter = minFilter;
        _magFilter = magFilter;
        GL.BindTexture(TextureTarget.Texture2D, Handle);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
    }

    public void SetWrapMode(TextureWrapMode wrapS, TextureWrapMode wrapT)
    {
        _wrapS = wrapS;
        _wrapT = wrapT;
        GL.BindTexture(TextureTarget.Texture2D, Handle);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)wrapS);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)wrapT);
    }

    public void Resize(int width, int height)
    {
        if (Width == width && Height == height)
            return;

        Width = width;
        Height = height;

        GL.BindTexture(TextureTarget.Texture2D, Handle);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f, width, height, 0, 
            PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
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

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, data.ToArray());

        return new Texture2D(handle, width, height);
    }

    public byte[] GetData()
    {
        GL.BindTexture(TextureTarget.Texture2D, Handle);
        var data = new byte[Width * Height * 4];
        GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
        return data;
    }

    public void SetRectangle(Rectangle rectangle, ReadOnlySpan<byte> data)
    {
        if (data.Length != rectangle.Width * rectangle.Height * 4)
            throw new ArgumentException("Data length does not match the specified rectangle size.");

        GL.BindTexture(TextureTarget.Texture2D, Handle);
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height,
                PixelFormat.Rgba, PixelType.UnsignedByte, data.ToArray());
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

    public void Bind(int unit)
    {
        GL.ActiveTexture(TextureUnit.Texture0 + unit);
        GL.BindTexture(TextureTarget.Texture2D, Handle);
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
