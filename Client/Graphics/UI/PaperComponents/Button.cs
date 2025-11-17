using Prowl.PaperUI;
using Prowl.PaperUI.Events;
using Prowl.PaperUI.LayoutEngine;

namespace Caligo.Client.Graphics.UI.PaperComponents;

public static partial class Components
{

    private static ElementBuilder BaseButton(string text)
    {
        var parent = Paper.Row(text)
        .BackgroundColor(Style.AccentColor)
        .Rounded(Style.FrameRounding)
        .Height(UnitValue.Auto)
        .Width(UnitValue.Auto)
        .Transition(GuiProp.BackgroundColor, 0.1)
        .Hovered.BackgroundColor(Style.AccentHoverColor).End()
        .Active.BackgroundColor(Style.AccentActiveColor).End()
        .TabIndex(0)
        .OnHover(_ => SetCursor(OpenTK.Windowing.Common.Input.MouseCursor.PointingHand));

        using (parent.Enter())
        {
            Paper.Box(text)
            .Text(text, Font)
            .HookToParent()
            .Margin(16, 4)
            .Height(UnitValue.Auto)
            .Width(UnitValue.Auto)
            .FontSize(16);
        }
        return parent;
    }
    public static ElementBuilder Button(string text, Action onClick) => Button(text, _ => onClick());
    public static ElementBuilder Button(string text, Action<ClickEvent> onClick)
    {
        return BaseButton(text).OnClick(onClick);
    }

    public static bool Button(string text)
    {
        var element = BaseButton(text);

        return Paper.IsElementActive(element._handle.Data.ID);
    }
}
