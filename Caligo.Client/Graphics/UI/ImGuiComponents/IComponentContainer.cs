namespace Caligo.Client.Graphics.UI.ImGuiComponents;


public interface IComponentContainer : IComponent, IEnumerable<IComponent>
{
    void Add(IComponent component);
}
