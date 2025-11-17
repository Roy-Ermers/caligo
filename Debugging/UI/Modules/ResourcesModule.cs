using System.Numerics;
using System.Text.Json;
using WorldGen.FileSystem;
using WorldGen.FileSystem.Images;
using WorldGen.FileSystem.Json;
using WorldGen.ModuleSystem.Storage;
using WorldGen.Graphics.UI.PaperComponents;
using WorldGen.Resources.Block;
using WorldGen.Graphics;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Debugging.UI.Modules;

public class ResourcesDebugModule : IDebugModule
{
    public bool Enabled { get; set; }
    public string Name => "Resources";
    public char? Icon => PaperIcon.Folder;

    private readonly Game _game;
    private string currentStorageKey;
    private (string key, Texture2D texture) _loadedTexture;

    public ResourcesDebugModule(Game game)
    {
        _game = game;
        currentStorageKey = game.ModuleRepository.Resources.Storages.Keys.FirstOrDefault() ?? "";
        _loadedTexture = ("", Texture2D.FromData(16, 16, []));
    }

    public void Render()
    {
        if (_game is null)
            return;

        var storages = _game.ModuleRepository.Resources.Storages;

        if (storages.Count == 0)
        {
            Components.Text("No storages found in this module.");
            return;
        }

        using (Components.Tabs("resources"))
        {
            foreach (var key in storages.Keys)
            {
                if (Components.Tab(key))
                {
                    currentStorageKey = key;
                }
            }
        }

        if (string.IsNullOrEmpty(currentStorageKey) || !storages.ContainsKey(currentStorageKey))
            return;

        var storage = storages[currentStorageKey];

        switch (storage)
        {
            case ResourceTypeStorage<Image> imageStorage:
                DrawImageStorage(imageStorage);
                break;
            case ResourceTypeStorage<string> configStorage when configStorage.Key == "Config":
                DrawConfigStorage(configStorage);
                break;
            case ResourceTypeStorage<Block> blockStorage:
                DrawBlockStorage(blockStorage);
                break;
            default:
                DrawUnknownStorage(storage.CastToObjectEnumerable());
                break;
        }

        Components.Text($"Total: {storage.Count}");
    }

    private static void DrawUnknownStorage(IEnumerable<KeyValuePair<string, object>> storage)
    {
        using var scrollContainer = Components.ScrollContainer().Enter();

        foreach (var (name, value) in storage)
        {
            Components.Tooltip(
            Components.ListItem(name, false),
            () =>
            {
                Components.Text($"Type: {value.GetType().Name}");
            });
        }
    }

    private static void DrawConfigStorage(ResourceTypeStorage<string> configStorage)
    {
        using var scrollContainer = Components.ScrollContainer().Enter();

        foreach (var (name, value) in configStorage)
        {
            var _value = value ?? string.Empty;

            if (_value is null)
                continue;

            Components.Text(name);
            // Note: PaperComponents might need a textbox implementation for editing
            Components.Text($"Value: {_value}");
        }
    }

    private void DrawImageStorage(ResourceTypeStorage<Image> imageStorage)
    {
        using var scrollContainer = Components.ScrollContainer().Enter();

        foreach (var (name, image) in imageStorage)
        {
            Components.Tooltip(Components.ListItem(name, false, _ => FileSystemUtils.OpenFile(image.Path)), () =>
            {
                Components.Text($"Path: {image.Path}");

                var loadedImage = image.Load();

                if (_loadedTexture.key != name)
                {
                    _loadedTexture.key = name;

                    _loadedTexture.texture.Dispose();

                    _loadedTexture.texture = Texture2D.FromData(loadedImage.Width, loadedImage.Height, loadedImage.Data);
                }
                Components.Text($"Size: {loadedImage.Width}x{loadedImage.Height}");

                Components.Texture(_loadedTexture.texture, UnitValue.Pixels(128));
            });
        }
    }

    private void DrawBlockStorage(ResourceTypeStorage<Block> blockStorage)
    {
        using var scrollContainer = Components.ScrollContainer().Enter();

        foreach (var (name, block) in blockStorage)
        {
            if (Components.Accordion($"{block.Name} (ID: {block.NumericId})"))
            {
                Components.Text($"Block ID: {block.NumericId}");
                Components.Text($"Block Name: {name}");
                Components.Text($"Variants: {block.Variants.Length}");

                foreach (var variant in block.Variants)
                {
                    var variantIndex = Array.IndexOf(block.Variants, variant) + 1;
                    if (Components.Accordion($"Variant {variantIndex}"))
                    {
                        Components.Text($"Weight: {variant.Weight}");
                        Components.Text($"Model: {variant.ModelName}");

                        if (variant.Textures.Count > 0)
                        {
                            if (Components.Accordion("Textures"))
                            {
                                foreach (var texture in variant.Textures)
                                {
                                    Components.Text($"{texture.Key}: {texture.Value}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
