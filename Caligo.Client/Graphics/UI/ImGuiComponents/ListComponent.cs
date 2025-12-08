using ImGuiNET;

namespace Caligo.Client.Graphics.UI.ImGuiComponents;

public readonly struct ListComponent : IDisposable
{
    public ListComponent(string Name)
    {
        ImGui.BeginListBox(Name);
    }

    public void End()
    {
        Dispose();
    }

    public void Dispose()
    {
        ImGui.EndListBox();
    }
}