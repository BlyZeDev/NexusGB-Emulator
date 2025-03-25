namespace NexusGB.GameBoy;

public static class GameBoySystem
{
    public const int ScreenWidth = 160;
    public const int ScreenHeight = 144;

    public const double ClockFrequency = 4194304;
    public const double RefreshRate = 59.7275;
    public const int CyclesPerUpdate = (int)(ClockFrequency / RefreshRate);
}