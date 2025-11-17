using System.Drawing;

namespace Caligo.Client.Debugging;

public static class WorldDebug
{
    public static void DebugRender(this Core.Universe.World.World world)
    {
        foreach (var item in world.Features)
        {
            Gizmo3D.DrawBoundingBox(item.BoundingBox, Color.Yellow);
        }
    }
}
