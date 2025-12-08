using System.Runtime.CompilerServices;
using OpenTK.Windowing.Common.Input;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace Caligo.Client.Graphics.UI.PaperComponents;

public static partial class Components
{
    private static readonly Dictionary<string, bool> CheckboxValues = [];

    public static ElementBuilder Checkbox(
        ref bool value,
        string label = "",
        [CallerLineNumber] int intID = 0
    )
    {
        var id = intID + "checkbox";

        ElementBuilder element;

        if (CheckboxValues.TryGetValue(id, out var newVal))
        {
            value = newVal;
            CheckboxValues.Remove(id);
        }

        if (!string.IsNullOrEmpty(label))
        {
            var _value = value;

            using var row = Paper.Row(id + "row")
                .RowBetween(16)
                .Height(UnitValue.Auto)
                .OnClick(_ => { CheckboxValues[id] = !_value; })
                .Width(UnitValue.StretchOne)
                .Enter();

            element = renderCheckbox(id, value);
            Text(label);
            return element;
        }

        element = renderCheckbox(id, value);

        return element;
    }

    private static ElementBuilder renderCheckbox(string id, bool value)
    {
        var element = Paper.Box(id)
            .Width(16)
            .Height(16)
            .Rounded(4)
            .BorderWidth(1)
            .BorderColor(Style.BorderColor)
            .OnHover(_ => SetCursor(MouseCursor.PointingHand))
            .Transition(GuiProp.BackgroundColor, 0.1)
            .Transition(GuiProp.BorderColor, 0.1)
            .If(value)
            .BackgroundColor(Style.AccentColor)
            .BorderColor(Style.AccentColor)
            .End()
            .OnClick(_ => { CheckboxValues[id] = !value; });

        var disposable = element.Enter();
        if (value)
            Icon(PaperIcon.Check, 18);
        disposable.Dispose();

        return element;
    }
}