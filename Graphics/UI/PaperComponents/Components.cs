using Prowl.PaperUI;
using Prowl.Scribe;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    internal static Paper Paper => PaperRenderer.Current.Paper;
    internal static FontFile Font => PaperRenderer.Current.Font;
    internal static FontFile MonospaceFont => PaperRenderer.Current.MonospaceFont;
    internal static FontFile IconFont => PaperRenderer.Current.IconFont;
    internal static UiStyle Style => PaperRenderer.Current.Style;

    internal static event Action? OnEnd;

    static Components()
    {
        PaperRenderer.Current.OnFrameEnd += () => OnEnd?.Invoke();

        OnEnd += () => RenderTooltip();
    }

    internal static void SetCursor(OpenTK.Windowing.Common.Input.MouseCursor cursor)
    {
        PaperRenderer.Current.SetCursor(cursor);
    }
}
