namespace Caligo.ModuleSystem.Runtime;

public class NamespacedConsole
{
    private readonly string _identifier;
    public NamespacedConsole(string identifier)
    {
        _identifier = identifier;
    }
    
    public void Log(params object[] args)
    {
        Console.WriteLine($"[{_identifier}] {string.Join(" ", args)}");
    }
    
    public void Warn(params object[] args)
    {
        Console.WriteLine($"[{_identifier}][WARN] {string.Join(" ", args)}");
    }
    
    public void Error(params object[] args)
    {
        Console.WriteLine($"[{_identifier}][ERROR] {string.Join(" ", args)}");
    }
}