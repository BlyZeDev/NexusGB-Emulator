namespace NexusGB.GameBoy;

public static class GameBoy
{
    public const double ClockFrequency = 4194304;
    public const double REFRESH_RATE = 59.7275f;
    public const int CYCLES_PER_UPDATE = (int)(ClockFrequency / REFRESH_RATE);
}