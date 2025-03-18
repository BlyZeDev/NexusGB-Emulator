namespace NexusGB.GameBoy.SoundChannels;

public sealed class NoiseChannel : BaseSoundChannel
{
    private int frequencyTimer;
    private int currentFrameSequencerTick;
    private int lengthTimer;
    private int currentEnvelopeVolume;
    private int volumePeriodTimer;
    private ushort shiftRegister;

    private byte InitialVolume => (byte)((Number2 & 0b1111_0000) >> 4);
    private bool VolumeEnvelopeDirection => Bits.Is(Number2, 3);
    private byte VolumeSweepPeriod => (byte)(Number2 & 0b0000_0111);

    private byte Divisor
    {
        get
        {
            return (byte)(Number3 & 0b0000_0111) switch
            {
                0 => 8,
                1 => 16,
                2 => 32,
                3 => 48,
                4 => 64,
                5 => 80,
                6 => 96,
                7 => 112,
                _ => 0,
            };
        }
    }

    public override byte Number1
	{
		set
		{
			number1 = (byte)(value & 0b0011_1111);
            lengthTimer = 64 - (byte)(number1 & 0b0011_1111);
        }
	}

	public override byte Number4
	{
		get => (byte)(number4 | 0b1011_1111);
		set
		{
			number4 = (byte)(value & 0b1100_0000);
			TriggerWritten();
		}
	}

	public NoiseChannel(SoundProcessor spu) : base(spu) { }

	public override void Update(in int cycles)
	{
		CheckDacEnabled();

		if (!Playing) return;

		if (_spu.ShouldTickFrameSequencer) TickFrameSequencer();

		frequencyTimer -= cycles;
		if (frequencyTimer > 0) return;

		frequencyTimer += Divisor << (byte)((Number3 & 0b1111_0000) >> 4);

		ShiftRight();
	}

	public override void Reset()
	{
		base.Reset();
		frequencyTimer = 0;
		currentFrameSequencerTick = 0;
		lengthTimer = 0;
		currentEnvelopeVolume = 0;
		volumePeriodTimer = 0;
		shiftRegister = 0;
	}

	public override short GetCurrentAmplitudeLeft()
	{
		if (!_spu.Enabled || !Bits.Is(_spu.SoundOutputTerminalSelectRegister, 7)) return 0;

		var volume = currentEnvelopeVolume * _spu.LeftChannelVolume * VolumeMultiplier;

		return (short)((Bits.Is(shiftRegister, 0) ? -1 : 1) * volume);
	}

	public override short GetCurrentAmplitudeRight()
	{
		if (!_spu.Enabled || !Bits.Is(_spu.SoundOutputTerminalSelectRegister, 3)) return 0;

		var volume = currentEnvelopeVolume * _spu.RightChannelVolume * VolumeMultiplier;

		return (short)((Bits.Is(shiftRegister, 0) ? -1 : 1) * volume);
	}

    private void ShiftRight()
    {
        bool xor = Bits.Is(shiftRegister, 1) ^ Bits.Is(shiftRegister, 0);

        shiftRegister >>= 1;

        if (xor) Bits.Set(ref shiftRegister, 14);
        else Bits.Clear(ref shiftRegister, 14);

        if (Bits.Is(Number3, 3))
        {
            if (xor) Bits.Set(ref shiftRegister, 6);
            else Bits.Clear(ref shiftRegister, 6);
        }
    }

    private void CheckDacEnabled()
    {
        bool dacEnabled = InitialVolume != 0 || VolumeEnvelopeDirection;
        if (!dacEnabled) Playing = false;
    }

    private void TriggerWritten()
    {
        if (!_spu.Enabled || !Bits.Is(number4, 7)) return;

        Playing = true;

        if (lengthTimer == 0) lengthTimer = 64;

        volumePeriodTimer = VolumeSweepPeriod;
        currentEnvelopeVolume = InitialVolume;

        shiftRegister = 0b0111_1111_1111_1111;

        CheckDacEnabled();
    }

    private void TickFrameSequencer()
    {
        if (currentFrameSequencerTick % 2 == 0) UpdateLength();
        if (Playing && currentFrameSequencerTick == 7) UpdateVolume();

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
}