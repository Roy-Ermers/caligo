using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Caligo.Core.FileSystem.Json.Converters;

public class JsonVector4Converter : JsonConverter<Vector4>
{
    public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of array for Vector4.");
        }

        reader.Read();
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException("Expected number for Vector4.X.");
        }
        float x = reader.GetSingle();

        reader.Read();
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException("Expected number for Vector4.Y.");
        }
        float y = reader.GetSingle();

        reader.Read();
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException("Expected number for Vector4.Z.");
        }
        float z = reader.GetSingle();

        reader.Read();
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException("Expected number for Vector4.Z.");
        }
        float w = reader.GetSingle();

        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Expected end of array for Vector4.");
        }

        return new Vector4(x, y, z, w);
    }

    public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteNumberValue(value.W);
        writer.WriteEndArray();
    }
}
