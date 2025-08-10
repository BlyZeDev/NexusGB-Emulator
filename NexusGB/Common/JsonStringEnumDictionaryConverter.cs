namespace NexusGB.Common;

using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class JsonStringEnumDictionaryConverter<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
    where TKey : struct, Enum
    where TValue : struct, Enum
{
    public override Dictionary<TKey, TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is not JsonTokenType.StartObject) throw new JsonException();

        var dictionary = new Dictionary<TKey, TValue>();
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndObject) return dictionary;

            if (!Enum.TryParse<TKey>(reader.GetString(), out var key)) throw new JsonException();

            reader.Read();

            if (!Enum.TryParse<TValue>(reader.GetString(), out var value)) throw new JsonException();

            dictionary[key] = value;
        }

        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<TKey, TValue> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var pair in value)
        {
            writer.WritePropertyName(pair.Key.ToString());
            writer.WriteStringValue(pair.Value.ToString());
        }

        writer.WriteEndObject();
    }
}