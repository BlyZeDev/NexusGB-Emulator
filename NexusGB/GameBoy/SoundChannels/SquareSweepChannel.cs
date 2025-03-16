namespace NexusGB.GameBoy.SoundChannels;

public sealed class SquareSweepChannel : SquareChannel
{
    private double frequencySweepClock;

    private float SweepTime => ((ReadNumber(0) >> 4) & 7) / 128f;

    public SquareSweepChannel(SoundProcessor spu) : base(spu, 1) { }

    public override void WriteNumber(in int index, in byte value)
    {
        base.WriteNumber(index, value);
        if (index == 0) frequencySweepClock = 0;
    }

    public override void Update(in int cycles)
    {
        if (SweepTime > 0)
        {
            frequencySweepClock += cycles / GameBoy.ClockFrequency;

            while (frequencySweepClock >= SweepTime)
            {
                frequencySweepClock -= SweepTime;

                var delta = (int)(FrequencyRegister / Math.Pow(2, ReadNumber(0) & 0b111));

                if (((ReadNumber(0) >> 3) & 1) != 0) delta = -delta;

                FrequencyRegister += delta;
            }
        }

        base.Update(cycles);
    }
}