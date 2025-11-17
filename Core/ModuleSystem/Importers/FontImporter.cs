using Caligo.Core.FileSystem;

namespace Caligo.Core.ModuleSystem.Importers;

public class FontImporter : IImporter
{
    public void Import(Module module)
    {
        var rootDirectory = Path.Join(module.AbsoluteDirectory, "fonts");

        if (!Directory.Exists(rootDirectory))
            return;

        var files = Directory.EnumerateFiles(rootDirectory, "*.ttf", SearchOption.AllDirectories);
        var fontStorage = module.GetStorage<Font>();

        foreach (var file in files)
        {
            try
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var fontName = Identifier.Create(module.Identifier, name);
                var font = new Font(file);
                fontStorage.Add(fontName, font);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error importing font: {file}");
                Console.WriteLine(e.Message);
            }
        }
    }

}
