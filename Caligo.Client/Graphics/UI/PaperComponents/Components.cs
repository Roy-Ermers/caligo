using OpenTK.Windowing.Common.Input;
using Prowl.PaperUI;
using Prowl.Scribe;

namespace Caligo.Client.Graphics.UI.PaperComponents;

public static partial class Components
{
    static Components()
    {
        PaperRenderer.Current.OnFrameEnd += () => OnEnd?.Invoke();

        OnEnd += () => RenderTooltip();
        OnEnd += () => RenderSelectDropdown();
    }

    internal static Paper Paper => PaperRenderer.Current.Paper;
    internal static FontFile Font => PaperRenderer.Current.Font;
    internal static FontFile MonospaceFont => PaperRenderer.Current.MonospaceFont;
    internal static FontFile IconFont => PaperRenderer.Current.IconFont;
    internal static UiStyle Style => PaperRenderer.Current.Style;

    internal static event Action? OnEnd;

    internal static void SetCursor(MouseCursor cursor)
    {
        PaperRenderer.Current.SetCursor(cursor);
    }
}