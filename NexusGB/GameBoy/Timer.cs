namespace NexusGB.GameBoy;

using System.Collections.Immutable;

public sealed class Timer
{
    private const int TIMER_INTERRUPT = 2;
    private const int DMG_DIV_FREQ = 256;
    private static readonly ImmutableArray<int> _timerControlFrequencies = [1024, 16, 64, 256];

    private readonly MemoryManagement _mmu;
    private readonly SoundProcessor _spu;

    private int divCounter;
    private int timerCounter;

    public Timer(MemoryManagement mmu, SoundProcessor spu)
    {
        _mmu = mmu;
        _spu = spu;
    }

    public void Update(in int cycles)
    {
        HandleDivider(cycles);
        if (_mmu.TimerControlEnabled) HandleTimer(cycles);
    }

    private void HandleDivider(in int cycles)
    {
        divCounter += cycles;
        if (divCounter < DMG_DIV_FREQ) return;

        divCounter -= DMG_DIV_FREQ;

        var prevDivider = _mmu.Divider;
        _mmu.Divider++;

        if ((prevDivider & 0b0001_0000) != 0 && (_mmu.Divider & 0b0001_0000) == 0)
        {
            _spu.TickFrameSequencer();
        }
    }

    private void HandleTimer(in int cycles)
    {
        var counterCycles = timerCounter -= cycles;
        if (counterCycles > 0) return;

        timerCounter = _timerControlFrequencies[_mmu.TimerControlFrequency];
        HandleTimer(-counterCycles);

        if (_mmu.TimerCounter == 0xFF)
        {
            _mmu.TimerCounter = _mmu.TimerModulo;
            _mmu.RequestInterrupt(TIMER_INTERRUPT);
        }
        else _mmu.TimerCounter++;
    }
}