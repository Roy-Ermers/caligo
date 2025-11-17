using System.Numerics;
using WorldGen.ModuleSystem.Storage;
using WorldGen.Utils;

using ColorToken = ImGuiNET.ImGuiCol;

namespace WorldGen.Graphics.UI;

public struct UiStyle
{
    public static UiStyle Current { get; private set; }
    public string FontName { get; set; }
    public int FontSize { get; set; }
    public Vector2 ItemMargin { get; set; }
    public Vector2 WindowPadding { get; set; }
    public float WindowRounding { get; set; }
    public int BorderWidth { get; set; }
    public Vector4 WindowBackground { get; set; }
    public int WindowBorderWidth { get; set; }
    public Vector4 TextColor { get; set; }
    public Vector4 SecondaryTextColor { get; set; }

    public Vector4 AccentColor { get; set; }
    public Vector4 AccentHoverColor { get; set; }
    public Vector4 AccentActiveColor { get; set; }

    public Vector2 FramePadding { get; set; }
    public float FrameRounding { get; set; }
    public Vector4 FrameBackground { get; set; }
    public int FrameBorderWidth { get; set; }
    public Vector4 FrameHoverBackground { get; set; }
    public Vector4 FrameActiveBackground { get; set; }
    public Vector4 HeaderBackground { get; set; }
    public Vector4 HeaderHoverBackground { get; set; }
    public Vector4 HeaderActiveBackground { get; set; }

    public Vector4 TitleBackground { get; set; }
    public Vector4 TitleActiveBackground { get; set; }
    public Vector4 TitleCollapsedBackground { get; set; }

    public Vector4 BorderColor { get; set; }
    public Vector4 BorderShadow { get; set; }


    public static UiStyle FromConfig(ResourceTypeStorage<string> config)
    {
        var style = new UiStyle
        {
            FontName = config["ui.font"],
            FontSize = int.TryParse(config["ui.fontSize"], out var size) ? size : 16,
            WindowRounding = float.Parse(config["ui.window.rounding"]),
            WindowPadding = new Vector2(
                float.Parse(config["ui.window.padding.vertical"]),
                float.Parse(config["ui.window.padding.horizontal"])
            ),
            FramePadding = new Vector2(
                float.Parse(config["ui.frame.padding.vertical"]),
                float.Parse(config["ui.frame.padding.horizontal"])
            ),
            ItemMargin = new Vector2(
                float.Parse(config["ui.itemMargin.vertical"]),
                float.Parse(config["ui.itemMargin.horizontal"])
            ),
            WindowBackground = ParseColor(config, "ui.window.background"),
            WindowBorderWidth = int.Parse(config["ui.window.borderWidth"]),
            TextColor = ParseColor(config, "ui.textColor"),
            SecondaryTextColor = ParseColor(config, "ui.secondaryTextColor"),
            TitleBackground = ParseColor(config, "ui.window.titlebar.background"),
            TitleActiveBackground = ParseColor(config, "ui.window.titlebar.activeBackground"),
            TitleCollapsedBackground = ParseColor(config, "ui.window.titlebar.collapsedBackground"),
            BorderColor = ParseColor(config, "ui.border.color"),
            BorderShadow = ParseColor(config, "ui.border.shadow"),
            BorderWidth = int.Parse(config["ui.border.width"]),
            FrameBorderWidth = int.Parse(config["ui.frame.borderWidth"]),
            FrameRounding = int.Parse(config["ui.frame.rounding"]),
            FrameBackground = ParseColor(config, "ui.frame.background"),
            FrameHoverBackground = ParseColor(config, "ui.frame.hoverBackground"),
            FrameActiveBackground = ParseColor(config, "ui.frame.activeBackground"),
            HeaderBackground = ParseColor(config, "ui.header.background"),
            HeaderHoverBackground = ParseColor(config, "ui.header.hoverBackground"),
            HeaderActiveBackground = ParseColor(config, "ui.header.activeBackground"),
            AccentColor = ParseColor(config, "ui.accentColor"),
            AccentHoverColor = ParseColor(config, "ui.accentHoverColor"),
            AccentActiveColor = ParseColor(config, "ui.accentActiveColor"),
        };

        return style;
    }

