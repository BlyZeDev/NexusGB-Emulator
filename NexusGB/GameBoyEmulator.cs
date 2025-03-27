namespace NexusGB;

using ConsoleNexusEngine;
using ConsoleNexusEngine.Graphics;
using ConsoleNexusEngine.IO;
using NexusGB.GameBoy;
using NexusGB.Statics;

public sealed class GameBoyEmulator : NexusConsoleGame
{
    private readonly string _romSavePath;

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

        Settings.ColorPalette = new GameBoyColorPalette();
        Settings.Font = new NexusFont("Consolas", new NexusSize(8));
        Settings.ForceStopKey = NexusKey.Escape;

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

    protected override void Load() => Logger.LogInfo("Initialization completed successfully");

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

                HandleInterrupts();
            }

            Input.UpdateGamepads();
            Input.Update();
            _joypad.HandleInputs(Input.Gamepad1, Input.Keys);

            cyclesThisUpdate -= GameBoySystem.CyclesPerUpdate;
        }
    }

    protected override void OnCrash(Exception exception)
    {
        Logger.LogCritical("The emulator crashed unexpectedly", exception);

#if !DEBUG
        Utility.ShowAlert("Error", $"An error occured:\n{exception}", NexusAlertIcon.Error);
#endif
    }

    protected override void CleanUp()
    {
        _mmu.SaveGame(_romSavePath);

        _soundOut.Dispose();
        _rpc.Dispose();
    }

    private void HandleInterrupts()
    {
        var ief = _mmu.InterruptEnable & _mmu.InterruptFlag;

        for (int i = 0; i < 5; i++)
        {
            if (((ief >> i) & 0x01) == 1)
            {
                _cpu.ExecuteInterrupt(i);
            }
        }

        _cpu.UpdateIme();
    }
}