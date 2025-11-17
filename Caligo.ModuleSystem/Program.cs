namespace Caligo.ModuleSystem;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        ModuleRepository repository = new ModuleRepository();

        ModuleImporter importer = new ModuleImporter(repository);
        
        importer.Load("modules");
        return 0;
    }
}