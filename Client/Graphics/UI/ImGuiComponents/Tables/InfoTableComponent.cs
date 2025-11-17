namespace Caligo.Client.Graphics.UI.ImGuiComponents.Tables;

public readonly struct InfoTableComponent : IComponent, IDisposable
{
    private readonly TableComponent table;

    public InfoTableComponent(string id)
    {
        table = new TableComponent(id)
        {
            TableSizing = TableSizing.None,
            DisableHeaders = true
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
            new TextComponent(value)
        ]);
    }

    public void Add(string key, IComponent component)
    {
        table.AddRow([
          new TextComponent(key).WithStyle(TextStyle.Secondary),
          component
        ]);
    }

    public void Dispose()
    {
        table.Dispose();
    }
}
