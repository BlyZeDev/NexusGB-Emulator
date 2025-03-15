namespace NexusGB.GameBoy.SoundChannels;

public abstract class BaseSoundChannel
{
    private readonly byte[] _numbers;

    protected BaseSoundChannel()
    {
        _numbers = new byte[5];
    }

    public abstract void Update(in int cycles);

    public virtual byte ReadNumber(in int index) => _numbers[index];

    public virtual void WriteNumber(in int index, in byte value) => _numbers[index] = value;
}