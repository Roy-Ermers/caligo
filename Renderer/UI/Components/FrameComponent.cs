using System.Numerics;
using ImGuiNET;

namespace WorldGen.Renderer.UI.Components;

public readonly struct FrameComponent(string Name) : IDisposable
{
    public FrameComponent Enter()
    {
        ImGui.BeginChild(Name, new Vector2(0, 0), ImGuiChildFlags.FrameStyle | ImGuiChildFlags.AutoResizeY);
        return this;
    }

    public void End()
    {
        this.Dispose();
    }

    public void Dispose()
    {
        ImGui.EndChild();
    }
}
