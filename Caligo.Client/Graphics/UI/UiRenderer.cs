using System.Numerics;
using Caligo.Client.Graphics.UI.Windows;
using Caligo.Core.FileSystem;
using ImGuiNET;

namespace Caligo.Client.Graphics.UI;

public class UiRenderer
{
    private readonly ImGuiRenderer _imGui;
    private readonly UiStyle _style;

    private readonly List<Window> _windows =
    [
        new FpsWindow(),
        new ModuleWindow(),
        new ChunkWindow(),
        new ResourcesWindow(),
        new CameraWindow()
    ];

    public UiRenderer(Game game)
    {
        _imGui = new ImGuiRenderer(game);
        var config = game.ModuleRepository.GetAll<string>("Config");
        _style = UiStyle.FromConfig(config);
        var defaultFont = game.ModuleRepository.Get<Font>(_style.FontName);
        ImGuiRenderer.AddFont(defaultFont, _style.FontSize);

        _imGui.Initialize();

        UpdateStyle();

        foreach (var window in _windows)
            window.Initialize(game);
    }

    private void UpdateStyle()
    {
        var io = ImGui.GetIO();
        io.FontAllowUserScaling = true;

        var imStyle = ImGui.GetStyle();
        _style.Apply(imStyle);
    }

    public UIFrame StartFrame(double deltaTime)
    {
        var frame = new UIFrame(_imGui);
        frame.Start(deltaTime);

        return frame;
    }

    public void Draw(double deltaTime)
    {
        ImGui.Text($"FPS {(int)(1 / deltaTime)}");

        var dockspace = ImGui.DockSpaceOverViewport(0, ImGui.GetMainViewport(),
            ImGuiDockNodeFlags.PassthruCentralNode | ImGuiDockNodeFlags.NoDockingOverCentralNode);

        foreach (var window in _windows)
        {
            if (!window.Enabled) continue;

            var moduleEnabled = window.Enabled;
            if (window.Area.HasValue)
            {
                var location = window.Area.Value.Location;
                var size = window.Area.Value.Size;
                ImGui.SetNextWindowPos(new Vector2(location.X, location.Y), ImGuiCond.FirstUseEver);
                ImGui.SetNextWindowSize(new Vector2(size.Width, size.Height),
                    ImGuiCond.FirstUseEver);
            }

            ImGui.Begin(window.Name, ref moduleEnabled,
                ImGuiWindowFlags.NoCollapse | window.Flags);
            window.Draw(deltaTime);
            ImGui.End();

            window.Enabled = moduleEnabled;
        }
    }

    public void WindowResized(int width, int height)
    {
        _imGui.WindowResized(width, height);
    }
}

public readonly struct UIFrame(ImGuiRenderer imGuiRenderer) : IDisposable
{
    private readonly ImGuiRenderer imGuiRenderer = imGuiRenderer;

    public readonly void Start(double deltaTime)
    {
        imGuiRenderer.Begin(deltaTime);
        ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new Vector2(ImGui.GetIO().DisplaySize.X, ImGui.GetIO().DisplaySize.Y),
            ImGuiCond.Always);
        ImGui.Begin("fullscreen", ImGuiWindowFlags.NoDecoration |
                                  ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings |
                                  ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus |
                                  ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMouseInputs);
    }

    public void End()
    {
        ImGui.End();
        imGuiRenderer.End();
    }

    public readonly void Dispose()
    {
        End();
    }
}