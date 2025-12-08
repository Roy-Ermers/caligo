using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Caligo.Core.FileSystem.Json.Converters;

public class JsonVector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected start of array for Vector3.");

        reader.Read();
        if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected number for Vector3.X.");
        var x = reader.GetSingle();

        reader.Read();
        if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected number for Vector3.Y.");
        var y = reader.GetSingle();

        reader.Read();
        if (reader.TokenType != JsonTokenType.Number) throw new JsonException("Expected number for Vector3.Z.");
        var z = reader.GetSingle();

        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray) throw new JsonException("Expected end of array for Vector3.");

        return new Vector3(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }
}