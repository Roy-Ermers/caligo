using System.Numerics;
using ImGuiNET;

namespace WorldGen.Graphics.UI.Components.Tables;

public enum TableSizing
{
    None = 0,
    FixedFit = 8192,
    FixedSame = 16384,
    StretchSame = 32768,
}

public class TableComponent : IDisposable, IComponent
{
    private readonly string Id;
    public List<string> Headers { get; set; } = [];
    private readonly List<TableRowComponent> Rows = [];

    private bool rendered = false;

    public TableSizing TableSizing { get; init; } = TableSizing.None;

    public bool EnableVirtualization { get; set; }
    public bool DisableHeaders { get; set; }

    public bool Border { get; set; }

    internal static TableComponent? Current { get; private set; }

    public TableComponent(string Name)
    {
        Id = Name;
        Current = this;
        DisableHeaders = Headers.Count > 0;
    }

    public void AddRow(TableRowComponent component)
    {
        if (Headers.Count == 0)
            Headers = [.. new string[component.ColumnCount].Select((_, i) => $"Column {i + 1}")];

        if (component.ColumnCount != Headers.Count)
            throw new ArgumentException($"Row data length {component.ColumnCount} does not match column length {Headers.Count}");

        Rows.Add(component);
    }

    public void AddRow(params string[] data)
    {
        if (Headers.Count == 0)
            Headers = [.. data.Select((_, i) => $"Column {i + 1}")];

        if (data.Length != Headers.Count)
            throw new ArgumentException($"Row data length {data.Length} does not match column length {Headers.Count}");

        Rows.Add(new(data));
    }

    public void AddHeader(string name)
    {
        Headers.Add(name);
    }

    public void Render(bool force = false)
    {
        if (rendered && !force)
            return;

        rendered = true;

        ImGui.PushID(Id);

        ImGuiTableFlags tableFlags = (ImGuiTableFlags)TableSizing;

        if (Border)
            tableFlags |= ImGuiTableFlags.BordersOuter;

        if (!ImGui.BeginTable(Id, Headers.Count, tableFlags))
            return;

        if (!DisableHeaders) RenderHeaders();
        if (EnableVirtualization)
            RenderVirtualization();
        else
            RenderSimple();

        ImGui.EndTable();
        ImGui.PopID();
    }

    private void RenderHeaders()
    {
        foreach (var header in Headers)
            ImGui.TableSetupColumn(header, ImGuiTableColumnFlags.None, 0.0f, 0);

        ImGui.TableHeadersRow();
    }

    private void RenderSimple()
    {
        foreach (var row in Rows)
        {
            ImGui.TableNextRow();
            row.Draw();
        }
    }

    private void RenderVirtualization()
    {
        unsafe
        {
            ImGuiListClipperPtr listClipper = new(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            listClipper.Begin(Rows.Count);

            while (listClipper.Step())
            {
                for (int i = listClipper.DisplayStart; i < listClipper.DisplayEnd; i++)
                {
                    ImGui.TableNextRow();
                    var row = Rows[i];
                    row.Draw();
                }
            }

            listClipper.End();
        }
    }

    public void Dispose()
    {
        Render();

        GC.SuppressFinalize(this);
        if (Current == this)
            Current = null;
    }
}
