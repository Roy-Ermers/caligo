using System.Drawing;
using Prowl.PaperUI;
using Prowl.PaperUI.LayoutEngine;

namespace WorldGen.Graphics.UI.PaperComponents;



record struct Tab(
string Id,
bool Active
);

public static partial class Components
{

    readonly struct TabsData(string Id, ElementBuilder header) : IDisposable
    {

        public readonly string Id = Id;
        public readonly List<Tab> Tabs = [];
        private readonly ElementBuilder Header = header;
        internal readonly bool Stretch { get; init; }

        private readonly string? ActiveTab
        {
            get => ActiveTabs.TryGetValue(Id, out var value) ? value : null;
            set
            {
                if (value is null)
                    ActiveTabs.Remove(Id);
                else
                {
                    ActiveTabs[Id] = value;
                }
            }
        }

        public bool AddTab(string id)
        {
            if (Tabs.Any(t => t.Id == id))
                return false;

            var active = ActiveTab is not null ? ActiveTab == id : Tabs.Count == 0;
            Tabs.Add(new Tab { Id = id, Active = active });
            return active;
        }

        public void Dispose()
        {
            var self = this;
            using var _ = Header
            .Height(UnitValue.Auto)
            .MinWidth(UnitValue.StretchOne)
            .Width(UnitValue.StretchOne)
            .MaxWidth(UnitValue.Percentage(100))
            .SetScroll(Scroll.ScrollX)
            .Margin(0, 0, 8, 0)
            .RowBetween(4)
            .ChildLeft(8)
            .ChildRight(8)
            .OnHover(_ => SetCursor(OpenTK.Windowing.Common.Input.MouseCursor.PointingHand))
            .Enter();

            foreach (var tab in Tabs)
            {
                using var tabButton = Paper.Row(tab.Id + "_tab")
                    .BackgroundColor(Color.FromArgb(0, Style.FrameBackground))
                    .RoundedTop(Style.FrameRounding)
                    .Transition(GuiProp.BackgroundColor, 0.125)
                    .Hovered.BackgroundColor(Style.FrameBackground).End()
                    .Focused.BackgroundColor(Style.FrameBackground).End()
                    .TabIndex(0)
                    .OnPress(e =>
                    {
                        self.ActiveTab = tab.Id;
                    })
                    .Width(UnitValue.Auto)
                    .Height(UnitValue.Auto)
                    .ChildBottom(4)
                    .ChildTop(4)
                    .ChildBottom(4)
                    .If(Stretch).Width(UnitValue.StretchOne).End()
                    .Enter();

                Paper.Box("selectedIndicator")
                .PositionType(PositionType.SelfDirected)
                .Top(UnitValue.StretchOne)
                .Bottom(0)
                .Left(0)
                .Height(2)
                .Clip()
                .RoundedTop(4)
                .Width(UnitValue.StretchOne)
                .BackgroundColor(Color.Transparent)
                .Transition(GuiProp.BackgroundColor, 0.125)
                .If(tab.Active).BackgroundColor(Style.AccentColor).End();


                Components.Text(tab.Id + "text")
                .Text(tab.Id, Font)
                .Alignment(TextAlignment.Center)
                .Height(UnitValue.Auto)
                .MinWidth(UnitValue.Auto)
                .Width(UnitValue.StretchOne)
                .Margin(8);
            }
        }
    }

    private static Dictionary<string, string> ActiveTabs = [];
    private static TabsData? TabData = null;
    public static IDisposable Tabs(string name, bool stretch = false)
    {
        var header = Paper.Row(name + "_header");
        TabData = new(name, header)
        {
            Stretch = stretch
        };

        Components.Divider();

        return TabData;
    }

    public static bool Tab(string name)
    {
        if (TabData == null)
            throw new InvalidOperationException("Tabs must be called within a Tabs context.");

        var isActive = TabData.Value.AddTab(name);
        return isActive;
    }
}
