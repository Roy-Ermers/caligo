using Caligo.Client.Graphics.UI.PaperComponents;
using Caligo.Core.FileSystem;
using OpenTK.Graphics.OpenGL4;

namespace Caligo.Client.Debugging.UI.Modules;

public class OpenGLDebugModule : IDebugModule
{
    private readonly string[] SupportedExtensions;
    private string search = "";

    public OpenGLDebugModule()
    {
        var supportedExtensions = new List<string>();
        var count = GL.GetInteger(GetPName.NumExtensions);
        for (var i = 0; i < count; i++)
        {
            var extension = GL.GetString(StringNameIndexed.Extensions, i);
            supportedExtensions.Add(extension);
        }

        SupportedExtensions = [.. supportedExtensions];
    }

    public bool Enabled { get; set; }

    public string Name => "OpenGL";

    public char? Icon => PaperIcon.Sdk;

    public void Render()
    {
        var vendor = GL.GetString(StringName.Vendor);
        if (vendor.Contains("NVIDIA"))
        {
            GL.NV.GetInteger((All)0x9048, out long total);
            GL.NV.GetInteger((All)0x9049, out long current);

            Components.Text("GPU Memory: " + ByteSizeFormatter.FormatByteSize(current) + '/' +
                            ByteSizeFormatter.FormatByteSize(total));
        }

        Components.Text("Version: " + GL.GetString(StringName.Version));
        Components.Text($"Vendor: {vendor}");
        Components.Text("Renderer: " + GL.GetString(StringName.Renderer));
        Components.Text("GLSL Version: " + GL.GetString(StringName.ShadingLanguageVersion));

        if (Components.Accordion("Extensions"))
        {
            Components.Textbox(ref search, placeholder: "search", icon: PaperIcon.Search);
            using var scrollContainer = Components.ScrollContainer().Enter();
            foreach (var extension in SupportedExtensions)
            {
                if (!extension.Contains(search))
                    continue;
                Components.Text(extension, fontFamily: FontFamily.Monospace);
            }
        }
    }
}