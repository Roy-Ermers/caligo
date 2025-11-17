using System.Numerics;
using ImGuiNET;

namespace WorldGen.Renderer.UI.Components.Tables;

public enum TableSizing
{
    None = 0,
    FixedFit = 8192,
    FixedSame = 16384,
    StretchSame = 32768
}

public class TableComponent : IDisposable, IComponent
{
    private readonly string Id;
    public int ColumnCount { get; private set; }
    private readonly List<TableRowComponent> Rows = [];

    private bool rendered = false;

    public TableSizing TableSizing { get; init; } = TableSizing.None;

    internal static TableComponent? Current { get; private set; }

    public TableComponent(string Name, int columnCount)
    {
        Id = Name;
        Current = this;
        ColumnCount = columnCount;
    }

    public void AddRow(TableRowComponent component)
    {
        Rows.Add(component);
    }

    public void AddRow(params string[] data)
    {
        if (data.Length != ColumnCount)
            throw new ArgumentException($"Row data length {data.Length} does not match column length {ColumnCount}");

        Rows.Add(new(data));
    }

    public void Render(bool force = false)
    {
        if (rendered && !force)
            return;

        rendered = true;

        ImGui.BeginTable(Id, ColumnCount, (ImGuiTableFlags)TableSizing);
        foreach (var row in Rows)
        {
            ImGui.TableNextRow();
            row.Draw();
        }
        ImGui.EndTable();
    }

    public void Dispose()
    {
        Render();

        GC.SuppressFinalize(this);
        if (Current == this)
            Current = null;
    }
}
