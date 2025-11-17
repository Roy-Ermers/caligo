using System.Diagnostics;
using System.Numerics;
using System.Text.Json;
using ImGuiNET;
using OpenTK.Graphics.ES20;
using OpenTK.Input.Hid;
using WorldGen.FileSystem;
using WorldGen.FileSystem.Images;
using WorldGen.FileSystem.Json;
using WorldGen.ModuleSystem;
using WorldGen.ModuleSystem.Storage;
using WorldGen.Resources.Block;

namespace WorldGen.Renderer.UI.Windows;

public class ModuleWindow : Window
{
    public override bool Enabled { get; set; } = true;
    Game _game = null!;
    Texture2D? imagePreview;

    public override void Initialize(Game game)
    {
        _game = game;
    }
    public override void Draw(double deltaTime)
    {
        if (_game is null)
            return;

        var storages = _game.ModuleRepository.Resources.Storages;

        ImGui.SeparatorText("Modules");

        ImGui.BeginChild("ModulesList", new Vector2(0, 0), ImGuiChildFlags.AutoResizeY);
        foreach (var module in _game.ModuleRepository.Modules)
        {
            if (ImGui.TreeNode(module.Identifier))
            {
                ImGui.BeginChild(module.Identifier + "Info", new Vector2(0, 0), ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY);
                ImGui.Text($"Identifier: {module.Identifier}");
                ImGui.Text($"Directory: {module.AbsoluteDirectory}");
                ImGui.Text($"Storage Count: {module.Storages.Count}");
                ImGui.Text($"Resources Count: {module.Storages.Sum(s => s.Value.Count)}");

                if (ImGui.SmallButton("Open Directory"))
                    FileSystemUtils.OpenDirectory(module.AbsoluteDirectory);

                ImGui.EndChild();
                ImGui.TreePop();
            }
        }
        ImGui.EndChild();

        ImGui.SeparatorText("Content");

        if (storages.Count == 0)
        {
            ImGui.Separator();
            ImGui.TextColored(Vector4.UnitX + Vector4.UnitW, "No storages found in this module.");
            return;
        }

        ImGui.BeginTabBar("ModuleTabs");
        foreach (var (key, storage) in storages)
        {
            if (ImGui.BeginTabItem(key))
            {
                ImGui.TextDisabled($"Total: {storage.Count}");

                switch (storage)
                {
                    case ResourceTypeStorage<Image> ImageStorage:
                        DrawImageStorage(ImageStorage);
                        break;
                    case ResourceTypeStorage<string> configStorage and { Key: "config" }:
                        DrawConfigStorage(configStorage);
                        break;
                    // case ResourceTypeStorage<Block> blockStorage:
                    //     DrawBlockStorage(blockStorage);
                    //     break;
                    default:
                        DrawUnknownStorage(storage.CastToObjectEnumerable());
                        break;
                }

                ImGui.EndTabItem();
            }
        }
        ImGui.EndTabBar();
    }

    private void DrawUnknownStorage(IEnumerable<KeyValuePair<string, object>> storage)
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
                ImGui.Text($"Identifier: {name}");
                ImGui.Text($"Type: {value.GetType().Name}");
                ImGui.EndTooltip();
            }
        }

        ImGui.EndChild();
    }

    private void DrawConfigStorage(ResourceTypeStorage<string> configStorage)
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
        ImGui.BeginGroup();

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

            ImGui.Selectable(name, false, ImGuiSelectableFlags.AllowDoubleClick);

            if (ImGui.BeginItemTooltip())
            {
                var loadedImage = image.Load();
                if (imagePreview is null)
                    imagePreview = Texture2D.FromImage(loadedImage);
                else
                    imagePreview.SetData(loadedImage.Data, loadedImage.Width, loadedImage.Height);
                var availableWidth = ImGui.GetContentRegionAvail().X * 0.66f;
                ImGui.Image(imagePreview.Handle,
                    new Vector2(availableWidth, availableWidth * loadedImage.Height / (float)loadedImage.Width),
                    new(0, 1),
                    new(1, 0)
                );

                ImGui.BeginTable("TextureInfo", 2, ImGuiTableFlags.BordersInnerH);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("ID");
                ImGui.TableNextColumn();
                ImGui.Text(name);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Size");
                ImGui.TableNextColumn();
                ImGui.Text($"{loadedImage.Width}x{loadedImage.Height}");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Path");
                ImGui.TableNextColumn();
                ImGui.Text(image.Path);
                ImGui.EndTable();
                ImGui.EndTooltip();
            }
        }

        ImGui.EndGroup();
    }

    private void DrawBlockStorage(ResourceTypeStorage<Block> blockStorage)
    {
        ImGui.BeginTable("Blocks", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg);
        ImGui.TableSetupColumn("Id");
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Model", ImGuiTableColumnFlags.WidthStretch);

        ImGui.TableHeadersRow();

        foreach (var (name, block) in blockStorage)
        {
            if (ImGui.BeginPopup(name))
            {
                if (ImGui.Selectable("Open Block definition"))
                {
                    var moduleName = Identifier.ResolveModule(name);
                    var module = _game.ModuleRepository.GetModule(moduleName);
                    FileSystemUtils.OpenFile($"{module.AbsoluteDirectory}/blocks/{name}.json");
                }

                ImGui.EndPopup();
            }

            ImGui.TableNextRow(ImGuiTableRowFlags.None, 32);
            ImGui.TableNextColumn();
            ImGui.Selectable(block.NumericId.ToString(), false, ImGuiSelectableFlags.SpanAllColumns);
            ImGui.OpenPopupOnItemClick(name);


            if (ImGui.BeginItemTooltip())
            {
                ImGui.Text($"Block ID: {block.NumericId}");
                ImGui.Text($"Block Name: {name}");
                ImGui.Text($"Model Name: {block.ModelName ?? "No model"}");
                ImGui.Text($"Textures: {string.Join(", ", block.Textures)}");
                ImGui.Separator();

                ImGui.EndTooltip();
            }

            ImGui.TableNextColumn();
            ImGui.Text(name);
            ImGui.TableNextColumn();


            if (block.ModelName is not null)
                ImGui.Text(block.ModelName);
            else
                ImGui.Text("No model");
        }

        ImGui.EndTable();
    }
}
