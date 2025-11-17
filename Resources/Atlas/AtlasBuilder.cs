using WorldGen.FileSystem.Images;
using WorldGen.Renderer;

namespace WorldGen.Resources.Atlas;

public class AtlasBuilder()
{
    public int? TextureSize { get; private set; }
    private readonly Dictionary<string, ImageData> _sprites = [];

    public void AddEntry(string name, Image image)
    {
        var texture = image.Load();

        TextureSize ??= Math.Max(texture.Width, texture.Height);

        if (texture.Width != TextureSize || texture.Height != TextureSize)
        {
            throw new Exception(
                $"Texture {name} ({image.Path}) is not the format specified by the module configuration ({TextureSize}x{TextureSize})");
        }

        _sprites.Add(name, texture);
    }

    private static string GetTextureName(string filePath)
    {
        var name = Path.GetFileName(filePath);
        return name.Replace(".png", "");
    }


    public Atlas Build()
    {
        if (_sprites.Count == 0)
            throw new InvalidOperationException("No sprites have been added to the atlas builder.");
        if (TextureSize is null)
            throw new InvalidOperationException("Texture size has not been set. Please add at least one sprite before building the atlas.");

        var entries = new string?[_sprites.Count];

        var index = 0;
        foreach (var name in _sprites.Select(sprite => GetTextureName(sprite.Key)))
        {
          entries[index] = name;

          index++;
        }

        var textureArray = new Texture2DArray([.. _sprites.Values]);

        var atlas = new Atlas()
        {
            TextureArray = textureArray,
            TileSize = TextureSize.Value,
            Aliases = entries
        };

        return atlas;
    }
}
