using WorldGen.FileSystem.Images;
using WorldGen.Renderer;

namespace WorldGen.Resources.Atlas;

public class AtlasBuilder(int textureSize = 16)
{
    private readonly Dictionary<string, ImageData> _sprites = [];

    public void AddEntry(string name, Image image)
    {
        var texture = image.Load();

        if (texture.Width != textureSize || texture.Height != textureSize)
        {
            throw new Exception(
                $"Texture {name} ({image.Path}) is not the format specified by the module configuration ({textureSize}x{textureSize})");
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
            TileSize = textureSize,
            Aliases = entries
        };

        return atlas;
    }
}
