﻿namespace NexusGB;

using ConsoleNexusEngine;
using ConsoleNexusEngine.Graphics;
using ConsoleNexusEngine.IO;
using Figgle;
using NexusGB.Common;
using NexusGB.GameBoy;
using NexusGB.Statics;
using System.Diagnostics;

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

    private readonly NexusSimpleSprite _overlaySprite;
    private readonly ClickableSprite _configButton;
    private readonly ClickableSprite _exitButton;

    private bool isPaused;
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
        Settings.ForceStopKey = NexusKey.None;

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

        var configSprite = new NexusFiggleText("Settings", FiggleFonts.OldBanner, NexusColorIndex.Color14, NexusColorIndex.Color15);
        _configButton = new ClickableSprite(Input, configSprite.Map,
            new NexusCoord(BufferSize.Width - configSprite.Map.Size.Width, BufferSize.Height / 2 - configSprite.Map.Size.Height));
        _configButton.MouseOver += OnConfigButtonMouseOver;

        var exitButtonMap = new NexusCompoundSpriteBuilder()
            .AddLine(new NexusCoord(BufferSize.Width - 5, 0), new NexusCoord(BufferSize.Width - 1, 4), new NexusChar(' ', NexusColorIndex.Color14, NexusColorIndex.Color14))
            .AddLine(new NexusCoord(BufferSize.Width - 5, 4), new NexusCoord(BufferSize.Width - 1, 0), new NexusChar(' ', NexusColorIndex.Color14, NexusColorIndex.Color14))
            .BuildMap();
        _exitButton = new ClickableSprite(Input, exitButtonMap, new NexusCoord(BufferSize.Width - 5, 0));
        _exitButton.MouseOver += OnExitButtonMouseOver;

        _overlaySprite = new NexusCompoundSpriteBuilder(new NexusRectangle(BufferSize, new NexusChar(' ', NexusColorIndex.Color15, NexusColorIndex.Color15), true), 0)
            .AddSprite(_configButton.StartPos, _configButton)
            .AddSprite(_exitButton)
            .Build();
    }

    private void OnConfigButtonMouseOver(object? sender, EventArgs e)
    {
        if (Input.Keys.Contains(NexusKey.MouseLeft))
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = _watcher.ConfigPath,
                    UseShellExecute = true
                };

                process.Start();
            }
        }
    }

    private void OnExitButtonMouseOver(object? sender, EventArgs e)
    {
        if (Input.Keys.Contains(NexusKey.MouseLeft))
        {
            isPaused = true;

            var isConfirmed = ShowConfirmation("Do you really want to close the emulator?");

            if (isConfirmed) Stop();

            Graphic.DrawSprite(NexusCoord.MinValue, _overlaySprite);

            isPaused = false;
        }
    }

    protected override void Load()
    {
        Logger.LogInfo("Initialization completed successfully");

        Graphic.DrawSprite(NexusCoord.MinValue, _overlaySprite);
        Graphic.Render();
    }

    protected override void Update()
    {
        if (isPaused) return;

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
        _configButton.MouseOver -= OnConfigButtonMouseOver;
        _exitButton.MouseOver -= OnExitButtonMouseOver;

        _configButton.Dispose();
        _exitButton.Dispose();

        _mmu.SaveGame(_romSavePath);

        _watcher.Dispose();
        _soundOut.Dispose();
        _rpc.Dispose();
    }

    private bool ShowConfirmation(string text)
    {
        var result = false;

        using (var waitSignal = new AutoResetEvent(false))
        {
            var message = new NexusFiggleText(text, FiggleFonts.OldBanner, NexusColorIndex.Color15);

            var yesMap = new NexusFiggleText("Yes", FiggleFonts.OldBanner, NexusColorIndex.Color15).Map;
            var yesPos = new NexusCoord((BufferSize.Width - yesMap.Size.Width) / 4, BufferSize.Height / 2);
            var yesBtn = new ClickableSprite(Input, yesMap, yesPos);
            yesBtn.MouseOver += (_, _) =>
            {
                if (Input.Keys.Contains(NexusKey.MouseLeft))
                {
                    result = true;
                    waitSignal.Set();
                }
            };

            var noMap = new NexusFiggleText("No", FiggleFonts.OldBanner, NexusColorIndex.Color15).Map;
            var noPos = new NexusCoord((BufferSize.Width - noMap.Size.Width) / 2, BufferSize.Height / 2);
            var noBtn = new ClickableSprite(Input, noMap, noPos);
            noBtn.MouseOver += (_, _) =>
            {
                if (Input.Keys.Contains(NexusKey.MouseLeft))
                {
                    result = false;
                    waitSignal.Set();
                }
            };

            Graphic.Clear();
            Graphic.DrawSprite(new NexusCoord((BufferSize.Width - message.Map.Size.Width) / 2, BufferSize.Height / 3), message);
            Graphic.DrawSprite(yesPos, yesBtn);
            Graphic.DrawSprite(noPos, noBtn);
            Graphic.Render();

            waitSignal.WaitOne();
        }

        return result;
    }

    private void OnConfigChange(object? sender, EmulatorConfig old)
    {
        var current = _watcher.Current;

        var newPalette = new GameBoyColorPalette(current.Color1, current.Color2, current.Color3, current.Color4, current.BackgroundColor);
        if (Settings.ColorPalette.SequenceEqual(newPalette)) return;

        Settings.ColorPalette = newPalette;
    }
}