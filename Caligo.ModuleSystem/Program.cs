namespace Caligo.ModuleSystem;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var repository = new ModuleRepository();

        var importer = new ModuleImporter(repository);

        importer.Load("modules");
        return 0;
    }
}