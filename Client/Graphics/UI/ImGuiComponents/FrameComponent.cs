using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace Caligo.Client.Graphics.UI.ImGuiComponents;

public struct FrameComponent(string Name)
{
    public readonly IDisposable Enter()
    {
        var renderer = PaperRenderer.Current;
        var parent = renderer.Paper.Column(Name)
        .BackgroundColor(renderer.Style.FrameBackground)
        .Rounded(renderer.Style.FrameRounding).Margin(UnitValue.Auto).Enter();

        renderer.Paper.Box("Header").Height(16).Text(Name, renderer.Font).Alignment(TextAlignment.MiddleCenter);
        return parent;
    }
}
