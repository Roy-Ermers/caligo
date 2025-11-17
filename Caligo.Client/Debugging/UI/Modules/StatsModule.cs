using Caligo.Client.Graphics.UI.PaperComponents;
using Caligo.Core.FileSystem;
using OpenTK.Graphics.OpenGL4;

namespace Caligo.Client.Debugging.UI.Modules;

public class StatsDebugModule : IDebugModule
{
    public bool Enabled { get; set; }

    public string Name => "Stats";

    public char? Icon => PaperIcon.AvgPace;

    private readonly Memory<float> _fps = new float[100];
    private readonly Game _game;
    private double _lastTime = 0;

    public StatsDebugModule(Game game)
    {
        _game = game;
    }

    public void Render()
    {
        // FPS and Performance
        var currentTime = _game.Time;
        var deltaTime = currentTime - _lastTime;
        if (deltaTime <= 0) deltaTime = 1.0 / 60.0; // Fallback
        _lastTime = currentTime;

        Components.Text("FPS: " + (int)(1 / deltaTime));
        Components.Text("CPU Memory: " + GC.GetTotalMemory(false) / 1024 / 1024 + " MB");

        // GPU Memory (NVIDIA only)
        var vendor = GL.GetString(StringName.Vendor);
        if (vendor.Contains("NVIDIA"))
        {
            GL.GetInteger((GetPName)0x9048, out var total);
            GL.GetInteger((GetPName)0x9049, out var current);

            Components.Text("GPU Memory: " + ByteSizeFormatter.FormatByteSize(current) + '/' + ByteSizeFormatter.FormatByteSize(total));
        }

        if (Components.Accordion(PaperIcon.DeleteSweep + " Garbage collection"))
        {
            Components.Text("Total Memory: " + ByteSizeFormatter.FormatByteSize(GC.GetTotalMemory(false)));
            Components.Text("Max Memory: " + ByteSizeFormatter.FormatByteSize(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes));
            Components.Text("0: " + GC.CollectionCount(0));
            Components.Text("1: " + GC.CollectionCount(1));
            Components.Text("2: " + GC.CollectionCount(2));
        }
    }
}
