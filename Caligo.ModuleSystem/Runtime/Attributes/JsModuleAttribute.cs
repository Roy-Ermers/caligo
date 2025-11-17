namespace Caligo.ModuleSystem.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public class JsModuleAttribute : Attribute
{
    public string? Name { get; }
    /// <summary>
    /// Makes this an auto-registered JS Module with the given name.
    /// </summary>
    /// <param name="name"></param>
    public JsModuleAttribute(string name)
    {
        Name = name;
    }    
    
    /// <summary>
    /// Makes this an auto-registered JS Module.
    /// </summary>
    public JsModuleAttribute()
    { }
}