using System.Drawing;
using System.Numerics;
using Caligo.Core.Utils;
using Caligo.ModuleSystem.Storage;
using ColorToken = ImGuiNET.ImGuiCol;

namespace Caligo.Client.Graphics.UI;

public struct UiStyle
{
    public static UiStyle Current { get; private set; }
    public string FontName { get; set; }
    public int FontSize { get; set; }
    public Vector2 ItemMargin { get; set; }
    public Vector2 WindowPadding { get; set; }
    public float WindowRounding { get; set; }
    public int BorderWidth { get; set; }
    public Color WindowBackground { get; set; }
    public int WindowBorderWidth { get; set; }
    public Color TextColor { get; set; }
    public Color SecondaryTextColor { get; set; }

    public Color AccentColor { get; set; }
    public Color AccentHoverColor { get; set; }
    public Color AccentActiveColor { get; set; }

    public Vector2 FramePadding { get; set; }
    public float FrameRounding { get; set; }
    public Color FrameBackground { get; set; }
    public int FrameBorderWidth { get; set; }
    public Color FrameHoverBackground { get; set; }
    public Color FrameActiveBackground { get; set; }
    public Color HeaderBackground { get; set; }
    public Color HeaderHoverBackground { get; set; }
    public Color HeaderActiveBackground { get; set; }

    public Color TitleBackground { get; set; }
    public Color TitleActiveBackground { get; set; }
    public Color TitleCollapsedBackground { get; set; }

    public Color BorderColor { get; set; }
    public Color BorderShadow { get; set; }


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

    private static Color ParseColor(ResourceTypeStorage<string> config, string name)
    {
        var hex = config[name];
        if (HexParser.TryParseHex(hex, out Vector4 color))
            return Color.FromArgb((int)color.W, (int)color.X, (int)color.Y, (int)color.Z);

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
        style.Colors[(int)ColorToken.Text] = TextColor.ToVector4();
        style.Colors[(int)ColorToken.TextLink] = AccentColor.ToVector4();
        style.Colors[(int)ColorToken.TextDisabled] = SecondaryTextColor.ToVector4();
        style.Colors[(int)ColorToken.WindowBg] = WindowBackground.ToVector4();
        style.Colors[(int)ColorToken.TitleBg] = WindowBackground.ToVector4();
        style.Colors[(int)ColorToken.TitleBgActive] = TitleActiveBackground.ToVector4();
        style.Colors[(int)ColorToken.TitleBgCollapsed] = TitleCollapsedBackground.ToVector4();
        style.Colors[(int)ColorToken.TitleBg] = TitleBackground.ToVector4();
        style.Colors[(int)ColorToken.ChildBg] = WindowBackground.ToVector4();
        style.Colors[(int)ColorToken.PopupBg] = WindowBackground.ToVector4();

        style.Colors[(int)ColorToken.Border] = BorderColor.ToVector4();
        style.Colors[(int)ColorToken.BorderShadow] = BorderShadow.ToVector4();

        style.Colors[(int)ColorToken.FrameBg] = FrameBackground.ToVector4();
        style.Colors[(int)ColorToken.FrameBgHovered] = FrameHoverBackground.ToVector4();
        style.Colors[(int)ColorToken.FrameBgActive] = FrameActiveBackground.ToVector4();

        style.Colors[(int)ColorToken.Header] = HeaderBackground.ToVector4();
        style.Colors[(int)ColorToken.HeaderHovered] = HeaderHoverBackground.ToVector4();
        style.Colors[(int)ColorToken.HeaderActive] = HeaderActiveBackground.ToVector4();

        style.Colors[(int)ColorToken.Button] = AccentColor.ToVector4();
        style.Colors[(int)ColorToken.ButtonHovered] = AccentHoverColor.ToVector4();
        style.Colors[(int)ColorToken.ButtonActive] = AccentActiveColor.ToVector4();
        style.Colors[(int)ColorToken.CheckMark] = AccentColor.ToVector4();
        style.Colors[(int)ColorToken.SliderGrab] = AccentColor.ToVector4();
        style.Colors[(int)ColorToken.SliderGrabActive] = AccentActiveColor.ToVector4();
        style.Colors[(int)ColorToken.Separator] = AccentColor.ToVector4();
        style.Colors[(int)ColorToken.SeparatorHovered] = AccentHoverColor.ToVector4();
        style.Colors[(int)ColorToken.SeparatorActive] = AccentActiveColor.ToVector4();
        style.Colors[(int)ColorToken.ResizeGrip] = AccentColor.ToVector4();
        style.Colors[(int)ColorToken.ResizeGripHovered] = AccentHoverColor.ToVector4();
        style.Colors[(int)ColorToken.ResizeGripActive] = AccentActiveColor.ToVector4();
        style.Colors[(int)ColorToken.Tab] = FrameBackground.ToVector4();
        style.Colors[(int)ColorToken.TabHovered] = AccentActiveColor.ToVector4();
        style.Colors[(int)ColorToken.TabDimmed] = FrameBackground.ToVector4();
        style.Colors[(int)ColorToken.TabSelected] = AccentColor.ToVector4();
        style.Colors[(int)ColorToken.TabSelectedOverline] = Vector4.Zero;
        style.Colors[(int)ColorToken.TabDimmedSelected] = AccentColor.ToVector4();
        style.Colors[(int)ColorToken.TabDimmedSelectedOverline] = Vector4.Zero;
        style.Colors[(int)ColorToken.ModalWindowDimBg] = new Vector4(0, 0, 0, 0.5f);
        Current = this;
    }
}
