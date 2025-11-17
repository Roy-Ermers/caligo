using Caligo.Client.Graphics.UI.ImGuiComponents;
using Caligo.Client.Graphics.UI.ImGuiComponents.Tables;
using Caligo.Core.FileSystem;
using Caligo.Core.ModuleSystem;
using Caligo.ModuleSystem;
using ImGuiNET;

namespace Caligo.Client.Graphics.UI.Windows;

public class ModuleWindow : Window
{
    public override bool Enabled { get; set; } = true;
    public override string Name => "Modules";

    Module? currentModule = null;

    Game _game = null!;

    public override void Initialize(Game game)
    {
        _game = game;
    }

    public override void Draw(double deltaTime)
    {
        if (_game is null)
            return;

        if (currentModule is not null)
        {
            if (ImGui.SmallButton("← Back"))
            {
                currentModule = null;
                return;
            }

            ImGui.SameLine();
            ImGui.Text(currentModule.Identifier);
            using var frame = new FrameComponent($"{currentModule.Identifier}-frame").Enter();
            using var table = new InfoTableComponent($"{currentModule.Identifier}-info");
            table.Add("Identifier", currentModule.Identifier);
            table.Add(
                "Directory",
                new LinkComponent(currentModule.AbsoluteDirectory)
                .OnClick(() => FileSystemUtils.OpenDirectory(currentModule.AbsoluteDirectory))
            );
            table.Add("Resources", string.Join('\n', currentModule.Storages.Select(x => x.Key)));
            table.Add("Total resources", currentModule.Storages.Sum(s => s.Value.Count));
        }
        else
        {
            foreach (var module in _game.ModuleRepository.Modules)
            {
                var identifier = module.Identifier;

                if (identifier == Identifier.MainModule)
                    identifier += " (Built-in)";

                if (ImGui.Selectable(identifier, currentModule == module))
                {
                    currentModule = module;
                }
            }
        }
    }
}
