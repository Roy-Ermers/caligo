using System.Text.Json;

namespace Caligo.Core.FileSystem.Json;

public class JsonPolicy : JsonNamingPolicy
{
    public override string ConvertName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}