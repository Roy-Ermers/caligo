using System.Text.Json;
using WorldGen.FileSystem.Json.Converters;

namespace WorldGen.FileSystem.Json;

public class JsonOptions
{
    public static JsonSerializerOptions SerializerOptions => new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNamingPolicy = new JsonPolicy(),
        IncludeFields = true,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        IgnoreReadOnlyProperties = true,
        Converters = {
            new JsonVector3Converter(),
            new JsonVector4Converter(),
            new JsonVector2Converter(),
        }
    };
}
