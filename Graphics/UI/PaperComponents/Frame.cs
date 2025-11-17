using System.Drawing;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static ElementBuilder Frame(string name, bool renderHeader = true)
    {
        var parent = Paper.Column(name)
        .BackgroundColor(Style.WindowBackground)
        .Border(Style.BorderWidth)
        .BorderColor(Style.BorderColor)
        .BoxShadow(0, 2, 4, 0, Color.FromArgb(35, 0, 0, 0))
        .Rounded(16)
        .Height(UnitValue.Auto)
        .Width(UnitValue.StretchOne)
        .ColBetween(8)
        .ChildBottom(8)
        .ChildTop(8)
        .ChildLeft(8)
        .ChildRight(8)
        .SetScroll(Prowl.PaperUI.Scroll.ScrollY);
        parent.Enter();


        if (renderHeader)
        {
            using var header = Paper.Row(name + "header")
            .Rounded(Style.FrameRounding)
            .Margin(0, 0, 8, 4)
            .Height(UnitValue.Auto)
            .Enter();

            Components.Text(name).Alignment(Prowl.PaperUI.TextAlignment.Center);
        }

        return parent;
    }
}
