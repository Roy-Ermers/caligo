using System.Drawing;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace Caligo.Client.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static ElementBuilder Frame(string name, bool renderHeader = true, char? icon = null)
    {
        var parent = Paper.Column(name)
        .BackgroundColor(Style.WindowBackground)
        .BorderColor(Style.BorderColor)
        .BoxShadow(0, 2, 4, 0, Color.FromArgb(35, 0, 0, 0))
        .Rounded(16)
        .Height(UnitValue.Auto)
        .Width(UnitValue.StretchOne)
        .ColBetween(8)
        .Border(8)
        .SetScroll(Prowl.PaperUI.Scroll.ScrollY);
        parent.Enter();


        if (renderHeader)
        {
            using var header = Paper.Row(name + "header")
            .Margin(4, 4, 0, 8)
            .Height(24)
            .Enter();
            if (icon is not null)
            {
                Components.Icon(icon.Value).PositionType(PositionType.SelfDirected).FontSize(20);
            }

            Components.Text(name).Alignment(Prowl.PaperUI.TextAlignment.MiddleCenter).Height(UnitValue.StretchOne);
        }

        return parent;
    }
}
