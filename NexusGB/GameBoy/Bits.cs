namespace NexusGB.GameBoy;

using System.Runtime.CompilerServices;

public static class Bits
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(ref byte value, in int bit) => value &= (byte)~(1 << bit);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set(ref byte value, in byte bit) => value |= (byte)(1 << bit);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Is(in int value, in int bit) => ((value >> bit) & 1) == 1;
}