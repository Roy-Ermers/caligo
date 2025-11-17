using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorldGen.FileSystem.Json.Converters;

public class JsonVector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of array for Vector2.");
        }

        reader.Read();
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException("Expected number for Vector2.X.");
        }
        float x = reader.GetSingle();

        reader.Read();
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException("Expected number for Vector2.Y.");
        }
        float y = reader.GetSingle();

        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException("Expected end of array for Vector2.");
        }

        return new Vector2(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}
