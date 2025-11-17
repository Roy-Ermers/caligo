namespace WorldGen.Graphics.UI.ImGuiComponents;


public interface IComponentContainer : IComponent, IEnumerable<IComponent>
{
    void Add(IComponent component);
}
