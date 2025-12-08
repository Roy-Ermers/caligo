using OpenTK.Mathematics;

namespace Caligo.Client.Graphics.UI.PaperComponents.fields;

public static partial class FieldComponents
{
    public static void Object(string name, ref object value)
    {
        using var field = Components.Field(name);

        switch (value)
        {
            case float f:
                Float(name, ref f);
                value = f;
                break;
            case string s:
                String(name, ref s);
                value = s;
                break;
            case Vector3 v3:
                Vector3(name, ref v3);
                value = v3;
                break;
            case Vector2 v2:
                Vector2(name, ref v2);
                value = v2;
                break;
            default:
                Components.Text($"Unsupported type: {value.GetType().Name}");
                break;
        }
    }
}