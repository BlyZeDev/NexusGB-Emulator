namespace NexusGB.GameBoy.SoundChannels;

public sealed class WaveSoundChannel : BaseSoundChannel
{
    private readonly byte[] _ram;

    private int frequencyTimer;
    private int waveRamPosition;
    private int currentFrameSequencerTick;
    private int lengthTimer;

    private int CurrentVolumeShiftAmount
    {
        get
        {
            return (byte)((Number2 & 0b0110_0000) >> 5) switch
            {
                0b00 => 4,
                0b01 => 0,
                0b10 => 1,
                0b11 => 2,
                _ => 0,
            };
        }
    }

    public override byte Number0
    {
        get => (byte)(number0 & 0b1000_0000);
        set => number0 = (byte)(value & 0b1000_0000);
    }

	public override byte Number1
	{
		get => number1;
		set
		{
			number1 = value;
            lengthTimer = 256 - Number1;
        }
	}

	public override byte Number2
	{
		get => (byte)(number2 & 0b0110_0000);
		set => number2 = (byte)(value & 0b0110_0000);
	}

	public override byte Number4
	{
		get => (byte)(number4 | 0b1011_1111);
		set
		{
			number4 = (byte)(value & 0b1100_0111);
			TriggerWritten();
		}
	}

	public WaveSoundChannel(SoundProcessor spu) : base(spu) => _ram = new byte[0x10];

	public override void Update(in int cycles)
	{
		CheckDacEnabled();

		if (!IsPlaying) return;

		if (_spu.ShouldTickFrameSequencer) TickFrameSequencer();

		frequencyTimer -= cycles;
		if (frequencyTimer > 0) return;

		frequencyTimer += (2048 - (ushort)(Bits.MakeWord(Number3, number4) & 0x7FF)) * 2;

		waveRamPosition++;
		waveRamPosition %= 0x20;
	}

    public byte ReadRam(in ushort address) => IsPlaying ? (byte)0xFF : _ram[address];

    public void WriteRam(in ushort address, in byte value)
	{
		if (IsPlaying) return;

		_ram[address] = value;
	}

	public override short GetCurrentAmplitudeLeft()
	{
		if (!_spu.Enabled || !Bits.Is(_spu.Number51, 6)) return 0;

		var volume = _spu.LeftChannelVolume * VolumeMultiplier;

		return (short)(GetWaveRamSample(waveRamPosition) * volume);
	}

	public override short GetCurrentAmplitudeRight()
	{
		if (!_spu.Enabled || !Bits.Is(_spu.Number51, 2)) return 0;

		var volume = _spu.RightChannelVolume * VolumeMultiplier;

		return (short)(GetWaveRamSample(waveRamPosition) * volume);
	}
    private void TriggerWritten()
    {
        if (!_spu.Enabled || !Bits.Is(number4, 7)) return;

        IsPlaying = true;

        if (lengthTimer == 0) lengthTimer = 256;

        CheckDacEnabled();
    }

    private void CheckDacEnabled()
    {
        if ((number0 & 0b1000_0000) != 0) IsPlaying = false;
    }

    private void TickFrameSequencer()
    {
        if (currentFrameSequencerTick % 2 == 0) UpdateLength();

        currentFrameSequencerTick++;
        currentFrameSequencerTick %= 8;
    }

    private void UpdateLength()
    {
        if (!Bits.Is(number4, 6)) return;

        if (lengthTimer <= 0 || --lengthTimer != 0) return;

        IsPlaying = false;
    }

    private sbyte GetWaveRamSample(in int index)
    {
        var samplePair = _ram[index / 2];

        var numberOfSample = index % 2;

        byte sample;
        if (numberOfSample == 0)
        {
            sample = (byte)((samplePair & 0b1111_0000) >> 4);
            sample >>= CurrentVolumeShiftAmount;

            return (sbyte)(sample - 8);
        }

        sample = (byte)(samplePair & 0b0000_1111);
        sample >>= CurrentVolumeShiftAmount;

        return (sbyte)(sample - 8);
    }
}