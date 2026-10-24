using System.Text.Json;
using System.Text.Json.Serialization;

namespace SmartTravelPlanner.Converters;

public class SingleLineStringListConverter : JsonConverter<List<string>>
{
    public override List<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of array.");
        }

        var list = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return list;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                list.Add(reader.GetString()!);
            }
        }

        throw new JsonException("Unexpected end of JSON array.");
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        string raw = "[" + string.Join(", ", value.Select(v => JsonSerializer.Serialize(v))) + "]";
        writer.WriteRawValue(raw);
    }
}
