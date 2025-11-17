using System.Drawing;
using System.Runtime.CompilerServices;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static ElementBuilder ScrollContainer([CallerLineNumber] int intID = 0)
    {
        var parent = Paper.Column(intID + "")
        .MinHeight(200)
        .Height(UnitValue.StretchOne)
        .Width(UnitValue.StretchOne)
        .ColBetween(8)
        .ChildBottom(8)
        .ChildTop(8)
        .ChildLeft(8)
        .ChildRight(8)
        .SetScroll(Prowl.PaperUI.Scroll.ScrollY);

        return parent;
    }
}
