namespace NexusGB.GameBoy.SoundChannels;

public sealed class VolumeEnvelope
{
    private readonly BaseSoundChannel _channel;
    private double timer;

    public int Volume { get; private set; }

    public VolumeEnvelope(BaseSoundChannel channel) => _channel = channel;

    public void Reset()
    {
        Volume = _channel.ReadNumber(2) >> 4;
        timer = 0;
    }

    public void Update(in int cycles)
    {
        var sweepCount = _channel.ReadNumber(2) & 7;
        if (sweepCount < 1) return;

        timer += cycles / Hardware.ClockFrequency;

        var interval = sweepCount / 64d;
        while (timer >= interval)
        {
            timer -= interval;

            if ((_channel.ReadNumber(2) & (1 << 3)) == 0) Volume--;
            else Volume++;

            Volume = Math.Clamp(Volume, 0, 15);
        }
    }
}