using System.Collections;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;
using WorldGen.Debugging.UI.Modules;
using WorldGen.Graphics.UI.PaperComponents;

namespace WorldGen.Debugging.UI;

public class DebugUiRenderer : IEnumerable<IDebugModule>
{
    private readonly List<IDebugModule> Modules = [];
    private readonly CommandBar commandBar = new();

    public DebugUiRenderer(params List<IDebugModule> modules)
    {
        Modules.AddRange(modules);
    }

    public void Add(IDebugModule module)
    {
        Modules.Add(module);
    }

    public IEnumerator<IDebugModule> GetEnumerator()
    {
        return Modules.GetEnumerator();
    }

    public void Render()
    {
        commandBar.Render(Modules);

        if (Components.Paper.IsKeyPressed(Prowl.PaperUI.PaperKey.F1))
            commandBar.Toggle();

        if (Components.Paper.IsKeyPressed(Prowl.PaperUI.PaperKey.Escape))
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
                  .TranslateX(385)
                  .Transition(GuiProp.TranslateX, 0.500, Easing.QuartOut)
                  .Hovered.TranslateX(0).End()
                  .Focused.TranslateX(0).End()
                  .Enter();

        foreach (var module in Modules)
        {
            if (module.Enabled)
            {
                using (Components.Frame(module.Name, icon: module.Icon))
                    module.Render();
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
