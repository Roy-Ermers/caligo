using System.Runtime.CompilerServices;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace Caligo.Client.Graphics.UI.PaperComponents;

public static partial class Components
{
    private static readonly Dictionary<string, string> TextboxValues = [];

    public static ElementBuilder Textbox(
        ref string text,
        FontFamily fontFamily = FontFamily.Regular,
        char? icon = null,
        string placeholder = "",
        [CallerLineNumber] int intID = 0
    )
    {
        var id = intID + "textbox";

        ElementBuilder element;

        using (InputBox(id, icon))
        {
            element = Paper.Box(intID + "area")
                .HookToParent()
                .Width(UnitValue.StretchOne)
                .Height(UnitValue.StretchOne)
                .IsNotInteractable()
                .FontSize(RootFontSize)
                .TextField(
                    text,
                    new ElementBuilder.TextInputSettings
                    {
                        Font = fontFamily.GetFont(),
                        Placeholder = placeholder,
                        TextColor = Style.TextColor,
                        PlaceholderColor = Style.SecondaryTextColor
                    },
                    value => TextboxValues[id] = value
                );
        }

        if (TextboxValues.TryGetValue(id, out var newVal))
        {
            text = newVal;
            TextboxValues.Remove(id);
        }

        return element;
    }
}