namespace NexusGB.Common;

using ConsoleNexusEngine.Graphics;
using System.Text.Json.Serialization;

[JsonSerializable(typeof(EmulatorConfig), GenerationMode = JsonSourceGenerationMode.Default, TypeInfoPropertyName = nameof(EmulatorConfig))]
public sealed record EmulatorConfig
{
    [JsonInclude, JsonRequired, JsonConverter(typeof(NexusColorConverter))]
    public required NexusColor Color1 { get; init; }

    [JsonInclude, JsonRequired, JsonConverter(typeof(NexusColorConverter))]
    public required NexusColor Color2 { get; init; }

    [JsonInclude, JsonRequired, JsonConverter(typeof(NexusColorConverter))]
    public required NexusColor Color3 { get; init; }

    [JsonInclude, JsonRequired, JsonConverter(typeof(NexusColorConverter))]
    public required NexusColor Color4 { get; init; }

    [JsonConstructor]
    public EmulatorConfig(NexusColor color1, NexusColor color2, NexusColor color3, NexusColor color4)
    {
        Color1 = color1;
        Color2 = color2;
        Color3 = color3;
        Color4 = color4;
    }

    public EmulatorConfig() { }
}