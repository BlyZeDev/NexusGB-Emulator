namespace NexusGB.GameBoy.SoundChannels;

public sealed class SquareChannel : BaseSoundChannel
{
    private int frequencyTimer;
    private int waveDutyPosition;
    private int currentFrameSequencerTick;
    private int lengthTimer;
    private int currentEnvelopeVolume;
    private int volumePeriodTimer;

    private byte WavePatternDuty => (byte)((number1 & 0b1100_0000) >> 6);
    private byte InitialVolume => (byte)((Number2 & 0b1111_0000) >> 4);
    private bool VolumeEnvelopeDirection => Bits.Is(Number2, 3);
    private byte VolumeSweepPeriod => (byte)(Number2 & 0b0000_0111);

    public override byte Number1
	{
		get => (byte)(number1 | 0b0011_1111);
		set
		{
			number1 = value;
            lengthTimer = 64 - (number1 & 0b0011_1111);
        }
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

	public SquareChannel(SoundProcessor spu) : base(spu) { }

	public override void Update(in int cycles)
	{
		CheckDacEnabled();

		if (!IsPlaying) return;

		if (_spu.ShouldTickFrameSequencer) TickFrameSequencer();

		frequencyTimer -= cycles;
		if (frequencyTimer > 0) return;

		frequencyTimer += (2048 - (ushort)(Bits.MakeWord(Number3, number4) & 0x7FF)) * 4;

		waveDutyPosition++;
		waveDutyPosition %= 8;
	}

	public override void Reset()
	{
		base.Reset();

		frequencyTimer = 0;
		waveDutyPosition = 0;
		currentFrameSequencerTick = 0;
		lengthTimer = 0;
		currentEnvelopeVolume = 0;
		volumePeriodTimer = 0;
	}

    public override short GetCurrentAmplitudeLeft()
	{
		if (!_spu.Enabled || !Bits.Is(_spu.Number51, 5)) return 0;

		var volume = currentEnvelopeVolume * _spu.LeftChannelVolume * VolumeMultiplier;

		return (short)(WaveDutyTable[WavePatternDuty, waveDutyPosition] * volume);
	}

	public override short GetCurrentAmplitudeRight()
	{
		if (!_spu.Enabled || !Bits.Is(_spu.Number51, 1)) return 0;

		var volume = currentEnvelopeVolume * _spu.RightChannelVolume * VolumeMultiplier;

		return (short)(WaveDutyTable[WavePatternDuty, waveDutyPosition] * volume);
	}
    private void TickFrameSequencer()
    {
        if (currentFrameSequencerTick % 2 == 0) UpdateLength();
        if (IsPlaying && currentFrameSequencerTick == 7) UpdateVolume();

        currentFrameSequencerTick++;
        currentFrameSequencerTick %= 8;
    }

    private void UpdateLength()
    {
        if (!Bits.Is(number4, 6)) return;

        if (lengthTimer <= 0 || --lengthTimer != 0) return;

        IsPlaying = false;
    }

    private void UpdateVolume()
    {
        if (volumePeriodTimer > 0) volumePeriodTimer--;
        if (volumePeriodTimer != 0) return;

        if (VolumeSweepPeriod == 0) return;

        volumePeriodTimer = VolumeSweepPeriod;

        var newVolume = currentEnvelopeVolume + (VolumeEnvelopeDirection ? 1 : -1);

        if (newVolume is >= 0 and < 16) currentEnvelopeVolume = newVolume;
    }

    private void TriggerWritten()
    {
        if (!_spu.Enabled || !Bits.Is(number4, 7)) return;

        IsPlaying = true;

        if (lengthTimer == 0) lengthTimer = 64;

        volumePeriodTimer = VolumeSweepPeriod;
        currentEnvelopeVolume = InitialVolume;

        CheckDacEnabled();
    }

    private void CheckDacEnabled()
    {
        var dacEnabled = InitialVolume != 0 || VolumeEnvelopeDirection;
        if (!dacEnabled) IsPlaying = false;
    }
}