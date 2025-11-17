using System.Numerics;
using System.Text.Json;
using ImGuiNET;
using WorldGen.FileSystem;
using WorldGen.FileSystem.Images;
using WorldGen.FileSystem.Json;
using WorldGen.ModuleSystem.Storage;
using WorldGen.Graphics.UI.ImGuiComponents;
using WorldGen.Graphics.UI.ImGuiComponents.Tables;
using WorldGen.Resources.Block;

namespace WorldGen.Graphics.UI.Windows;

public class ResourcesWindow : Window
{
    public override bool Enabled { get; set; } = true;
    public override string Name => "Resources";

    Game _game = null!;
    Texture2D? imagePreview;

    string currentStorageKey;

    public override void Initialize(Game game)
    {
        _game = game;
        currentStorageKey = game.ModuleRepository.Resources.Storages.Keys.First();
    }


    public override void Draw(double deltaTime)
    {
        if (_game is null)
            return;

        var storages = _game.ModuleRepository.Resources.Storages;

        if (storages.Count == 0)
        {
            ImGui.TextColored(Vector4.UnitX + Vector4.UnitW, "No storages found in this module.");
            return;
        }
        ImGui.Text("ResourceType");
        if (ImGui.BeginCombo("##ResourceTypes", currentStorageKey))
        {
            foreach (var key in storages.Keys)
            {
                if (ImGui.Selectable(key, currentStorageKey == key))
                    currentStorageKey = key;
            }
            ImGui.EndCombo();
        }

        ImGui.Separator();

        var storage = storages[currentStorageKey];
        switch (storage)
        {
            case ResourceTypeStorage<Image> ImageStorage:
                DrawImageStorage(ImageStorage);
                break;
            case ResourceTypeStorage<string> configStorage and { Key: "Config" }:
                DrawConfigStorage(configStorage);
                break;
            case ResourceTypeStorage<Block> blockStorage:
                DrawBlockStorage(blockStorage);
                break;
            default:
                DrawUnknownStorage(storage.CastToObjectEnumerable());
                break;
        }
        ImGui.TextDisabled($"Total: {storage.Count}");
    }

    private static void DrawUnknownStorage(IEnumerable<KeyValuePair<string, object>> storage)
    {
        ImGui.BeginChild("UnknownStorage", new Vector2(0, 0), ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.FrameStyle);

        foreach (var (name, value) in storage)
        {
            if (ImGui.BeginPopupContextWindow(name))
            {
                ImGui.SetWindowSize(new Vector2(400, 300));
                ImGui.Text(JsonSerializer.Serialize(value, JsonOptions.SerializerOptions));
                ImGui.EndPopup();
            }

            if (ImGui.Selectable(name))
                ImGui.OpenPopup(name);

            if (ImGui.BeginItemTooltip())
            {
                ImGui.BeginTable("info", 2);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextDisabled("Identifier");
                ImGui.TableNextColumn();
                ImGui.Text(name);

                ImGui.TableNextColumn();
                ImGui.TextDisabled("Type");
                ImGui.TableNextColumn();
                ImGui.Text(value.GetType().Name);
                ImGui.EndTable();
                ImGui.EndTooltip();
            }
        }

        ImGui.EndChild();
    }

    private static void DrawConfigStorage(ResourceTypeStorage<string> configStorage)
    {
        ImGui.BeginGroup();

        foreach (var (name, value) in configStorage)
        {
            var _value = value ?? string.Empty;

            if (_value is null)
                continue;

            ImGui.Text(name);
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetWindowSize().X / 2 - 16);
            ImGui.SetNextItemWidth(ImGui.GetWindowSize().X / 2);
            ImGui.InputText($"##{name}", ref _value, 256);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                configStorage[name] = _value;
                _value = value;
            }
        }

        ImGui.EndGroup();
    }

    private void DrawImageStorage(ResourceTypeStorage<Image> imageStorage)
    {
        ImGui.BeginChild("images", new Vector2(0, 0), ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.FrameStyle);

        foreach (var (name, image) in imageStorage)
        {
            ImGui.OpenPopupOnItemClick(name, ImGuiPopupFlags.MouseButtonRight);
            if (ImGui.BeginPopupContextItem(name))
            {
                if (ImGui.Selectable("Open File"))
                {
                    FileSystemUtils.OpenFile(image.Path);
                }
                ImGui.EndPopup();
            }

            ImGui.Selectable(name, false);

            if (ImGui.BeginItemTooltip())
            {
                var loadedImage = image.Load();
                if (imagePreview is null)
                    imagePreview = Texture2D.FromImage(loadedImage);
                else
                    imagePreview.SetData(loadedImage.Data, loadedImage.Width, loadedImage.Height);
                var availableWidth = 256;

                ImGui.Image(imagePreview.Handle,
                    new Vector2(availableWidth, availableWidth * loadedImage.Height / (float)loadedImage.Width),
                    new(0, 1),
                    new(1, 0)
                );

                using var TextureInfo = new InfoTableComponent("Text info");
                TextureInfo.Add("ID", name);
                TextureInfo.Add("Size", $"{loadedImage.Width}x{loadedImage.Height}");
                TextureInfo.Add("Path", image.Path);
                TextureInfo.Dispose();
                ImGui.EndTooltip();
            }
        }

        ImGui.EndChild();
    }

    private void DrawBlockStorage(ResourceTypeStorage<Block> blockStorage)
    {
        using var widget = new FrameComponent("Blocks").Enter();

        foreach (var (name, block) in blockStorage)
        {
            if (ImGui.Selectable(block.Name))
                ImGui.OpenPopup(block.Name);

            if (ImGui.BeginPopup(block.Name))
            {
                ImGui.Text("Block info");
                var info = new InfoTableComponent("blockTextures");
                info.Add("Block ID", block.NumericId);
                info.Add("Block Name", name);
                info.Dispose();

                foreach (var variant in block.Variants)
                {
                    if (ImGui.CollapsingHeader($"Variant {Array.IndexOf(block.Variants, variant) + 1}"))
                    {
                        ImGui.Text("Info:");
                        using var VariantInfo = new InfoTableComponent("variant");

                        VariantInfo.Add("Weight", variant.Weight);
                        VariantInfo.Add("Model", variant.ModelName);
                        VariantInfo.Dispose();

                        if (variant.Textures.Count > 0)
                        {
                            ImGui.Text("Textures:");
                            using var blockTexture = new InfoTableComponent("blockTextures");
                            blockTexture.Set(variant.Textures);
                        }
                    }
                }

                ImGui.EndPopup();
            }
            if (ImGui.BeginItemTooltip())
            {
                var info = new InfoTableComponent("blockTextures");
                info.Add("Block ID", block.NumericId);
                info.Add("Block Name", name);
                info.Add("Variants", block.Variants.Length);

                info.Dispose();
                ImGui.EndTooltip();
            }
            ImGui.SameLine();
            ImGui.TextDisabled(block.NumericId.ToString());
        }
    }
}
