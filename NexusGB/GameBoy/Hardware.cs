namespace NexusGB.GameBoy;

public static class Hardware
{
    public const int ClockFrequency = 4194304;
    public const float REFRESH_RATE = 59.7275f;
    public const int CYCLES_PER_UPDATE = (int)(ClockFrequency / REFRESH_RATE);
}