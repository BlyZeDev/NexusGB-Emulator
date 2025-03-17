namespace NexusGB.GameBoy;

using System.Runtime.CompilerServices;

public static class Bits
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(ref byte value, in int bit) => value &= (byte)~(1 << bit);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(ref ushort value, in int bit) => value &= (ushort)~(1 << bit);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set(ref byte value, in byte bit) => value |= (byte)(1 << bit);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Set(ref ushort value, in byte bit) => value |= (ushort)(1 << bit);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Is(in int value, in int bit) => ((value >> bit) & 1) == 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte MakeByte(params ReadOnlySpan<bool> bits)
    {
        byte result = 0;
        for (int i = 0; i < 8; i++)
        {
            if (bits[i]) result |= (byte)(1 << (7 - i));
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort MakeWord(in byte low, in byte high) => (ushort)((high << 8) | low);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetLowByte(in ushort word) => (byte)(word & 0xFF);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetHighByte(in ushort word) => (byte)(word >> 8);
}