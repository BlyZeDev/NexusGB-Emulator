namespace NexusGB;

using ConsoleNexusEngine.Helpers;

sealed class Program
{
    static void Main()
    {
        if (!NexusEngineHelper.IsSupportedConsole())
        {
            NexusEngineHelper.StartInSupportedConsole();
            return;
        }

        using (var emulator = new GameBoyEmulator(@"C:\Users\leons\Downloads\Tetris.gb"))
        {
            emulator.Start();
            emulator.Stop();
        }
    }
}
