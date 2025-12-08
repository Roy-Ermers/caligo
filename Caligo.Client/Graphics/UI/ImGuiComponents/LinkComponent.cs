using ImGuiNET;

namespace Caligo.Client.Graphics.UI.ImGuiComponents;

public struct LinkComponent(string content) : IDrawableComponent
{
    public readonly string Content = content;
    private Action? OnClickAction;

    public LinkComponent OnClick(Action action)
    {
        return this with { OnClickAction = action };
    }

    public readonly void Draw()
    {
        if (ImGui.TextLink(Content) && OnClickAction is not null)
            OnClickAction();
    }
}