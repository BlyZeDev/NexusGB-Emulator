namespace NexusGB.GameBoy.SoundChannels;

using System.Buffers;
using System.Runtime.CompilerServices;

public sealed class WaveSoundChannel : BaseSoundChannel
{
    private readonly byte[] _ram;

    private int coordinate;
    private bool top;

    private int Frequency => (int)(GameBoySystem.ClockFrequency / (64 * (2048 - (ReadNumber(3) | (ReadNumber(4) & 0b111) << 8))));

    public WaveSoundChannel(SoundProcessor spu)
        : base(spu, 3) => _ram = new byte[16];

    public override void Update(in int cycles)
    {
        var delta = cycles / GameBoySystem.ClockFrequency;

        var sampleRate = _out.SampleRate;
        var sampleCount = (int)Math.Ceiling(delta * sampleRate) * 2;

        using (var memory = MemoryPool<float>.Shared.Rent(sampleCount))
        {
            var buffer = memory.Memory.Span;

            var intervalSampleCount = (int)(1d / Frequency * sampleRate);

            if (intervalSampleCount > 0)
            {
                for (int i = 0; i < sampleCount; i += 2)
                {
                    coordinate++;
                    if (coordinate >= intervalSampleCount)
                    {
                        top = !top;
                        coordinate = 0;
                    }

                    var ramCoordinate = (int)(coordinate / (double)intervalSampleCount * _ram.Length);
                    var waveDataSample = top ? (_ram[ramCoordinate] & 0xF) : ((_ram[ramCoordinate] >> 4) & 0xF);

                    _spu.WriteToSoundBuffer(_channelNumber, buffer, i, ChannelVolume * CalcOutputLevel() * (waveDataSample - 7) / 15f);
                }
            }

            _out.BufferSoundSamples(buffer, sampleCount);
        }
    }

    public byte ReadRam(in ushort address) => _ram[address];

    public void WriteRam(in ushort address, in byte value) => _ram[address] = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float CalcOutputLevel()
    {
        return (ReadNumber(2) >> 5 & 0b11) switch
        {
            1 => 1f,
            2 => 0.5f,
            3 => 0.25f,
            _ => 0f
        };
    }
}