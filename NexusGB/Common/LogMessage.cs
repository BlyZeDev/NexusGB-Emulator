namespace NexusGB.Common;

public sealed record LogMessage
{
    public LogLevel LogLevel { get; }
    public TimeSpan Timestamp { get; }
    public string Message { get; }
    public Exception? Exception { get; }

    public LogMessage(in LogLevel logLevel, in TimeSpan timestamp, string message, Exception? exception)
    {
        LogLevel = logLevel;
        Timestamp = timestamp;
        Message = message;
        Exception = exception;
    }
}