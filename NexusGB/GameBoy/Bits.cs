namespace NexusGB.GameBoy;

public static class Bits
{
    public static byte Clear(ref byte value, in int bit) => value &= (byte)~(1 << bit);
    public static byte Set(ref byte value, in byte bit) => value |= (byte)(1 << bit);
    public static bool Is(in int value, in int bit) => ((value >> bit) & 1) == 1;
}