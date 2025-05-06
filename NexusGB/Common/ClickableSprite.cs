namespace NexusGB.Common;

using ConsoleNexusEngine;
using ConsoleNexusEngine.Graphics;

public sealed class ClickableSprite : INexusSprite, IDisposable
{
    private readonly NexusConsoleInput _input;
    private readonly CancellationTokenSource _cts;

    public NexusSpriteMap Map { get; }

    public NexusCoord StartPos { get; }

    public event EventHandler? MouseOver;

    public ClickableSprite(NexusConsoleInput input, in NexusSpriteMap map, in NexusCoord startPos)
    {
        _cts = new CancellationTokenSource();
        _input = input;
        StartPos = startPos;

        Map = map;

        Task.Factory.StartNew(() =>
        {
            while (!_cts.IsCancellationRequested)
            {
                _input.Update();

                if (_input.MousePosition.IsInRange(StartPos, Map.Size)) MouseOver?.Invoke(this, EventArgs.Empty);
            }
        }, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}