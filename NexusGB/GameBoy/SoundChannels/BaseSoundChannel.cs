namespace NexusGB.GameBoy.SoundChannels;

public abstract class BaseSoundChannel
{
    protected const int VolumeMultiplier = 25;
    protected static readonly sbyte[,] WaveDutyTable =
    {
        { -1, 1, 1, 1, 1, 1, 1, 1 },
        { -1, -1, 1, 1, 1, 1, 1, 1 },
        { -1, -1, -1, -1, 1, 1, 1, 1 },
        { -1, -1, -1, -1, -1, -1, 1, 1 }
    };

    protected readonly SoundProcessor _spu;

    protected byte number0;
    protected byte number1;
    protected byte number2;
    protected byte number3;
    protected byte number4;

    public virtual byte Number0
    {
        get => number0;
        set => number0 = value;
    }

    public virtual byte Number1
    {
        get => number1;
        set => number1 = value;
    }

    public virtual byte Number2
    {
        get => number2;
        set => number2 = value;
    }

    public virtual byte Number3
    {
        get => number3;
        set => number3 = value;
    }

    public virtual byte Number4
    {
        get => number4;
        set => number4 = value;
    }

    public bool IsPlaying { get; protected set; }

    protected BaseSoundChannel(SoundProcessor spu) => _spu = spu;

    public virtual void Reset()
    {
        number0 = 0;
        number1 = 0;
        number2 = 0;
        number3 = 0;
        number4 = 0;
    }

    public abstract void Update(in int cycles);

    public abstract short GetCurrentAmplitudeLeft();
    public abstract short GetCurrentAmplitudeRight();
}