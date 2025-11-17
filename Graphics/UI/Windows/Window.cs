using System.Drawing;
using ImGuiNET;

namespace WorldGen.Graphics.UI.Windows;

public abstract class Window
{
    public virtual string Name => "Debug";

    public virtual ImGuiWindowFlags Flags { get; set; } = ImGuiWindowFlags.None;
    public virtual Rectangle? Area { get; set; } = null;
    public virtual bool Enabled { get; set; } = false;

    public virtual void Initialize(Game game) { }

    public abstract void Draw(double deltaTime);
}
