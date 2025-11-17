using System.Drawing;
using Prowl.PaperUI;
using Prowl.PaperUI.Events;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

record struct CurrentTooltip(
    ulong Id,
        double X,
    double Y,
    string Text
);

public static partial class Components
{
    private static CurrentTooltip? currentTooltip = null;
    private static bool ShowTooltip = false;

    public static void Tooltip(ElementBuilder element, string text)
    {
        element.OnHover(e => OnHover(e, text))
        .OnFocusChange(e => OnFocus(e, text));
    }

    private static void OnHover(ElementEvent e, string text)
    {
        currentTooltip = new(0, e.PointerPosition.x, e.PointerPosition.y, text);

        ShowTooltip = true;
        ShowTooltip = true;
    }

    private static void OnFocus(FocusEvent e, string text)
    {
        if (!e.IsFocused)
            return;
        currentTooltip = new(0, e.Source.Data.LayoutRect.x, e.Source.Data.LayoutRect.Top, text);

        ShowTooltip = true;
    }

    public static void RenderTooltip()
    {


        Paper.Box("tooltip")
        .Layer(Layer.Overlay)
        .PositionType(PositionType.SelfDirected)
        .Position(Math.Round(currentTooltip?.X ?? 0) + 16, Math.Round(currentTooltip?.Y ?? 0) - 8)
        .Width(UnitValue.Auto)
        .Height(UnitValue.Auto)
        .Text(currentTooltip?.Text ?? string.Empty, Font)
        .Rounded(1)
        .BackgroundColor(Style.FrameBackground)
        .IsNotInteractable()
        .BorderWidth(16)
        .BorderColor(Style.FrameBackground)
        .Visible(ShowTooltip)
        ;
        ShowTooltip = false;
    }
}
