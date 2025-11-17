using System.Data;

namespace WorldGen.Renderer.UI.Components.Tables;

public readonly struct InfoTableComponent : IComponent, IDisposable
{
    private readonly TableComponent table;

    public InfoTableComponent(string id)
    {
        table = new TableComponent(id, 2)
        {
            TableSizing = TableSizing.StretchSame
        };
    }


    public void Set(IEnumerable<KeyValuePair<string, string>> values)
    {
        foreach (var (key, value) in values)
            Add(key, value);
    }

    public void Add(string key, int value) => Add(key, value.ToString());
    public void Add(string key, float value) => Add(key, value.ToString("0.##"));
    public void Add(string key, double value) => Add(key, value.ToString("0"));
    public void Add(string key, bool value) => Add(key, value ? "Yes" : "No");
    public void Add(string key, object? value) => Add(key, value?.ToString() ?? "null");
    public void Add(string key, string value)
    {
        table.AddRow([
            new TextComponent(key).WithStyle(TextStyle.Secondary),
            new TextComponent(value).WithWrapping()
        ]);
    }

    public void Dispose()
    {
        table.Dispose();
    }
}
