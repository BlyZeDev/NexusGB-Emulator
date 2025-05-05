namespace NexusGB.Common;

using ConsoleNexusEngine;
using ConsoleNexusEngine.Graphics;

public sealed class ClickableSprite : INexusSprite
{
    private readonly NexusConsoleInput _input;
    private readonly NexusCoord _startPos;
    private readonly NexusSize _range;

    public NexusSpriteMap Map { get; }

    public event EventHandler? MouseOver;

    public ClickableSprite(NexusConsoleInput input, in NexusSpriteMap map, in NexusCoord startPos, in NexusSize range)
    {
        _input = input;
        _startPos = startPos;
        _range = range;

        Map = map;
    }

    public void Update()
    {
        if (!_input.MousePosition.IsInRange(_startPos, _range)) return;

        MouseOver?.Invoke(this, EventArgs.Empty);
    }
}