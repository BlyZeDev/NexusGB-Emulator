namespace NexusGB.Statics;

using System.Text.Json;

public sealed class Json
{
    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    public static T ReadFromFile<T>(string filepath)
    {
        using (var reader = new StreamReader(filepath))
        {
            return JsonSerializer.Deserialize<T>(reader.ReadToEnd(), _options)
                ?? throw new JsonException("The deserialization result was null", filepath, null, null);
        }
    }

    public static T? TryReadFromFile<T>(string filepath)
    {
        try
        {
            using (var reader = new StreamReader(filepath))
            {
                return JsonSerializer.Deserialize<T>(reader.ReadToEnd(), _options)
                    ?? throw new JsonException("The deserialization result was null", filepath, null, null);
            }
        }
        catch (Exception)
        {
            return default;
        }
    }

    public static void WriteToFile<T>(string filepath, T jsonObject)
    {
        using (var writer = new StreamWriter(filepath))
        {
            writer.Write(JsonSerializer.Serialize(jsonObject, _options));
            writer.Flush();
        }
    }
}