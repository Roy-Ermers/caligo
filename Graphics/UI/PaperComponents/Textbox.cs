using System.Runtime.CompilerServices;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;

public static partial class Components
{
    private static Dictionary<string, string> TextboxValues = [];
    public static ElementBuilder Textbox(
        ref string text,
        FontFamily fontFamily = FontFamily.Regular,
        char? icon = null,
        string placeholder = "",
        [CallerLineNumber] int intID = 0
    )
    {
        var id = intID + "textbox";
        var currentValue = TextboxValues.TryGetValue(id, out var val) ? val : text;
        var box = Paper.Row(id)
        .Width(UnitValue.StretchOne)
        .MinHeight(32)
        .Height(UnitValue.Auto)
        .Rounded(Style.FrameRounding)
        .BackgroundColor(Style.TitleCollapsedBackground)
        .BorderWidth(1)
        .BorderColor(Style.FrameBackground)
        .TabIndex(0)
        .ChildTop(8)
        .ChildBottom(8)
        .ChildLeft(8)
        .ChildRight(8)
        .RowBetween(4)
        .RowBetween(4)
        .OnHover(_ => SetCursor(OpenTK.Windowing.Common.Input.MouseCursor.IBeam))
        .Transition(GuiProp.BackgroundColor, 0.1)
        .Transition(GuiProp.BorderColor, 0.1)
        .BorderWidth(1)
        .Focused
         .BackgroundColor(Style.FrameHoverBackground)
         .BorderColor(Style.AccentColor)
        .End();

        using (box.Enter())
        {
            if (icon is not null)
            {
                var iconComponent = Components.Icon(icon.Value, 16).TextColor(Style.TextColor);
                if (string.IsNullOrWhiteSpace(currentValue))
                {
                    iconComponent.TextColor(Style.SecondaryTextColor);
                }
            }

            var element = Paper.Box(intID + "area")
            .HookToParent()
            .Width(UnitValue.StretchOne)
            .Height(UnitValue.StretchOne)
            .IsNotInteractable()
            .FontSize(RootFontSize)
            .TextField(
                currentValue,
                new ElementBuilder.TextInputSettings()
                {
                    Font = fontFamily.GetFont(),
                    Placeholder = placeholder,
                    TextColor = Style.TextColor,
                    PlaceholderColor = Style.SecondaryTextColor
                },
                value => TextboxValues[id] = value
            );
        }

        text = TextboxValues.TryGetValue(id, out var newVal) ? newVal : text;

        return box;
    }
}
