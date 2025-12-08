using System.Drawing;
using Caligo.Client.Graphics;
using Caligo.Client.Graphics.UI.PaperComponents;
using Caligo.Client.Graphics.UI.PaperComponents.fields;
using Caligo.Core.Spatial;
using Caligo.Core.Spatial.PositionTypes;
using Caligo.Core.Universe;
using Caligo.Core.Universe.Worlds;
using Vector3 = System.Numerics.Vector3;

namespace Caligo.Client.Debugging.UI.Modules;

public class CameraDebugModule : IDebugModule
{
    private readonly Camera Camera;
    private readonly World world;

    private bool renderChunk;

    private float renderDistance = 15;
    private bool renderHit;

    public CameraDebugModule(Game game)
    {
        Camera = game.Camera;
        world = game.world;
    }

    public bool Enabled { get; set; }

    public string Name => "Camera";

    public char? Icon => PaperIcon.CameraAlt;

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

        if (Components.Accordion("Current Chunk"))
        {
            Components.Text("Chunk: " + blockPosition.ChunkPosition);
            Components.Text("Sector: " + sector);

            Components.Checkbox(ref renderChunk, "Render Chunk Bounding Box");

            if (renderChunk)
                Gizmo3D.DrawBoundingBox(new BoundingBox(blockPosition.ChunkPosition.ToWorldPosition(), Chunk.Size),
                    Color.Lime);
        }

        if (Components.Accordion("Current Block"))
        {
            var position = (Vector3)Camera.Position;
            Components.Checkbox(ref renderHit, "Render Hit Info");

            if (world.Raycast(
                    new Ray(position, (Vector3)Camera.Forward),
                    16,
                    out var hit)
               )
            {
                Components.Text("Hit Block: " + hit.Block.Name);
                Components.Text("Hit Position: " + hit.Position);
                Components.Text("Distance: " + hit.Distance.ToString("0.00"));
                Components.Text($"Normal: {hit.Normal.X}, {hit.Normal.Y}, {hit.Normal.Z}");


                if (renderHit)
                {
                    Gizmo3D.DrawBoundingBox(new BoundingBox(hit.Position, 1, 1, 1));
                    Gizmo3D.DrawLine(hit.HitPoint, hit.HitPoint + hit.Normal, Color.Red);
                }
            }
            else
            {
                Components.Text("Not looking at any block within 16 units.");
            }
        }
    }
}