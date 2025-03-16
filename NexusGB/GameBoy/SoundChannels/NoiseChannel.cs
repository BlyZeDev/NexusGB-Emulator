namespace NexusGB.GameBoy.SoundChannels;

using System.Buffers;

public sealed class NoiseChannel : BaseSoundChannel
{
    private readonly VolumeEnvelope _volume;
    private readonly LfsRegister _lfsr;

    private int clock;
    private double length;

    private bool UseSoundLength => (ReadNumber(4) & (1 << 6)) != 0;
    private double SoundLength => (64 - (ReadNumber(1) & 0b111111)) * (1 / 256f);
    private int DividingRatio => ReadNumber(3) & 0b111;
    private float Frequency => (float)(GameBoySystem.ClockFrequency / 8 / (DividingRatio == 0 ? 0.5 : DividingRatio) / Math.Pow(2, (ReadNumber(3) >> 4) + 1));

    public NoiseChannel(SoundProcessor spu) : base(spu, 4)
    {
        _volume = new VolumeEnvelope(this);
        _lfsr = new LfsRegister(this);
    }

    public override void Update(in int cycles)
    {
        _volume.Update(cycles);
        var amplitude = ChannelVolume * (_volume.Volume / 15f);
        var delta = cycles / GameBoySystem.ClockFrequency;

        var sampleRate = _out.SampleRate;
        var sampleCount = (int)Math.Ceiling(delta * sampleRate) * 2;

        using (var memory = MemoryPool<float>.Shared.Rent(sampleCount))
        {
            var buffer = memory.Memory.Span;
            if (!UseSoundLength || length >= 0)
            {
                var periodSampleCount = (int)(1 / Frequency * sampleRate) * 2;

                for (int i = 0; i < sampleCount; i += 2)
                {
                    _spu.WriteToSoundBuffer(_channelNumber, buffer, i, amplitude * (_lfsr.CurrentValue ? 1f : 0f));

                    clock += 2;
                    if (clock >= periodSampleCount)
                    {
                        _lfsr.Shift();
                        clock -= periodSampleCount;
                    }
                }

                if (UseSoundLength) length -= delta;
            }

            _out.BufferSoundSamples(buffer, sampleCount);
        }
    }

    public override byte ReadNumber(in int index) => index == 0 ? byte.MinValue : base.ReadNumber(index);

    public override void WriteNumber(in int index, in byte value)
    {
        switch (index)
        {
            case 0: break;
            case 1:
                base.WriteNumber(index, value);
                length = SoundLength;
                break;
            case 2:
                base.WriteNumber(index, value);
                _volume.Reset();
                break;
            case 3:
                base.WriteNumber(index, value);
                clock = 0;
                _lfsr.Reset();
                break;
            case 4: base.WriteNumber(index, value); break;
        }
    }

    private sealed class LfsRegister
    {
        private readonly NoiseChannel _channel;
        private short state;

        public bool CurrentValue => (state & 1) == 1;

        public bool Use7BitStepWidth
        {
            get => (_channel.ReadNumber(3) & (1 << 3)) != 0;
            set
            {
                _channel.WriteNumber(3, (byte)((_channel.ReadNumber(3) & ~(1 << 3)) | (value ? (1 << 3) : 0)));
                Reset();
            }
        }

        public LfsRegister(NoiseChannel channel)
        {
            _channel = channel;
            state = 0x7F;
        }

        public void Reset() => state = (short)(Use7BitStepWidth ? 0x7F : 0x7FFF);

        public void Shift()
        {
            var nextBit = (byte)(((state >> 1) & 1) ^ (state & 1));
            state >>= 1;
            state |= (short)(nextBit << (Use7BitStepWidth ? 6 : 14));
        }
    }
}