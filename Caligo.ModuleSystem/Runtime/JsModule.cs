namespace Caligo.ModuleSystem.Runtime;

/// <summary>
/// A module that executes JavaScript code and can define blocks programmatically.
/// </summary>
public class JsModule : Module
{
    public static JsModule CurrentModule { private set; get; }

    internal JsModule(string identifier, string absoluteDirectory) : base(identifier, absoluteDirectory)
    {
        CurrentModule = this;
        var moduleEngine = JsEngine.CreateEngine(identifier, absoluteDirectory);

        var moduleEntry = Path.Combine(absoluteDirectory, "module.js");

        moduleEngine.Modules.Add(identifier, File.ReadAllText(moduleEntry));

        moduleEngine.Modules.Import(identifier);
    }
}