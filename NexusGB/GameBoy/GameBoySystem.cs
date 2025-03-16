namespace NexusGB.GameBoy;

public static class GameBoySystem
{
    public const double ClockFrequency = 4194304;
    public const double RefreshRate = 59.7275;
    public const int CyclesPerUpdate = (int)(ClockFrequency / RefreshRate);
}