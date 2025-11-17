namespace WorldGen.Graphics.UI.PaperComponents.fields;

public static partial class FieldComponents
{
    public static void String(string name, ref string value)
    {
        using var field = Components.Field(name);

        Components.Textbox(ref value);
    }
}
