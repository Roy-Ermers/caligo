using System.Drawing;
using System.Runtime.CompilerServices;
using OpenTK.Windowing.Common.Input;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static ElementBuilder InputBox(
        string id,
        char? icon = null,
        MouseCursor? cursor = null
    )
    {
        var box = Paper.Row(id)
        .Width(UnitValue.StretchOne)
        .MinHeight(32)
        .Height(UnitValue.Auto)
        .Rounded(Style.FrameRounding)
        .BackgroundColor(Style.TitleCollapsedBackground)
        .BorderWidth(1)
        .BorderColor(Style.FrameActiveBackground)
        .TabIndex(0)
        .Border(8)
        .RowBetween(4)
        .OnHover(_ => SetCursor(cursor ?? MouseCursor.IBeam))
        .Transition(GuiProp.BackgroundColor, 0.1)
        .Transition(GuiProp.BorderColor, 0.1)
        .BorderWidth(1)
        .Focused
         .BackgroundColor(Style.FrameBackground)
         .BorderColor(Style.AccentColor)
        .End();

        box.Enter();

        if (icon is not null)
        {
            var iconComponent = Components.Icon(icon.Value, 16).TextColor(Style.TextColor);

        }


        return box;
    }
}
