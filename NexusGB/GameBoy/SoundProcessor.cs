namespace NexusGB.GameBoy;

using NexusGB.GameBoy.SoundChannels;
using System.Collections.Immutable;

public sealed class SoundProcessor
{
    private readonly ImmutableArray<BaseSoundChannel> _channels;
    private readonly WaveSoundChannel _wave;

    private byte number50;
    private byte number51;
    private byte number52;

    private byte Sound1Volume => (byte)(number50 & 0x7);

    public SoundProcessor()
    {
        _channels =
        [
            new SquareSweepChannel(this),
            new SquareChannel(this),
            _wave = new WaveSoundChannel(this),
            new NoiseChannel(this)
        ];

        _channels[0].WriteNumber(0, 0x80);
        _channels[0].WriteNumber(1, 0xBF);
        _channels[0].WriteNumber(2, 0xF3);
        _channels[0].WriteNumber(4, 0xBF);

        _channels[1].WriteNumber(1, 0x3F);
        _channels[1].WriteNumber(2, 0x00);
        _channels[1].WriteNumber(4, 0xBF);

        _channels[2].WriteNumber(0, 0x7F);
        _channels[2].WriteNumber(1, 0xFF);
        _channels[2].WriteNumber(2, 0x9F);
        _channels[2].WriteNumber(3, 0xBF);

        _channels[3].WriteNumber(1, 0xFF);
        _channels[3].WriteNumber(2, 0x00);
        _channels[3].WriteNumber(3, 0x00);
        _channels[3].WriteNumber(4, 0xBF);

        number50 = 0x77;
        number51 = 0xF3;
        number52 = 0xF1;
    }

    public void Update(in int cycles)
    {
        if ((number52 & (1 << 7)) == 0) return;

        foreach (var channel in _channels)
        {
            channel.Update(cycles);
        }
    }

    public byte ReadByte(in ushort address)
    {
        switch (address)
        {
            case 0xFF24: return number50;
            case 0xFF25: return number51;
            case 0xFF26: return number52;

            case >= 0xFF27 and < 0xFF30: return 0x00;
            case >= 0xFF30 and < 0xFF40: return _wave.ReadRam((ushort)(address - 0xFF30));
        }

        var relativeAddress = address - 0xFF10;
        return _channels[relativeAddress / 5].ReadNumber(relativeAddress % 5);
    }

    public void WriteByte(in ushort address, in byte value)
    {
        switch (address)
        {
            case 0xFF24: number50 = value; return;
            case 0xFF25: number51 = value; return;
            case 0xFF26: number52 = value; return;

            case >= 0xFF27 and < 0xFF30: return;
            case >= 0xFF30 and < 0xFF40: _wave.WriteRam((ushort)(address - 0xFF30), value); return;
        }

        var relativeAddress = address - 0xFF10;
        _channels[relativeAddress / 5].WriteNumber(relativeAddress % 5, value);
    }

    public void WriteToSoundBuffer(in int channel, in Span<float> totalBuffer, in int index, float sample)
    {
        sample *= Sound1Volume / 7f;

        if ((number51 & (1 << (channel - 1))) != 0)
            totalBuffer[index + 1] = sample;

        if ((number51 & (1 << (channel + 3))) != 0)
            totalBuffer[index] = sample;
    }
}