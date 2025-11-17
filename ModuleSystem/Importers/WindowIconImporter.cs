using WorldGen.FileSystem.Images;

namespace WorldGen.ModuleSystem.Importers;

public class WindowIconImporter : IImporter
{
    public void Import(Module module)
    {
        if (module.Identifier != Identifier.MainModule)
        {
            return;
        }

        var storage = module.GetStorage<OpenTK.Windowing.Common.Input.WindowIcon>();


        var icon = new Image(Path.Combine(module.AbsoluteDirectory, "window_icon.png"));

        var data = icon.Load();

        var openTKImage = new OpenTK.Windowing.Common.Input.Image(data.Width, data.Height, data.Data);

        storage.Add(Identifier.Create("window_icon"), new OpenTK.Windowing.Common.Input.WindowIcon(openTKImage));
    }
}