    private static Vector4 ParseColor(ResourceTypeStorage<string> config, string name)
    {
        var hex = config[name];
        if (HexParser.TryParseHex(hex, out Vector4 color))
            return color;

        throw new InvalidDataException($"{hex} is not a valid color in the config file {name}. Expected format: #RRGGBB or #RRGGBBAA");
    }

    public readonly void Apply(ImGuiNET.ImGuiStylePtr style)
    {
        style.WindowPadding = WindowPadding;
        style.ItemSpacing = ItemMargin;
        style.WindowRounding = WindowRounding;
        style.ScrollbarRounding = WindowRounding;
        style.FramePadding = FramePadding;
        style.FrameRounding = FrameRounding;
        style.ChildRounding = FrameRounding;
        style.ScrollbarSize = 8f;

        style.TabBorderSize = FrameBorderWidth;
        style.WindowBorderSize = WindowBorderWidth;
        style.ChildBorderSize = BorderWidth;
        style.PopupBorderSize = BorderWidth;
        style.FrameBorderSize = FrameBorderWidth;

        style.Colors[(int)ColorToken.Text] = TextColor;
        style.Colors[(int)ColorToken.TextLink] = AccentColor;
        style.Colors[(int)ColorToken.TextDisabled] = SecondaryTextColor;
        style.Colors[(int)ColorToken.WindowBg] = WindowBackground;
        style.Colors[(int)ColorToken.TitleBg] = WindowBackground;
        style.Colors[(int)ColorToken.TitleBgActive] = TitleActiveBackground;
        style.Colors[(int)ColorToken.TitleBgCollapsed] = TitleCollapsedBackground;
        style.Colors[(int)ColorToken.TitleBg] = TitleBackground;
        style.Colors[(int)ColorToken.ChildBg] = WindowBackground;
        style.Colors[(int)ColorToken.PopupBg] = WindowBackground;

        style.Colors[(int)ColorToken.Border] = BorderColor;
        style.Colors[(int)ColorToken.BorderShadow] = BorderShadow;

        style.Colors[(int)ColorToken.FrameBg] = FrameBackground;
        style.Colors[(int)ColorToken.FrameBgHovered] = FrameHoverBackground;
        style.Colors[(int)ColorToken.FrameBgActive] = FrameActiveBackground;

        style.Colors[(int)ColorToken.Header] = HeaderBackground;
        style.Colors[(int)ColorToken.HeaderHovered] = HeaderHoverBackground;
        style.Colors[(int)ColorToken.HeaderActive] = HeaderActiveBackground;

        style.Colors[(int)ColorToken.Button] = AccentColor;
        style.Colors[(int)ColorToken.ButtonHovered] = AccentHoverColor;
        style.Colors[(int)ColorToken.ButtonActive] = AccentActiveColor;
        style.Colors[(int)ColorToken.CheckMark] = AccentColor;
        style.Colors[(int)ColorToken.SliderGrab] = AccentColor;
        style.Colors[(int)ColorToken.SliderGrabActive] = AccentActiveColor;
        style.Colors[(int)ColorToken.Separator] = AccentColor;
        style.Colors[(int)ColorToken.SeparatorHovered] = AccentHoverColor;
        style.Colors[(int)ColorToken.SeparatorActive] = AccentActiveColor;
        style.Colors[(int)ColorToken.ResizeGrip] = AccentColor;
        style.Colors[(int)ColorToken.ResizeGripHovered] = AccentHoverColor;
        style.Colors[(int)ColorToken.ResizeGripActive] = AccentActiveColor;
        style.Colors[(int)ColorToken.Tab] = FrameBackground;
        style.Colors[(int)ColorToken.TabHovered] = AccentActiveColor;
        style.Colors[(int)ColorToken.TabDimmed] = FrameBackground;
        style.Colors[(int)ColorToken.TabSelected] = AccentColor;
        style.Colors[(int)ColorToken.TabSelectedOverline] = Vector4.Zero;
        style.Colors[(int)ColorToken.TabDimmedSelected] = AccentColor;
        style.Colors[(int)ColorToken.TabDimmedSelectedOverline] = Vector4.Zero;
        Current = this;
    }
}
