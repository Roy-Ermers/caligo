using System.Runtime.CompilerServices;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static ElementBuilder Row(double spacing = 8, [CallerLineNumber] int intID = 0)
    {
        var row = Paper.Row(intID + "row")
                .Height(UnitValue.Auto)
                .Width(UnitValue.StretchOne)
                .RowBetween(spacing);
        row.Enter();
        return row;
    }
}
