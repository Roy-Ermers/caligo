using System.Runtime.CompilerServices;
using Prowl.PaperUI;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static ElementBuilder Box([CallerLineNumber] int intID = 0)
    {
        return Paper.Row("box " + intID);
    }
}
