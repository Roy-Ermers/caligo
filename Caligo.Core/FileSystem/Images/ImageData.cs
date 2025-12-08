using StbImageSharp;
using StbImageWriteSharp;
using ColorComponents = StbImageSharp.ColorComponents;

namespace Caligo.Core.FileSystem.Images;

public class ImageData
{
    public readonly byte[] Data;
    public readonly int Height;
    public readonly int Width;

    internal ImageData(int width, int height, byte[] data)
    {
        Width = width;
        Height = height;
        Data = data;
    }

    public static ImageData Load(string path)
    {
        using var stream = File.OpenRead(path);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        StbImage.stbi_set_flip_vertically_on_load(1);

        return new ImageData(image.Width, image.Height, image.Data);
    }

    public static ImageData Load(int width, int height, byte[] data)
    {
        return new ImageData(width, height, data);
    }

    public void Dump(string path)
    {
        using Stream stream = File.OpenWrite(path);
        ImageWriter writer = new();
        writer.WritePng(Data, Width, Height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
    }
}