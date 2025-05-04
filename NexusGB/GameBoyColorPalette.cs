namespace NexusGB;

using ConsoleNexusEngine.Graphics;
using System.Collections.Immutable;

public sealed record GameBoyColorPalette : NexusColorPalette
{
    public GameBoyColorPalette(in NexusColor color1, in NexusColor color2, in NexusColor color3, in NexusColor color4, in NexusColor backgroundColor)
    {
        var builder = ImmutableArray.CreateBuilder<NexusColor>(MaxColorCount);

        builder.Add(color1);
        builder.Add(color2);
        builder.Add(color3);
        builder.Add(color4);

        for (int i = 0; i < MaxColorCount - 6; i++)
        {
            builder.Add(color1);
        }

        builder.Add(new NexusColor((byte)(0xFF - backgroundColor.R), (byte)(0xFF - backgroundColor.G), (byte)(0xFF - backgroundColor.B)));
        builder.Add(backgroundColor);

        Colors = builder.MoveToImmutable();
    }
}