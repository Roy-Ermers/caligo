using System.Text.Json;
using System.Text.Json.Serialization;
using Caligo.Core.FileSystem.Json.Converters;

namespace Caligo.Core.FileSystem.Json;

public class JsonOptions
{
    public static JsonSerializerOptions SerializerOptions => new()
    {
        PropertyNameCaseInsensitive = true,
        RespectNullableAnnotations = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNamingPolicy = new JsonPolicy(),
        IncludeFields = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IgnoreReadOnlyProperties = true,
        Converters =
        {
            new JsonVector3Converter(),
            new JsonVector4Converter(),
            new JsonVector2Converter()
        }
    };
}