using System.Runtime.CompilerServices;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace Caligo.Client.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static ElementBuilder ScrollContainer(Scroll scroll = Scroll.ScrollY, [CallerLineNumber] int intID = 0)
    {
        var parent = Paper.Column(intID + "")
            .MinHeight(200)
            .MaxWidth(UnitValue.Percentage(100))
            .Height(UnitValue.StretchOne)
            .ColBetween(8)
            .ChildBottom(8)
            .ChildTop(8)
            .ChildLeft(8)
            .ChildRight(8)
            .SetScroll(scroll);

        return parent;
    }
}