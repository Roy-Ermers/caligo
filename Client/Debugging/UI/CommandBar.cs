using Caligo.Client.Debugging.UI.Modules;
using Caligo.Client.Graphics.UI.PaperComponents;

namespace Caligo.Client.Debugging.UI;

public class CommandBar
{
    private bool _visible;
    private string _search = "";

    public void Toggle(bool? force = null)
    {
        _visible = force ?? !_visible;
        if (_visible)
        {
            _search = "";
        }
    }

    public void Render(List<IDebugModule> modules)
    {
        using var commandBar = Components.Frame("Commandbar", false)
        .PositionType(Prowl.PaperUI.PositionType.SelfDirected)
        .Margin(Prowl.PaperUI.LayoutEngine.UnitValue.StretchOne)
        .MaxHeight(0)
        .SetScroll(Prowl.PaperUI.Scroll.ScrollY)
        .BoxShadow(Prowl.PaperUI.BoxShadow.None)
        .Transition(Prowl.PaperUI.GuiProp.MaxHeight, 0.25)
        .If(_visible)
        .MaxHeight(300)
        .End();

         Components.Textbox(ref _search, FontFamily.Monospace, placeholder: "Search modules...");

        foreach (var module in modules)
        {
            if (!module.Name.Contains(_search, StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrWhiteSpace(_search))
                continue;

            Components.ListItem(module.Name, module.Enabled, _ => module.Enabled = !module.Enabled, module.Icon);
        }
    }
}
