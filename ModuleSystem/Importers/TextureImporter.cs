using WorldGen.WorldRenderer;
using WorldGen.FileSystem.Images;
using WorldGen.ModuleSystem.Storage;
using WorldGen.Resources.Atlas;

namespace WorldGen.ModuleSystem.Importers;

public class TextureImporter : IImporter, IResourceProcessor
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

    public void Process(ResourceStorage repository)
    {
        var storage = repository.GetStorage<Image>();
        var atlas = new AtlasBuilder();

        foreach (var image in storage)
            atlas.AddEntry(image.Key, image.Value);

        var atlasImage = atlas.Build();

        var atlasIdentifier = Identifier.Resolve(ChunkMesher.AtlasIdentifier);

        var atlasStorage = repository.GetStorage<Atlas>();

        atlasStorage.Add(atlasIdentifier, atlasImage);
    }
}
