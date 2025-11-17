using Caligo.Core.FileSystem.Images;
using OpenTK.Graphics.OpenGL;

namespace Caligo.Client.Graphics;

public class Texture2DArray
{
    public int Handle { private set; get; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Length { get; set; }

    private ImageData[]? _images;

    public void Bind(int unit)
    {
        GL.ActiveTexture(TextureUnit.Texture0 + unit);
        GL.BindTexture(TextureTarget.Texture2DArray, Handle);
    }

    public Texture2DArray(Image[] images)
    {
        var imageData = new ImageData[images.Length];
        for (var i = 0; i < images.Length; i++)
        {
            imageData[i] = images[i].Load();
        }

        _images = imageData;

        Initialize();
    }

    public Texture2DArray(ImageData[] images)
    {
        _images = images;

        Initialize();
    }

    public void Initialize()
    {
        if (_images == null || _images.Length == 0)
        {
            throw new ArgumentException("No images provided to create Texture2DArray.");
        }

        var width = _images[0].Width;
        var height = _images[0].Height;

        var handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2DArray, handle);

        GL.TexImage3D(TextureTarget.Texture2DArray, 0, PixelInternalFormat.Rgba, width, height, _images.Length, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

        for (var i = 0; i < _images.Length; i++)
        {
            var image = _images[i];
            GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, i, width, height, 1, PixelFormat.Rgba,
                PixelType.UnsignedByte, image.Data);
        }

        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Nearest);

        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);

        Handle = handle;
        Width = width;
        Height = height;
        Length = _images.Length;

        _images = null; // Clear the images array to free memory
    }
}
