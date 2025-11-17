namespace WorldGen.Graphics.UI.Components;


public interface IComponentContainer : IComponent, IEnumerable<IComponent>
{
    void Add(IComponent component);
}
