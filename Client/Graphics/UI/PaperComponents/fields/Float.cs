namespace Caligo.Client.Graphics.UI.PaperComponents.fields;

public static partial class FieldComponents
{
    public static void Object(string name, ref float value) => Float(name, ref value);
    public static void Float(string name, ref float value)
    {
        using var field = Components.Field(name);

        Components.NumberInput(ref value, FontFamily.Monospace, placeholder: name);
    }
}
