using System.Runtime.CompilerServices;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static ElementBuilder Divider([CallerLineNumber] int intID = 0)
    {
        return Paper.Box("divider" + intID)
        .Margin(0, 0, 0, 8)
        .Height(1)
        .Width(UnitValue.Percentage(100))
        .BackgroundColor(Style.BorderColor);
    }
}
