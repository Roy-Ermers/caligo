using System.Runtime.CompilerServices;
using Prowl.PaperUI.LayoutEngine;

namespace Caligo.Client.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static IDisposable ButtonGroup([CallerLineNumber] int intID = 0)
    {
        var parent = Paper.Row("button-group" + intID).ChildLeft(UnitValue.StretchOne).Height(UnitValue.Auto);

        return parent.Enter();
    }
}
