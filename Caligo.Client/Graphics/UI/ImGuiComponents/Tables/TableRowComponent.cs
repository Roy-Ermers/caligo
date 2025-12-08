using System.Collections;
using ImGuiNET;

namespace Caligo.Client.Graphics.UI.ImGuiComponents.Tables;

public struct TableRowComponent : IDrawableComponent, IComponentContainer
{
    private IDrawableComponent[] Columns;
    public int ColumnCount => Columns.Length;
    public Guid ID = Guid.NewGuid();

    public TableRowComponent(params string[] data)
    {
        Columns = [.. data.Select(value => new TextComponent(value))];
    }

    public TableRowComponent()
    {
        Columns = [];
    }

    public void Add(IComponent component)
    {
        if (component is not IDrawableComponent drawable)
            throw new ArgumentException("Component must be drawable", nameof(component));

        var columns = Columns.ToList();
        columns.Add(drawable);
        Columns = [.. columns];
    }

    public readonly void Draw()
    {
        ImGui.PushID(ID.ToString());
        foreach (var column in Columns)
            if (ImGui.TableNextColumn())
                column.Draw();
        ImGui.PopID();
    }

    public readonly IEnumerator<IComponent> GetEnumerator()
    {
        return ((IEnumerable<IComponent>)Columns).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enter()
    {
        throw new NotImplementedException();
    }

    public void Exit()
    {
        throw new NotImplementedException();
    }
}