using OpenTK.Graphics.OpenGL4;
using WorldGen.FileSystem;
using WorldGen.Graphics.UI.PaperComponents;

namespace WorldGen.Debugging.UI.Modules;

public class OpenGLDebugModule : IDebugModule
{
    public bool Enabled { get; set; }

    public string Name => "OpenGL";

    public char? Icon => PaperIcon.Sdk;

    private readonly string[] SupportedExtensions;
    private string search = "";

    public OpenGLDebugModule()
    {
        var supportedExtensions = new List<string>();
        int count = GL.GetInteger(GetPName.NumExtensions);
        for (int i = 0; i < count; i++)
        {
            string extension = GL.GetString(StringNameIndexed.Extensions, i);
            supportedExtensions.Add(extension);
        }

        SupportedExtensions = [.. supportedExtensions];
    }

    public void Render()
    {
        var vendor = GL.GetString(StringName.Vendor);
        if (vendor.Contains("NVIDIA"))
        {
            GL.NV.GetInteger((All)0x9048, out long total);
            GL.NV.GetInteger((All)0x9049, out long current);

            Components.Text("GPU Memory: " + ByteSizeFormatter.FormatByteSize(current) + '/' + ByteSizeFormatter.FormatByteSize(total));
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
