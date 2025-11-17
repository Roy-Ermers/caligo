using System.Drawing;
using Prowl.PaperUI;
using Prowl.PaperUI.Events;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static ElementBuilder ListItem(string text, bool selected = false, Action<ClickEvent>? onClick = null, char? icon = null)
    {
        var parent = Paper.Row(text)
        .Rounded(Style.FrameRounding)
        .Height(UnitValue.Auto)
        .Width(UnitValue.StretchOne)
        .BackgroundColor(Style.FrameBackground)
        .Transition(GuiProp.BackgroundColor, 0.1)
        .Focused.BackgroundColor(Style.FrameActiveBackground).End()
        .Hovered.BackgroundColor(Style.FrameActiveBackground).End()
        .TabIndex(0)
        .Border(8)
        .TabIndex(0)
        .OnHover(_ => SetCursor(OpenTK.Windowing.Common.Input.MouseCursor.PointingHand));

        if (onClick is not null)
            parent.OnClick(onClick);

        using (parent.Enter())
        {

            Components.Icon(PaperIcon.Check, 16)
            .Transition(GuiProp.TextColor, 0.1)
            .TextColor(Color.Transparent)
            .If(selected)
            .TextColor(Style.TextColor)
            .End();

            if (icon is not null)
            {
                Components.Icon(icon.Value, 16).TextColor(Style.TextColor);
            }

            Paper.Box(text)
            .Text(text, Font)
            .Height(UnitValue.StretchOne)
            .Width(UnitValue.StretchOne)
            .HookToParent()
            .Alignment(TextAlignment.MiddleLeft)
            .FontSize(16);
        }
        return parent;
    }
}
