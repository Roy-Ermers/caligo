using System.Numerics;
using System.Runtime.CompilerServices;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    private static readonly Dictionary<string, float> NumberValues = [];
    public static void NumberInput(
        ref float value,
        FontFamily fontFamily = FontFamily.Regular,
        char? icon = null,
        string placeholder = "",
        [CallerLineNumber] int intID = 0
    )
    {
        var id = intID + "numberInput";

        using var _ = Components.InputBox(id, icon);

        var element = Paper.Box(intID + "area")
        .HookToParent()
        .Width(UnitValue.StretchOne)
        .Height(UnitValue.StretchOne)
        .IsNotInteractable()
        .FontSize(RootFontSize)
        .TextField(
            value.ToString("0.##"),
            new ElementBuilder.TextInputSettings()
            {
                Font = fontFamily.GetFont(),
                Placeholder = placeholder,
                TextColor = Style.TextColor,
                PlaceholderColor = Style.SecondaryTextColor,
            },
            value =>
            {
                if (float.TryParse(value, out var floatValue))
                    NumberValues[id] = floatValue;
            }
        );


        if (NumberValues.TryGetValue(id, out var newValue))
        {
            value = newValue;
            NumberValues.Remove(id);
        }
    }
}
