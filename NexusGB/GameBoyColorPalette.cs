﻿namespace NexusGB;

using ConsoleNexusEngine.Graphics;
using System.Collections.Immutable;

[IgnoreColorPalette]
public sealed record GameBoyColorPalette : NexusColorPalette
{
    public GameBoyColorPalette(in NexusColor color1, in NexusColor color2, in NexusColor color3, in NexusColor color4, in NexusColor backgroundColor)
        : base(GetPalette(color1, color2, color3, color4, backgroundColor)) { }

    private static ImmutableArray<NexusColor> GetPalette(in NexusColor color1, in NexusColor color2, in NexusColor color3, in NexusColor color4, in NexusColor backgroundColor)
    {
        var builder = ImmutableArray.CreateBuilder<NexusColor>(MaxColorCount);

        builder.Add(color1);
        builder.Add(color2);
        builder.Add(color3);
        builder.Add(color4);

        for (int i = 0; i < MaxColorCount - 5; i++)
        {
            builder.Add(color1);
        }

        builder.Add(backgroundColor);

        return builder.MoveToImmutable();
    }
}