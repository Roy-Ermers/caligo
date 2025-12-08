using System.Collections;
using Caligo.Client.Debugging.UI.Modules;
using Caligo.Client.Graphics.UI.PaperComponents;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace Caligo.Client.Debugging.UI;

public class DebugUiRenderer : IEnumerable<IDebugModule>
{
    private readonly CommandBar commandBar = new();
    private readonly List<IDebugModule> Modules = [];

    public DebugUiRenderer(params List<IDebugModule> modules)
    {
        Modules.AddRange(modules);
    }

    public IEnumerator<IDebugModule> GetEnumerator()
    {
        return Modules.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(IDebugModule module)
    {
        Modules.Add(module);
    }

    public void Render()
    {
        commandBar.Render(Modules);

        if (Components.Paper.IsKeyPressed(PaperKey.F1))
            commandBar.Toggle();

        if (Components.Paper.IsKeyPressed(PaperKey.Escape))
            commandBar.Toggle(false);

        using var sidebar = Components.Box()
            .PositionType(PositionType.SelfDirected)
            .LayoutType(LayoutType.Column)
            .ColBetween(16)
            .Left(UnitValue.StretchOne)
            .Top(0)
            .Bottom(0)
            .Height(UnitValue.StretchOne)
            .MaxHeight(UnitValue.Percentage(100))
            .SetScroll(Scroll.ScrollY)
            .Width(400)
            .Border(32)
            .Enter();

        foreach (var module in Modules)
            if (module.Enabled)
                using (Components.Frame(module.Name, icon: module.Icon))
                {
                    module.Render();
                }
    }
}