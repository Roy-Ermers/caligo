using System.Drawing;
using OpenTK.Windowing.Common.Input;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;
using Prowl.Vector;

namespace Caligo.Client.Graphics.UI.PaperComponents;

record struct CurrentSelect(
    string Id,
    string[] Options,
    string? Value,
    Rect Rect,
    bool Visible
);
public static partial class Components
{
    private static CurrentSelect? CurrentSelect = null!;
    public static IDisposable Select(
        string id,
        ref string? value,
        string[] options,
        string placeholder = "",
        char? icon = null
    )
    {
        var inputBox = Components.InputBox(id, icon, MouseCursor.PointingHand);

        var isOpen = CurrentSelect?.Id == id && CurrentSelect?.Visible == true;
        using (inputBox)
        {
            Components.Text(value ?? placeholder)
                .Width(UnitValue.StretchOne)
                .IsNotInteractable()
                .If(value == null)
                    .TextColor(Style.SecondaryTextColor)
                .End();


            Components.Icon(PaperIcon.ArrowDropDown, 16)
                .IsNotInteractable()
                .TransformOrigin(0.5, 0.5)
                .Transition(GuiProp.Rotate, 0.2)
                .If(isOpen).Rotate(180).End();

        }

        var currentValue = value;

        inputBox.OnPress(e =>
        {
            if (CurrentSelect?.Id == id && CurrentSelect?.Visible == true)
                CurrentSelect = CurrentSelect.Value with { Visible = false };
            else
                CurrentSelect = new(
                  id,
                  options,
                  currentValue,
                  Rect.Empty,
                  true
                );
        });

        inputBox.OnPostLayout((handle, rect) =>
        {
            if (CurrentSelect.HasValue && CurrentSelect.Value.Id == id)
            {
                CurrentSelect = CurrentSelect.Value with { Rect = rect };
            }
        });

        if (CurrentSelect.HasValue && CurrentSelect?.Id == id && CurrentSelect.Value.Value != value)
        {
            value = CurrentSelect.Value.Value;
            CurrentSelect = CurrentSelect.Value with { Visible = false };
        }

        return inputBox;
    }

    public static void RenderSelectDropdown()
    {
        if (!CurrentSelect.HasValue)
            return;


        var data = CurrentSelect.Value;

        var elementRect = data.Rect;
        var selectedValue = data.Value;

        using var dropdown = Paper.Column(data.Id + "dropdown")
                    .TabIndex(99)
                    .Width(Math.Max(elementRect.width, 200))
                    .PositionType(PositionType.SelfDirected)
                    .Top(elementRect.Bottom + 4)
                    .Left(elementRect.Left)
                    .Layer(Layer.Overlay)
                    .Rounded(Style.FrameRounding)
                    .SetScroll(Scroll.ScrollY)
                    .RowBetween(4)
                    .MaxHeight(0)
                    .BackgroundColor(Style.FrameBackground)
                    .BoxShadow(0, 0, 0, 0, Color.FromArgb(64, 0, 0, 0))
                    .Transition(GuiProp.BackgroundColor, 0.25)
                    .Transition(GuiProp.MaxHeight, 0.25)
                    .Transition(GuiProp.BoxShadow, 0.25)
                    .Height(UnitValue.Auto)
                    .Border(4)
                    .If(CurrentSelect?.Visible ?? false)
                        .MaxHeight(200)
                        .BackgroundColor(Style.FrameBackground)
                        .BoxShadow(0, 4, 8, 0, Color.FromArgb(64, 0, 0, 0))
                    .End();

        dropdown.Enter();

        foreach (var option in data.Options)
        {
            Components.ListItem(option, selectedValue == option, e =>
            {
                CurrentSelect = data with { Value = option };
            });
        }
    }
}
