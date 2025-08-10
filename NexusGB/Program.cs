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

        string? romPath;
        while (true)
        {
            Console.Clear();

            romPath = RomFileHandler.OpenRomFile(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            if (romPath is not null && Path.GetExtension(romPath).Equals(".gb", StringComparison.OrdinalIgnoreCase)) break;

            Logger.LogWarning($"Invalid ROM file: {romPath}");

            Console.Write("Invalid Rom file!\nPress Escape to close the emulator or any other key to select another file...");
            var keyInfo = Console.ReadKey(true);

            if (keyInfo.Key is ConsoleKey.Escape) Environment.Exit(0);
        }

        using (var emulator = new GameBoyEmulator(romPath))
        {
            Logger.LogInfo("Emulator has started");

            emulator.Start();

            Logger.LogInfo("Emulator has stopped");
        }

        Logger.Dispose();
    }
}
