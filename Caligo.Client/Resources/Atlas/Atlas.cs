using Caligo.Client.Graphics;
using Caligo.Core.ModuleSystem;
using Caligo.ModuleSystem;

namespace Caligo.Client.Resources.Atlas;

public record class Atlas
{
    public required Texture2DArray TextureArray { get; init; }
    public int TileSize { get; init; }
    public required string?[] Aliases { get; init; }

    public int Count => TextureArray.Length;

    public int this[string name] => Array.IndexOf(Aliases, Identifier.Resolve(name));

    public int this[int index]
    {
        get
        {
            if (index < 0 || index >= Aliases.Length)
            {
                throw new IndexOutOfRangeException(
                    $"Index {index} is out of range for atlas with {Aliases.Length} entries.");
            }

            return index;
        }
    }
}
