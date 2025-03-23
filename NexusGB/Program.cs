namespace NexusGB;

using ConsoleNexusEngine.Helpers;
using NexusGB.Statics;

sealed class Program
{
    static void Main()
    {
        if (!NexusEngineHelper.IsSupportedConsole())
        {
            NexusEngineHelper.StartInSupportedConsole();
            return;
        }

        var rom = RomFileHandler.OpenRomFile(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        if (rom is null)
        {
            Console.Write("Invalid Rom file...");
            Console.ReadKey(true);
            return;
        }

        using (var emulator = new GameBoyEmulator(rom))
        {
            emulator.Start();
        }
    }
}
