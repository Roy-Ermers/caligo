using Caligo.Client.Graphics.UI.PaperComponents;
using Caligo.Core.FileSystem;
using Caligo.Core.ModuleSystem;

namespace Caligo.Client.Debugging.UI.Modules;

public class ModuleDebugModule : IDebugModule
{
    public bool Enabled { get; set; }
    public string Name => "Modules";
    public char? Icon => PaperIcon.Extension;

    private Module? currentModule = null;
    private readonly Game _game;

    public ModuleDebugModule(Game game)
    {
        _game = game;
    }

    public void Render()
    {
        if (_game is null)
            return;

        if (currentModule is not null)
        {
            Components.Button("← Back", () =>
            currentModule = null
            );

            if (currentModule is null)
                return;

            Components.Text(currentModule.Identifier);

            if (Components.Accordion("Module Info"))
            {
                Components.Text("Identifier: " + currentModule.Identifier);

                if (Components.Button("Open Directory"))
                {
                    FileSystemUtils.OpenDirectory(currentModule.AbsoluteDirectory);
                }

                Components.Text("Directory: " + currentModule.AbsoluteDirectory);

                if (Components.Accordion("Resources"))
                {
                    foreach (var storage in currentModule.Storages)
                    {
                        Components.Text($"{storage.Key}: {storage.Value.Count} items");
                    }
                }

                Components.Text("Total resources: " + currentModule.Storages.Sum(s => s.Value.Count));
            }
        }
        else
        {
            Components.Text("Select a module:");

            using var scrollContainer = Components.ScrollContainer().Enter();

            foreach (var module in _game.ModuleRepository.Modules)
            {
                var identifier = module.Identifier;

                if (identifier == Identifier.MainModule)
                    identifier += " (Built-in)";

                if (Components.Button(identifier))
                {
                    currentModule = module;
                }
            }
        }
    }
}
