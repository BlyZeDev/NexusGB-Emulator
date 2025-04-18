﻿namespace NexusGB;

using ConsoleNexusEngine;
using ConsoleNexusEngine.Graphics;
using ConsoleNexusEngine.IO;
using NexusGB.Common;
using NexusGB.GameBoy;
using NexusGB.Statics;

public sealed class GameBoyEmulator : NexusConsoleGame
{
    private readonly string _romSavePath;
    private readonly ConfigurationWatcher _watcher;

    private readonly WindowsSoundOut _soundOut;
    private readonly DiscordRpc _rpc;

    private readonly Processor _cpu;
    private readonly SoundProcessor _spu;
    private readonly MemoryManagement _mmu;
    private readonly PixelProcessor _ppu;
    private readonly Timer _timer;
    private readonly Joypad _joypad;

    private double accumulatedTime;
    private int cpuCycles;
    private int cyclesThisUpdate;

    public GameBoyEmulator(string romPath, string romSavePath)
    {
        _romSavePath = romSavePath;
        _watcher = new ConfigurationWatcher();

        _watcher.Changed += OnConfigChange;

        var current = _watcher.Current;
        Settings.ColorPalette = new GameBoyColorPalette(current.Color1, current.Color2, current.Color3, current.Color4, current.BackgroundColor);
        Settings.ForceStopKey = NexusKey.Escape;

        var fontSize = ScreenSize.Height / 100;
        if (fontSize % 2 != 0) fontSize++;

        do
        {
            fontSize -= 2;
            Settings.Font = new NexusFont("Consolas", new NexusSize(fontSize));
        } while (BufferSize.Width <= GameBoySystem.ScreenWidth || BufferSize.Height <= GameBoySystem.ScreenHeight);

        _soundOut = new WindowsSoundOut
        {
            Volume = 25f
        };

        _rpc = DiscordRpc.Initialize();

        _spu = new SoundProcessor(_soundOut);
        _mmu = MemoryManagement.LoadGamePak(romPath, _romSavePath, _spu);
        _cpu = new Processor(_mmu);
        _timer = new Timer(_mmu, _spu);
        _ppu = new PixelProcessor(Graphic, _mmu, BufferSize.Width, BufferSize.Height);
        _joypad = new Joypad(_mmu);

        _rpc.SetGame(_mmu.GameTitle);
        Settings.Title = $"NexusGB - {_mmu.GameTitle}";
    }

    protected override void Load()
    {
        Logger.LogInfo("Initialization completed successfully");

        Graphic.DrawShape(NexusCoord.MinValue, new NexusRectangle(BufferSize, new NexusChar(' ', NexusColorIndex.Color15, NexusColorIndex.Color15), true));
        Graphic.DrawLine(new NexusCoord(BufferSize.Width - 5, 0), new NexusCoord(BufferSize.Width - 1, 4), new NexusChar(' ', NexusColorIndex.Color14, NexusColorIndex.Color14));
        Graphic.DrawLine(new NexusCoord(BufferSize.Width - 1, 0), new NexusCoord(BufferSize.Width - 5, 4), new NexusChar(' ', NexusColorIndex.Color14, NexusColorIndex.Color14));
        Graphic.Render();
    }

    protected override void Update()
    {
        accumulatedTime += DeltaTime * 1_000_000_000;

        while (accumulatedTime >= 16742706)
        {
            accumulatedTime -= 16742706;

            while (cyclesThisUpdate < GameBoySystem.CyclesPerUpdate)
            {
                cpuCycles = _cpu.Execute();
                cyclesThisUpdate += cpuCycles;

                _ppu.Update(cpuCycles);
                _timer.Update(cpuCycles);
                _spu.Update(cpuCycles);
                _joypad.Update();

                _cpu.HandleInterrupts();
            }

            Input.UpdateGamepads();
            Input.Update();
            _joypad.HandleInputs(_watcher.Current.Controls, Input.Gamepad1, Input.Keys);

            cyclesThisUpdate -= GameBoySystem.CyclesPerUpdate;
        }

        Graphic.DrawText(NexusCoord.MinValue, new NexusText($"FPS: {FramesPerSecond}", NexusColorIndex.Background, NexusColorIndex.Color15));
    }

    protected override void OnCrash(Exception exception)
    {
        Logger.LogCritical("The emulator crashed unexpectedly", exception);

#if !DEBUG
        Utility.ShowAlert("Error", $"An error occured:\n{exception}", NexusAlertIcon.Error);
#endif

        CleanUp();
    }

    protected override void CleanUp()
    {
        _mmu.SaveGame(_romSavePath);

        _watcher.Dispose();
        _soundOut.Dispose();
        _rpc.Dispose();
    }

    private void OnConfigChange(object? sender, EmulatorConfig old)
    {
        var current = _watcher.Current;
        Settings.ColorPalette = new GameBoyColorPalette(current.Color1, current.Color2, current.Color3, current.Color4, current.BackgroundColor);
    }
}