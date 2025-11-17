using WorldGen.Graphics;
using WorldGen.Graphics.UI.PaperComponents;
using WorldGen.Graphics.UI.PaperComponents.fields;
using WorldGen.Universe.PositionTypes;

namespace WorldGen.Debugging.UI.Modules;

public class CameraDebugModule : IDebugModule
{
    public bool Enabled { get; set; }

    public string Name => "Camera";

    public char? Icon => PaperIcon.CameraAlt;

    private readonly Camera Camera;

    private float renderDistance = 15;

    public CameraDebugModule(Game game)
    {
        Camera = game.Camera;
    }

    public void Render()
    {
        var blockPosition = new WorldPosition((int)Camera.Position.X, (int)Camera.Position.Y, (int)Camera.Position.Z);

        FieldComponents.Vector3("Position", ref Camera.Position);

        using (Components.Row())
        {
            FieldComponents.Object("Pitch", ref Camera.Pitch);
            FieldComponents.Object("Yaw", ref Camera.Yaw);
        }

        // Components.Text("Position: " + Camera.Position);

        var sector = blockPosition / 64;
        Components.Text("Sector: " + sector);

        Components.Text("Chunk: " + blockPosition.ChunkPosition);
        
        Components.NumberInput(ref renderDistance);

        Game.Instance.renderer.RenderDistance = (int)renderDistance;
    }
}
