using Prowl.PaperUI;
using Prowl.PaperUI.Events;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static ElementBuilder Button(string text, Action<ClickEvent> onClick)
    {
        var parent = Paper.Row(text)
        .BackgroundColor(Style.AccentColor)
        .Rounded(Style.FrameRounding)
        .Height(UnitValue.Auto)
        .Width(UnitValue.Auto)
        .Transition(GuiProp.BackgroundColor, 0.1)
        .TabIndex(0)
        .Hovered.BackgroundColor(Style.AccentHoverColor).End()
        .Active.BackgroundColor(Style.AccentActiveColor).End()
        .OnClick(onClick)
        .OnHover(_ => SetCursor(OpenTK.Windowing.Common.Input.MouseCursor.PointingHand));

        using (parent.Enter())
        {
            Paper.Box(text)
            .Text(text, Font)
            .Margin(16, 4)
            .Height(UnitValue.Auto)
            .Width(UnitValue.Auto)
            .FontSize(16);
        }
        return parent;
    }
}
