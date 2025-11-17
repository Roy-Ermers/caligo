using ImGuiNET;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.Graphics.UI.Windows;

public class CameraWindow : Window
{
    public override string Name => "Camera";
    public override bool Enabled { get; set; } = true;

    Game Game = null!;

    public override void Initialize(Game game)
    {
        Game = game;
    }

    public override void Draw(double deltaTime)
    {
        var camera = Game.Camera;

        var blockPosition = new WorldPosition((int)camera.Position.X, (int)camera.Position.Y, (int)camera.Position.Z);

        ImGui.Text("Position: " + camera.Position);

        var sector = blockPosition / 64;
        ImGui.Text("Sector: " + sector);

        ImGui.Text("Chunk: " + blockPosition.ChunkPosition);
    }

}
