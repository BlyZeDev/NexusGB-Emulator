namespace NexusGB;

using ConsoleNexusEngine.Helpers;
using NexusGB.Statics;

sealed class Program
{
    static void Main()
    {
        if (!NexusEngineHelper.IsSupportedConsole())
        {
            Logger.LogInfo("Wrong console trying to restart in compatible console");

            NexusEngineHelper.StartInSupportedConsole();
            return;
        }

        var romPath = RomFileHandler.OpenRomFile(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        if (romPath is null || Path.GetExtension(romPath) != ".gb")
        {
            Logger.LogWarning($"Invalid ROM file: {romPath}");

            Console.Write("Invalid Rom file...");
            Console.ReadKey(true);
            return;
        }

        using (var emulator = new GameBoyEmulator(romPath, Path.ChangeExtension(romPath, ".sav")))
        {
            Logger.LogInfo("Emulator has started");

            emulator.Start();
        }

        Logger.Dispose();
    }
}
