using WorldGen.Debugging.UI.Modules;
using WorldGen.Graphics.UI.PaperComponents;

namespace WorldGen.Debugging.UI;

public class CommandBar
{
    public bool Visible = false;
    private string search = "";

    public void Toggle(bool? force = null)
    {
        Visible = force ?? !Visible;
        if (Visible)
        {
            search = "";
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
        .If(Visible)
        .MaxHeight(300)
        .End();

        var textbox = Components.Textbox(ref search, FontFamily.Monospace, placeholder: "Search modules...");

        foreach (var module in modules)
        {
            if (!module.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) && !string.IsNullOrWhiteSpace(search))
                continue;

            Components.ListItem(module.Name, module.Enabled, _ => module.Enabled = !module.Enabled, module.Icon);
        }
    }
}
