namespace NexusGB.GameBoy;

using System.Collections.Immutable;

public sealed class Timer
{
    private const int TIMER_INTERRUPT = 2;
    private const int DMG_DIV_FREQ = 256;
    private static readonly ImmutableArray<int> _timerControlFrequencies = [1024, 16, 64, 256];

    private readonly MemoryManagement _mmu;

    private int divCounter;
    private int timerCounter;

    public Timer(MemoryManagement mmu) => _mmu = mmu;

    public void Update(in int cycles)
    {
        HandleDivider(cycles);
        HandleTimer(cycles);
    }

    private void HandleDivider(in int cycles)
    {
        divCounter += cycles;
        while (divCounter >= DMG_DIV_FREQ)
        {
            _mmu.Divider++;
            divCounter -= DMG_DIV_FREQ;
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