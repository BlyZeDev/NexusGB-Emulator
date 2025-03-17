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
        HandleTimer(cycles);
    }

    private void HandleDivider(in int cycles)
    {
        var prevDivider = _mmu.Divider;

        divCounter += cycles;
        while (divCounter >= DMG_DIV_FREQ)
        {
            _mmu.Divider++;
            divCounter -= DMG_DIV_FREQ;
        }

        if ((prevDivider & 0b0001_0000) != 0 && (_mmu.Divider & 0b0001_0000) == 0)
        {
            _spu.TickFrameSequencer();
        }
    }

    private void HandleTimer(in int cycles)
    {
        if (!_mmu.TimerControlEnabled) return;

        timerCounter += cycles;
        while (timerCounter >= _timerControlFrequencies[_mmu.TimerControlFrequency])
        {
            _mmu.TimerCounter++;
            timerCounter -= _timerControlFrequencies[_mmu.TimerControlFrequency];
        }

        if (_mmu.TimerCounter == 0xFF)
        {
            _mmu.RequestInterrupt(TIMER_INTERRUPT);
            _mmu.TimerCounter = _mmu.TimerModulo;
        }
    }
}