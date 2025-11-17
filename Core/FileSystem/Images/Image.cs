namespace Caligo.Core.FileSystem.Images;

public class Image
{
    public readonly string Path;
    private ImageData? _data;

    public Image(string path)
    {
        Path = path;
    }

    public ImageData Load()
    {
        if (_data is not null)
            return _data;

        if (!File.Exists(Path))
            throw new FileNotFoundException($"Image file not found: {Path}");

        _data = ImageData.Load(Path);

        return _data;
    }
}
