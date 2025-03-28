namespace NexusGB.Common;

using ConsoleNexusEngine.Graphics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class NexusColorConverter : JsonConverter<NexusColor>
{
    public override NexusColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var hex = reader.GetString();

        if (hex?.Length != 7 || hex[0] != '#') throw new JsonException("Invalid color format. Expected format: #RRGGBB.");
        if (!int.TryParse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int colorValue)) throw new JsonException("Invalid hex color format.");

        return new NexusColor((byte)((colorValue >> 16) & 0xFF), (byte)((colorValue >> 8) & 0xFF), (byte)(colorValue & 0xFF));
    }

    public override void Write(Utf8JsonWriter writer, NexusColor value, JsonSerializerOptions options)
        => writer.WriteStringValue($"#{value.R:X2}{value.G:X2}{value.B:X2}");
}