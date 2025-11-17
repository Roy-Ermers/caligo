using System.Drawing;
using WorldGen.Debugging;

namespace WorldGen.Universe;

public partial class World : IEnumerable<Chunk>
{
    public void DebugRender()
    {
        foreach (var item in Features)
        {
            Gizmo3D.DrawBoundingBox(item.BoundingBox, Color.Yellow);
        }
    }
}
