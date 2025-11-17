using Prowl.PaperUI;
using Prowl.PaperUI.Events;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static IDisposable Field(string label)
    {

        var box = Paper.Column(label)
        .ColBetween(4)
        .Width(UnitValue.StretchOne)
        .Height(UnitValue.Auto)
        .BorderBottom(8)
        .FontSize(RootFontSize).Enter();

        Components.Text(label);

        return box;
    }
}
