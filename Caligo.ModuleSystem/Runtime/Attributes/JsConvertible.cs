namespace Caligo.ModuleSystem.Runtime.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class JsConvertibleAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class JsPropertyAttribute : Attribute
{
    public string Name { get; }
    public JsPropertyAttribute(string name) => Name = name;
}