using System.Numerics;
using ImGuiNET;

namespace WorldGen.Graphics.UI.Components;

public readonly struct ListComponent : IDisposable
{
    public ListComponent(string Name)
    {
        ImGui.BeginListBox(Name);
    }

    public void End()
    {
        this.Dispose();
    }

    public void Dispose()
    {
        ImGui.EndListBox();
    }
}
