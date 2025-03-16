namespace NexusGB.GameBoy.SoundChannels;

using System.Buffers;
using System.Runtime.CompilerServices;

public class SquareChannel : BaseSoundChannel
{
    private readonly VolumeEnvelope _volume;

    private int coordinate;
    private double length;
    private int frequencyRegister;

    protected bool UseSoundLength => (ReadNumber(4) & (1 << 6)) != 0;
    protected double SoundLength => (64 - (ReadNumber(1) & 0b111111)) * (1 / 256f);

    protected int FrequencyRegister
    {
        get => frequencyRegister;
        set => frequencyRegister = value & 0b111_1111_1111;
    }

    protected float Frequency
    {
        get => (float)(GameBoySystem.ClockFrequency / (32 * (2048 - FrequencyRegister)));
        set => FrequencyRegister = (int)(2048 - GameBoySystem.ClockFrequency / (32 * value));
    }

    public SquareChannel(SoundProcessor spu) : this(spu, 2) { }

    protected SquareChannel(SoundProcessor spu, in int channelNumber)
        : base(spu, channelNumber) => _volume = new VolumeEnvelope(this);

    public override void Update(in int cycles)
    {
        _volume.Update(cycles);
        var amplitude = ChannelVolume * (_volume.Volume / 15d);
        var delta = cycles / GameBoySystem.ClockFrequency;

        var sampleRate = _out.SampleRate;
        var sampleCount = (int)Math.Ceiling(delta * sampleRate) * 2;
        using (var memory = MemoryPool<float>.Shared.Rent(sampleCount))
        {
            var buffer = memory.Memory.Span;
            if (!UseSoundLength || length >= 0)
            {
                var period = 1d / Frequency;
                for (int i = 0; i < sampleCount; i += 2)
                {
                    var sample = DutyWave(amplitude, (double)coordinate / sampleRate, period);
                    _spu.WriteToSoundBuffer(_channelNumber, buffer, i, sample);

                    coordinate = (coordinate + 1) % sampleRate;
                }

                if (UseSoundLength) length -= delta;
            }

            _out.BufferSoundSamples(buffer, sampleCount);
        }
    }

    public sealed override byte ReadNumber(in int index)
        => index == 3 ? (byte)(FrequencyRegister & 0xFF) : base.ReadNumber(index);

    public override void WriteNumber(in int index, in byte value)
    {
        switch (index)
        {
            case 0: base.WriteNumber(index, value); break;
            case 1:
                base.WriteNumber(index, value);
                length = SoundLength;
                break;
            case 2:
                base.WriteNumber(index, value);
                _volume.Reset();
                break;
            case 3: FrequencyRegister = (FrequencyRegister & ~0xFF) | value; break;
            case 4:
                base.WriteNumber(index, (byte)(value & (1 << 6)));
                if ((value & (1 << 7)) != 0)
                {
                    length = SoundLength;
                    coordinate = 0;
                }
                FrequencyRegister = (FrequencyRegister & 0xFF) | (value & 0b111) << 8;
                break;
        }
    }

    private float DutyWave(in double amplitude, in double x, in double period)
    {
        var duty = ((ReadNumber(1) >> 6) & 0b11) switch
        {
            0 => 0.125f,
            1 => 0.25f,
            2 => 0.5f,
            _ => 0.75f
        };

        float saw1 = (float)(-2 * amplitude / Math.PI * Math.Atan(Cot(x * Math.PI / period)));
        float saw2 = (float)(-2 * amplitude / Math.PI * Math.Atan(Cot(x * Math.PI / period - (1 - duty) * Math.PI)));
        return saw1 - saw2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double Cot(in double x) => 1 / Math.Tan(x);
}