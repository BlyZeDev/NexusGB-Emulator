namespace NexusGB.Common;

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

public sealed class FileLog : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly CancellationTokenSource _cts;
    private readonly Task _messageTask;

    private readonly ConcurrentQueue<LogMessage> _messages;

    public FileLog(string path)
    {
        _writer = new StreamWriter(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true
        };
        _cts = new CancellationTokenSource();

        _messages = new ConcurrentQueue<LogMessage>();
        _messageTask = Task.Factory.StartNew(MessageThread, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void Log(LogMessage message) => _messages.Enqueue(message);

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _cts.Cancel();
        _cts.Dispose();

        _messageTask.GetAwaiter().GetResult();

        _writer.Dispose();
        _messageTask.Dispose();
    }

    private async Task MessageThread()
    {
        while (!_cts.IsCancellationRequested)
        {
            if (_messages.TryDequeue(out var message))
            {
                await _writer.WriteLineAsync(FormatMessage(message));
            }
        }

        while (_messages.TryDequeue(out var message))
        {
            await _writer.WriteLineAsync(FormatMessage(message));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string FormatMessage(LogMessage message)
        => $"{message.Timestamp:hh:mm:ss.fff} | {message.LogLevel} | {message.Message}";
}