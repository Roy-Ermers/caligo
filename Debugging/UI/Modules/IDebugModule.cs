namespace WorldGen.Debugging.UI.Modules;

public interface IDebugModule
{
    string Name { get; }
    char? Icon { get; }
    bool Enabled { get; set; }

    void Render();
}
