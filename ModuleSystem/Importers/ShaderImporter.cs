using WorldGen.Renderer.Shaders;

namespace WorldGen.ModuleSystem.Importers;

public class ShaderImporter : IImporter
{
  public void Import(Module module)
  {
    var rootDirectory = Path.Join(module.AbsoluteDirectory, "shaders");
    if (!Directory.Exists(rootDirectory))
      return;

    var files = Directory.EnumerateFiles(rootDirectory, "*.*", SearchOption.AllDirectories);
    var renderStorage = module.GetStorage<RenderShader>();
    var computeStorage = module.GetStorage<ComputeShader>();

    foreach (var file in files)
    {
      try
      {
        var extension = Path.GetExtension(file);

        if (extension != ".vert" && extension != ".comp")
        {
          continue;
        }

        var name = Path.GetFileNameWithoutExtension(file);
        var directory = Path.GetDirectoryName(file) ?? "";
        var shaderName = Identifier.Create(module.Identifier, name);

        switch (extension)
        {
          case ".vert":
            {
              var fragFile = Path.Combine(directory, $"{name}.frag");
              var vertFile = Path.Combine(directory, $"{name}.vert");
              if (File.Exists(fragFile) && File.Exists(vertFile))
              {
                var renderShader = new RenderShader(vertFile, fragFile);
                renderStorage.Add(shaderName, renderShader);
              }

              break;
            }
          case ".comp":
            {
              var computeShader = new ComputeShader(file);
              computeStorage.Add(shaderName, computeShader);
              break;
            }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine($"Error importing shader: {file}");
        Console.WriteLine(e.Message);
      }
    }
  }
}
