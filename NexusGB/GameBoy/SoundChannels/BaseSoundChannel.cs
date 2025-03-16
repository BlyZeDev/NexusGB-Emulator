namespace NexusGB.GameBoy.SoundChannels;

public abstract class BaseSoundChannel
{
    private readonly byte[] _numbers;
    protected readonly int _channelNumber;
    protected readonly SoundProcessor _spu;
    protected readonly WindowsSoundOut _out;

    private float volume;

    public ref float ChannelVolume => ref volume;

    protected BaseSoundChannel(SoundProcessor spu, in int channelNumber)
    {
        _numbers = new byte[5];
        _channelNumber = channelNumber;
        _spu = spu;
        _out = new WindowsSoundOut();

        ChannelVolume = 0.05f;
    }

    public abstract void Update(in int cycles);

    public virtual byte ReadNumber(in int index) => _numbers[index];

    public virtual void WriteNumber(in int index, in byte value) => _numbers[index] = value;
}