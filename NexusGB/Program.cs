﻿namespace NexusGB;

using ConsoleNexusEngine.Helpers;

sealed class Program
{
    static void Main()
    {
        //To be removed
        const string TestRomsBasePath = @"C:\Users\leons\OneDrive\Desktop\!Programmierung\!Meine Programme\C#\ConsoleApps\NexusGB\NexusGB.Tests";
        const string PlayRomsBasePath = @"C:\Users\leons\Downloads";

        if (!NexusEngineHelper.IsSupportedConsole())
        {
            NexusEngineHelper.StartInSupportedConsole();
            return;
        }

        using (var emulator = new GameBoyEmulator(Path.Combine(PlayRomsBasePath, "Tetris.gb")))
        {
            emulator.Start();
            emulator.Stop();
        }
    }
}
