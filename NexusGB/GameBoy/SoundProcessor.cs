namespace NexusGB.GameBoy;

using NexusGB.Common;
using NexusGB.GameBoy.SoundChannels;
using System.Collections.Immutable;
using System.Diagnostics;

public sealed class SoundProcessor
{
    private readonly ImmutableArray<BaseSoundChannel> _channels;
    private readonly WaveSoundChannel _wave;
    private readonly WindowsSoundOut _soundOut;

    private int internalCounter;
    private byte number50;

    public byte Number51 { get; private set; }
    public byte Number52
    {
        get => Bits.MakeByte(Enabled, true, true, true, _channels[3].IsPlaying, _channels[2].IsPlaying, _channels[1].IsPlaying, _channels[0].IsPlaying);
        private set => Enabled = (value & 0b1000_0000) != 0;
    }

    public byte LeftChannelVolume => (byte)((number50 & 0b0111_0000) >> 4);
    public byte RightChannelVolume => (byte)(number50 & 0b0000_0111);

    public bool Enabled { get; private set; }

    public SoundProcessor(WindowsSoundOut soundOut)
    {
        _channels =
        [
            new SquareSweepChannel(this),
            new SquareChannel(this),
            _wave = new WaveSoundChannel(this),
            new NoiseChannel(this)
        ];
        _soundOut = soundOut;
    }

    public bool ShouldTickFrameSequencer { get; private set; }

    public void TickFrameSequencer()
    {
        if (!Enabled) return;

        ShouldTickFrameSequencer = true;
    }

    public void Update(in int cycles)
    {
        if (!Enabled)
        {
            Reset();
            foreach (var channel in _channels)
            {
                channel.Reset();
            }

            return;
        }

        foreach (var channel in _channels)
        {
            channel.Update(cycles);
        }

        if (ShouldTickFrameSequencer) ShouldTickFrameSequencer = false;

        internalCounter += WindowsSoundOut.SAMPLE_RATE * cycles;

        if (internalCounter < GameBoySystem.ClockFrequency) return;

        internalCounter -= (int)GameBoySystem.ClockFrequency;

        var leftSample = 0;
        var rightSample = 0;
        foreach (var channel in _channels)
        {
            if (channel.IsPlaying)
            {
                leftSample += channel.GetCurrentAmplitudeLeft();
                rightSample += channel.GetCurrentAmplitudeRight();
            }
        }

        _soundOut.AddSamples((short)leftSample, (short)rightSample);
    }

    public byte ReadByte(in ushort address)
    {
        switch (address)
        {
            case 0xFF24: return number50;
            case 0xFF25: return Number51;
            case 0xFF26: return Number52;

            case >= 0xFF27 and < 0xFF30: return 0x00;
            case >= 0xFF30 and < 0xFF40: return _wave.ReadRam((ushort)(address - 0xFF30));
        }

        var relativeAddress = address - 0xFF10;
        var channel = _channels[relativeAddress / 5];
        return (relativeAddress % 5) switch
        {
            0 => channel.Number0,
            1 => channel.Number1,
            2 => channel.Number2,
            3 => channel.Number3,
            4 => channel.Number4,
            _ => throw new UnreachableException(nameof(ReadByte))
        };
    }

    public void WriteByte(in ushort address, in byte value)
    {
        switch (address)
        {
            case 0xFF24: number50 = value; return;
            case 0xFF25: Number51 = value; return;
            case 0xFF26: Number52 = value; return;

            case >= 0xFF27 and < 0xFF30: return;
            case >= 0xFF30 and < 0xFF40: _wave.WriteRam((ushort)(address - 0xFF30), value); return;
        }

        var relativeAddress = address - 0xFF10;
        var channel = _channels[relativeAddress / 5];
        
        switch (relativeAddress % 5)
        {
            case 0: channel.Number0 = value; break;
            case 1: channel.Number1 = value; break;
            case 2: channel.Number2 = value; break;
            case 3: channel.Number3 = value; break;
            case 4: channel.Number4 = value; break;
        }
    }

    private void Reset()
    {
        number50 = 0;
        Number51 = 0;
        internalCounter = 0;
    }
}