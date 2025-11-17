using System.Numerics;
using ImGuiNET;

namespace WorldGen.Renderer.UI.Components;


public enum TextStyle
{
    Regular,
    Secondary
}
public struct TextComponent(string content) : IDrawableComponent
{
    public readonly string Content = content;
    public TextStyle Style { get; private set; } = TextStyle.Regular;
    public bool EnableWrapping { get; private set; } = false;

    public Vector4? Color { get; private set; }

    private readonly Vector4? ImplicitColor
    {
        get
        {
            if (Color is not null) return Color.Value;
            if (Style == TextStyle.Secondary)
                return UiStyle.Current.SecondaryTextColor;

            return null;
        }
    }


    public TextComponent WithStyle(TextStyle style)
    {
        return this with { Style = style };
    }

    public TextComponent WithWrapping(bool enable = true)
    {
        return this with { EnableWrapping = enable };
    }

    public TextComponent WithColor(Vector4 color)
    {
        return this with { Color = color };
    }

    public readonly void Draw()
    {

        if (ImplicitColor is not null)
            ImGui.PushStyleColor(ImGuiCol.Text, ImplicitColor.Value);

        if (EnableWrapping)
            ImGui.TextWrapped(Content);
        else
            ImGui.Text(Content);

        if (ImplicitColor is not null)
            ImGui.PopStyleColor();
    }
}
