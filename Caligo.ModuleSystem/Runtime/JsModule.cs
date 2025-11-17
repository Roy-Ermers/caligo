using Jint;

namespace Caligo.ModuleSystem.Runtime;

public class JsModule : Module
{
    private Engine _moduleEngine;
    
    internal JsModule(string identifier, string absoluteDirectory) : base(identifier, absoluteDirectory)
    {
        _moduleEngine = JsEngine.CreateEngine(identifier, absoluteDirectory);
        
        var moduleEntry = Path.Combine(absoluteDirectory, "module.js");
        
        _moduleEngine.Modules.Add(identifier, File.ReadAllText(moduleEntry));
        
        var result = _moduleEngine.Modules.Import(identifier);
    }
}