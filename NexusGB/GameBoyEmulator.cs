namespace NexusGB;

using ConsoleNexusEngine;
using ConsoleNexusEngine.Graphics;
using ConsoleNexusEngine.IO;
using Microsoft.VisualBasic;
using NexusGB.GameBoy;

public sealed class GameBoyEmulator : NexusConsoleGame
{
    private const int DMG_4Mhz = 4194304;
    private const float REFRESH_RATE = 59.7275f;
    private const int CYCLES_PER_UPDATE = (int)(DMG_4Mhz / REFRESH_RATE);

    private readonly Processor _cpu;
    private readonly MemoryManagement _mmu;
    private readonly PixelProcessor _ppu;
    private readonly Timer _timer;
    private readonly Joypad _joypad;

    private double accumulatedTime;
    private int cpuCycles;
    private int cyclesThisUpdate;

    public GameBoyEmulator(string rom)
    {
        _mmu = MemoryManagement.LoadGamePak(rom);
        _cpu = new Processor(_mmu);
        _ppu = new PixelProcessor(Graphic, _mmu);
        _timer = new Timer(_mmu);
        _joypad = new Joypad(_mmu);
    }

    protected override void Load()
    {
        Settings.ColorPalette = new GameBoyColorPalette();
        Settings.Font = new NexusFont("Consolas", new NexusSize(8));
        Settings.Title = "NexusGB";
        Settings.StopGameKey = NexusKey.Escape;
    }
    
    protected override void Update()
    {
        accumulatedTime += DeltaTime * 1_000_000_000;

        while (accumulatedTime >= 16740000)
        {
            accumulatedTime -= 16740000;

            Input.UpdateGamepads();

            while (cyclesThisUpdate < CYCLES_PER_UPDATE)
            {
                _joypad.HandleInputs(Input.Gamepad1);

                cpuCycles = _cpu.Execute();
                cyclesThisUpdate += cpuCycles;

                _timer.Update(cpuCycles);
                _ppu.Update(cpuCycles);
                _joypad.Update();
                HandleInterrupts();
            }

            cyclesThisUpdate -= CYCLES_PER_UPDATE;
        }
    }

    protected override void OnCrash(Exception exception)
        => Utility.ShowAlert("Error", $"An error occured:\n{exception}", NexusAlertIcon.Error);

    protected override void CleanUp()
    {

    }

    private void HandleInterrupts()
    {
        var interruptEnable = _mmu.InterruptEnable;
        var interruptFlag = _mmu.InterruptFlag;

        for (int i = 0; i < 5; i++)
        {
            if ((((interruptEnable & interruptFlag) >> i) & 0x01) == 1)
            {
                _cpu.ExecuteInterrupt(i);
            }
        }

        _cpu.UpdateIme();
    }
}