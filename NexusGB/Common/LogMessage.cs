namespace NexusGB.Common;

public sealed record LogMessage
{
    public required LogLevel LogLevel { get; init; }
    public required DateTime Timestamp { get; init; }
    public required string Message { get; init; }
    public required Exception? Exception { get; init; }
}