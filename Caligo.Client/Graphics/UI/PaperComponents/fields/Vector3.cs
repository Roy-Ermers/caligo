using OpenTK.Mathematics;

namespace Caligo.Client.Graphics.UI.PaperComponents.fields;

public static partial class FieldComponents
{
    public static void Vector3(string name, ref Vector3 value)
    {
        using var field = Components.Field(name);

        using var _ = Components.Row();

        Components.NumberInput(ref value.X, FontFamily.Monospace, placeholder: "X", intID: 0);
        Components.NumberInput(ref value.Y, FontFamily.Monospace, placeholder: "Y", intID: 1);
        Components.NumberInput(ref value.Z, FontFamily.Monospace, placeholder: "Z", intID: 2);
    }
}