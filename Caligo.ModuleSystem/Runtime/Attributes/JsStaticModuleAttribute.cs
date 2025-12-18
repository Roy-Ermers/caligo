namespace Caligo.ModuleSystem.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public class JsStaticModuleAttribute : Attribute
{
    /// <summary>
    ///     Makes this an auto-registered JS Module with the given name.
    /// </summary>
    /// <param name="name"></param>
    public JsStaticModuleAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    ///     Makes this an auto-registered JS Module.
    /// </summary>
    public JsStaticModuleAttribute()
    {
    }

    public string? Name { get; }
}