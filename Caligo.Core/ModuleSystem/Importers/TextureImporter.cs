using Caligo.Core.FileSystem.Images;
using Caligo.ModuleSystem;
using Caligo.ModuleSystem.Importers;

namespace Caligo.Core.ModuleSystem.Importers;

public class TextureImporter : IImporter
{
    public void Import(Module module)
    {
        var storage = module.GetStorage<Image>();

        var rootDirectory = Path.Combine(module.AbsoluteDirectory, "textures");

        if (!Directory.Exists(rootDirectory))
            return;

        var files = Directory.EnumerateFiles(rootDirectory, "*.png", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            var name = Path.ChangeExtension(Path.GetRelativePath(rootDirectory, file), null);
            var identifier = Identifier.Create(module.Identifier, name);

            var texture = new Image(file);

            storage.Add(identifier, texture);
        }
    }
}
