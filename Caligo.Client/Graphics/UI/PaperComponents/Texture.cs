using System.Drawing;
using Prowl.PaperUI.LayoutEngine;

namespace Caligo.Client.Graphics.UI.PaperComponents;

public static partial class Components
{
    public static void Texture(Texture2D texture, UnitValue? width = null, UnitValue? height = null)
    {
        var aspectRatio = (float)texture.Width / texture.Height;

        var _width = width ?? (height is not null ? UnitValue.Pixels((height.Value.Value * aspectRatio)) : texture.Width);
        var _height = height ?? (width is not null ? UnitValue.Pixels((width.Value.Value / aspectRatio)) : texture.Height);

        using var container = Paper.Box("texture" + texture.Handle)
        .Width(_width)
        .Height(_height)
        .HookToParent()
        .Enter();

        Paper.AddActionElement((ev, rect) =>
        {
            if (texture is null)
                return;

            ev.Image(texture, rect.x, rect.y, rect.width, rect.height, Color.White);
        });
    }
}
