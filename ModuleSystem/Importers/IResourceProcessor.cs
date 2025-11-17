using WorldGen.ModuleSystem.Storage;

namespace WorldGen.ModuleSystem.Importers;

public interface IResourceProcessor
{
    public void Process(ResourceStorage repository);
}
