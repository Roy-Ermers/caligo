using System.Drawing;
using Prowl.PaperUI;
using Prowl.PaperUI.Events;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

record struct CurrentTooltip(
    ulong Id,
    double X,
    double Y,
    Action Content
);

public static partial class Components
{
    private static CurrentTooltip? currentTooltip = null;
    private static bool ShowTooltip = false;

    public static void Tooltip(ElementBuilder element, Action content)
    {
        element.OnHover(e => OnHover(e, content))
        .OnFocusChange(e => OnFocus(e, content));
    }

    private static void OnHover(ElementEvent e, Action content)
    {
        currentTooltip = new(0, e.Source.Data.LayoutRect.x, e.Source.Data.LayoutRect.Top, content);

        ShowTooltip = true;
        ShowTooltip = true;
    }

    private static void OnFocus(FocusEvent e, Action content)
    {
        if (!e.IsFocused)
            return;
        currentTooltip = new(0, e.Source.Data.LayoutRect.x, e.Source.Data.LayoutRect.Top, content);

        ShowTooltip = true;
    }

    public static void RenderTooltip()
    {
        using var tooltip = Paper.Box("tooltip")
        .Layer(Layer.Overlay)
        .PositionType(PositionType.SelfDirected)
        .Position(Math.Round(currentTooltip?.X ?? 0) + 16, Math.Round(currentTooltip?.Y ?? 0) - 8)
        .Width(UnitValue.Auto)
        .Height(UnitValue.Auto)
        .Rounded(1)
        .BackgroundColor(Style.FrameBackground)
        .IsNotInteractable()
        .IsNotFocusable()
        .BorderWidth(16)
        .BorderColor(Style.FrameBackground)
        .Visible(ShowTooltip)
        .Enter();

        currentTooltip?.Content();

        ShowTooltip = false;
    }
}
