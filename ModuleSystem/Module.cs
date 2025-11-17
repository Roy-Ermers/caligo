using WorldGen.ModuleSystem.Storage;

namespace WorldGen.ModuleSystem;

public class Module : ResourceStorage
{
    public string Identifier { get; }
    public string AbsoluteDirectory { get; }

    /// <summary>
    /// Should be called by the ModulePipeline
    /// </summary>
    /// <param name="identifier">Identifier prefix</param>
    /// <param name="absoluteDirectory">The root directory of this module</param>
    internal Module(string identifier, string absoluteDirectory)
    {
        Identifier = identifier;
        AbsoluteDirectory = absoluteDirectory;
    }
}