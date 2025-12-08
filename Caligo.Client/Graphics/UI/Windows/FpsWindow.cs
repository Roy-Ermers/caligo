using System.Numerics;
using Caligo.Core.FileSystem;
using ImGuiNET;
using OpenTK.Graphics.OpenGL;

namespace Caligo.Client.Graphics.UI.Windows;

public class FpsWindow : Window
{
    private readonly Memory<float> _fps = new float[100];
    private readonly PolygonMode[] _polygonModes = [PolygonMode.Fill, PolygonMode.Line, PolygonMode.Point];

    private int _polygonModeIndex;
    public override string Name => "Stats";
    public override ImGuiWindowFlags Flags => ImGuiWindowFlags.NoResize;
    public override bool Enabled { get; set; } = true;

    public override void Draw(double deltaTime)
    {
        GL.GetInteger(GetPName.PolygonMode, out var polygonMode);

        ImGui.Text("FPS: " + (int)(1 / deltaTime));
        ImGui.Text("CPU Memory: " + GC.GetTotalMemory(false) / 1024 / 1024 + " MB");

        if (ImGui.CollapsingHeader("FPS Graph"))
        {
            ImGui.PlotLines("##", ref _fps.Span[0], _fps.Length, 0, "##", 0, 3000, new Vector2(0, 100));

            for (var i = 0; i < _fps.Length - 1; i++) _fps.Span[i] = _fps.Span[i + 1];

            _fps.Span[^1] = (float)(1 / deltaTime);
        }


        var vendor = GL.GetString(StringName.Vendor);
        if (vendor.Contains("NVIDIA"))
        {
            GL.GetInteger((GetPName)0x9048, out var total);
            GL.GetInteger((GetPName)0x9049, out var current);

            ImGui.Text("GPU Memory: " + ByteSizeFormatter.FormatByteSize(current) + '/' +
                       ByteSizeFormatter.FormatByteSize(total));
        }

        if (ImGui.CollapsingHeader("OpenGL Info"))
        {
            ImGui.Text("Version: " + GL.GetString(StringName.Version));
            ImGui.Text($"Vendor: {vendor}");
            ImGui.Text("Renderer: " + GL.GetString(StringName.Renderer));
            ImGui.Text("GLSL Version: " + GL.GetString(StringName.ShadingLanguageVersion));
        }

        if (ImGui.CollapsingHeader("Garbage Collection"))
        {
            ImGui.Text("Total Memory: " + ByteSizeFormatter.FormatByteSize(GC.GetTotalMemory(false)));
            ImGui.Text(
                "Max Memory: " + ByteSizeFormatter.FormatByteSize(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes));
            ImGui.Text("0: " + GC.CollectionCount(0));
            ImGui.Text("1: " + GC.CollectionCount(1));
            ImGui.Text("2: " + GC.CollectionCount(2));
        }

        ImGui.Text("Polygon mode: " + _polygonModeIndex);

        if (ImGui.Combo("Polygon mode", ref _polygonModeIndex, [.. _polygonModes.Select(m => m.ToString())],
                _polygonModes.Length))
            GL.PolygonMode(TriangleFace.FrontAndBack, 2 - _polygonModeIndex + PolygonMode.Point);
    }
}