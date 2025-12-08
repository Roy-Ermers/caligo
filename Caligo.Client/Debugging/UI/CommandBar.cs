using Caligo.Client.Debugging.UI.Modules;
using Caligo.Client.Graphics.UI.PaperComponents;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace Caligo.Client.Debugging.UI;

public class CommandBar
{
    private string _search = "";
    private bool _visible;

    public void Toggle(bool? force = null)
    {
        _visible = force ?? !_visible;
        if (_visible) _search = "";
    }

    public void Render(List<IDebugModule> modules)
    {
        using var commandBar = Components.Frame("Commandbar", false)
            .PositionType(PositionType.SelfDirected)
            .Margin(UnitValue.StretchOne)
            .MaxHeight(0)
            .SetScroll(Scroll.ScrollY)
            .BoxShadow(BoxShadow.None)
            .Transition(GuiProp.MaxHeight, 0.25)
            .If(_visible)
            .MaxHeight(300)
            .End();

        Components.Textbox(ref _search, FontFamily.Monospace, placeholder: "Search modules...");

        foreach (var module in modules)
        {
            if (!module.Name.Contains(_search, StringComparison.CurrentCultureIgnoreCase) &&
                !string.IsNullOrWhiteSpace(_search))
                continue;

            Components.ListItem(module.Name, module.Enabled, _ => module.Enabled = !module.Enabled, module.Icon);
        }
    }
}