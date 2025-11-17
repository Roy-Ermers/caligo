using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;
using Prowl.Scribe;

namespace WorldGen.Graphics.UI.PaperComponents;

public enum FontFamily
{
    Regular,
    Monospace,
    Icon
}
public static partial class Components
{
    public static int RootFontSize { get; set; } = 17;

    public static FontFile GetFont(this FontFamily family)
    {
        return family switch
        {
            FontFamily.Regular => Font,
            FontFamily.Monospace => MonospaceFont,
            FontFamily.Icon => IconFont,
            _ => Font
        };

    }
    public static ElementBuilder Text(string text, float fontSize = 16f, FontFamily fontFamily = FontFamily.Regular)
    {
        var parent = Paper.Box(text)
        .Text(text, fontFamily.GetFont())
        .FontSize(RootFontSize / fontSize * RootFontSize)
        .Height(UnitValue.Auto)
        .Width(UnitValue.Auto);
        return parent;
    }
}
