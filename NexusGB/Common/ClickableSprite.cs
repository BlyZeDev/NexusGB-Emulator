namespace NexusGB.Common;

using ConsoleNexusEngine.Graphics;
using ConsoleNexusEngine;

public readonly struct ClickableSprite : INexusSprite
{
    public readonly NexusSpriteMap Map { get; }

    public readonly NexusCoord StartPos { get; }

    public ClickableSprite(in NexusSpriteMap map, in NexusCoord startPos)
    {
        StartPos = startPos;
        Map = map;
    }

    public bool IsHoveredOver(in NexusCoord mousePos) => mousePos.IsInRange(StartPos, Map.Size);
}