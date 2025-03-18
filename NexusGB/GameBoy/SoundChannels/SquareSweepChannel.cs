namespace NexusGB.GameBoy.SoundChannels;

public sealed class SquareSweepChannel : BaseSoundChannel
{
    private int frequencyTimer;
    private int waveDutyPosition;
    private int currentFrameSequencerTick;
    private int lengthTimer;
    private int currentEnvelopeVolume;
    private int volumePeriodTimer;
    private int sweepTimer;
    private int shadowFrequency;
    private bool sweepEnabled;

    private byte WavePatternDuty => (byte)((number1 & 0b1100_0000) >> 6);
    private byte InitialVolume => (byte)((Number2 & 0b1111_0000) >> 4);
    private bool VolumeEnvelopeDirection => Bits.Is(Number2, 3);
    private byte VolumeSweepPeriod => (byte)(Number2 & 0b0000_0111);

    private byte FrequencySweepPeriod => (byte)((Number0 & 0b0111_0000) >> 4);
    private byte FrequencySweepShiftAmount => (byte)(Number0 & 0b0000_0111);

    private ushort FrequencyRegister
    {
        get => (ushort)(Bits.MakeWord(number4, Number3) & 0x7FF);
        set
        {
            number4 = (byte)((number4 & 0b1111_1000) | (Bits.GetHighByte(value) & 0b0000_0111));

            Number3 = Bits.GetLowByte(value);
        }
    }

    public override byte Number0
	{
		get => (byte)(number0 | 0b1000_0000);
		set => number0 = (byte)(value | 0b1000_0000);
	}

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

	public SquareSweepChannel(SoundProcessor spu) : base(spu) { }

	public override void Update(in int cycles)
	{
		CheckDacEnabled();

		if (!Playing) return;

		if (_spu.ShouldTickFrameSequencer) TickFrameSequencer();

		frequencyTimer -= cycles;
		if (frequencyTimer > 0) return;

		frequencyTimer += (2048 - FrequencyRegister) * 4;

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
		sweepTimer = 0;
		shadowFrequency = 0;
		sweepEnabled = false;
	}

	public override short GetCurrentAmplitudeLeft()
	{
		if (!_spu.Enabled || !Bits.Is(_spu.SoundOutputTerminalSelectRegister, 4)) return 0;

		var volume = currentEnvelopeVolume * _spu.LeftChannelVolume * VolumeMultiplier;

		return (short)(WaveDutyTable[WavePatternDuty, waveDutyPosition] * volume);
	}

	public override short GetCurrentAmplitudeRight()
	{
		if (!_spu.Enabled || !Bits.Is(_spu.SoundOutputTerminalSelectRegister, 0)) return 0;

		var volume = currentEnvelopeVolume * _spu.RightChannelVolume * VolumeMultiplier;

		return (short)(WaveDutyTable[WavePatternDuty, waveDutyPosition] * volume);
	}

    private void TickFrameSequencer()
    {
        if (currentFrameSequencerTick % 2 == 0) UpdateLength();
        if (Playing && currentFrameSequencerTick == 7) UpdateVolume();
        if (Playing && currentFrameSequencerTick is 2 or 6) UpdateSweep();

        currentFrameSequencerTick++;
        currentFrameSequencerTick %= 8;
    }

    private void UpdateLength()
    {
        if (!Bits.Is(number4, 6)) return;

        if (lengthTimer <= 0 || --lengthTimer != 0) return;

        Playing = false;
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

    private void UpdateSweep()
    {
        if (sweepTimer > 0) sweepTimer--;
        if (sweepTimer != 0) return;

        sweepTimer = FrequencySweepPeriod != 0 ? FrequencySweepPeriod : 8;

        if (!sweepEnabled || FrequencySweepPeriod == 0) return;

        int newFrequency = CalculateNewFrequency();

        if (newFrequency >= 2048 || FrequencySweepShiftAmount <= 0) return;

        FrequencyRegister = (ushort)newFrequency;
        shadowFrequency = newFrequency;

        CalculateNewFrequency();
    }

    private int CalculateNewFrequency()
    {
        int newFrequency = shadowFrequency >> FrequencySweepShiftAmount;

        if (Bits.Is(Number0, 3)) newFrequency = -newFrequency;

        newFrequency += shadowFrequency;

        if (newFrequency >= 2048) Playing = false;

        return newFrequency;
    }

    private void TriggerWritten()
    {
        if (!_spu.Enabled || !Bits.Is(number4, 7)) return;

        Playing = true;

        if (lengthTimer == 0) lengthTimer = 64;

        volumePeriodTimer = VolumeSweepPeriod;
        currentEnvelopeVolume = InitialVolume;

        shadowFrequency = FrequencyRegister;
        sweepTimer = FrequencySweepPeriod != 0 ? FrequencySweepPeriod : 8;
        sweepEnabled = FrequencySweepPeriod != 0 || FrequencySweepShiftAmount != 0;

        if (FrequencySweepShiftAmount != 0) CalculateNewFrequency();

        CheckDacEnabled();
    }

    private void CheckDacEnabled()
    {
        var dacEnabled = InitialVolume != 0 || VolumeEnvelopeDirection;
        if (!dacEnabled) Playing = false;
    }
}