namespace NexusGB.Statics;

using NexusGB.Common;

public static class Logger
{
    private static readonly string LogFilePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()?.Location) ?? Environment.CurrentDirectory, "Logs.log");

    private static readonly FileLog _logger;

    public static LogLevel LogLevel { get; set; }

    static Logger()
    {
        _logger = new FileLog(LogFilePath);
        LogLevel = LogLevel.Info;
    }

    public static void LogTrace(string message) => Log(LogLevel.Trace, message, null);
    public static void LogDebug(string message) => Log(LogLevel.Debug, message, null);
    public static void LogInfo(string message) => Log(LogLevel.Info, message, null);
    public static void LogWarning(string message, Exception? exception = null) => Log(LogLevel.Warning, message, exception);
    public static void LogError(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);
    public static void LogCritical(string message, Exception? exception = null) => Log(LogLevel.Critical, message, exception);

    public static void Dispose() => _logger.Dispose();

    private static void Log(in LogLevel logLevel, string message, Exception? exception)
    {
        if (logLevel < LogLevel) return;

        _logger.Log(new LogMessage
        {
            LogLevel = logLevel,
            Timestamp = DateTime.Now,
            Message = message,
            Exception = exception
        });
    }
}