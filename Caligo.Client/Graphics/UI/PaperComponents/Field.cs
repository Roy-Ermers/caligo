using Prowl.PaperUI.LayoutEngine;

namespace Caligo.Client.Graphics.UI.PaperComponents;

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

        Text(label);

        return box;
    }
}