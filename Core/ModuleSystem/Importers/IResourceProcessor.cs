using Caligo.Core.ModuleSystem.Storage;

namespace Caligo.Core.ModuleSystem.Importers;

public interface IResourceProcessor
{
    public void Process(ResourceStorage repository);
}
