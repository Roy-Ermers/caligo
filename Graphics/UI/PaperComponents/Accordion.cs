using System.Drawing;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    private static readonly List<string> OpenedAccordions = [];
    public static bool Accordion(string name)
    {
        var isOpen = OpenedAccordions.Contains(name);
        using var header = Paper.Row(name + "header")
        .Rounded(Style.FrameRounding)
        .BackgroundColor(Style.HeaderBackground)
        .Width(UnitValue.StretchOne)
        .Height(UnitValue.Auto)
        .TabIndex(0)
        .Transition(GuiProp.BackgroundColor, 0.1)
        .Hovered.BackgroundColor(Style.HeaderHoverBackground).End()
        .Active.BackgroundColor(Style.HeaderActiveBackground).End()
        .Focused.BackgroundColor(Style.HeaderActiveBackground).End()
        .OnPress(_ =>
        {
            if (!OpenedAccordions.Remove(name))
                OpenedAccordions.Add(name);
        })
        .Enter();

        Components.Icon(isOpen ? PaperComponents.PaperIcon.ArrowDropDown : PaperComponents.PaperIcon.ArrowRight).Margin(8, UnitValue.StretchOne);

        Components.Text(name)
        .Alignment(TextAlignment.MiddleLeft)
        .Height(UnitValue.StretchOne)
        .Margin(0, 16)
        .FontSize(18);

        return isOpen;
    }
}
