using Caligo.ModuleSystem.Storage;

namespace Caligo.ModuleSystem.Importers;

public interface IResourceProcessor
{
    public void Process(ResourceStorage repository);
}