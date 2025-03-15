namespace NexusGB.GameBoy.SoundChannels;

public sealed class SquareChannel : BaseSoundChannel
{
    private readonly VolumeEnvelope _volume;

    public SquareChannel()
    {
        _volume = new VolumeEnvelope(this);
    }

    public override void Update(in int cycles)
    {

    }
}