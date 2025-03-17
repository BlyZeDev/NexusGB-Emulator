namespace NexusGB.GameBoy.SoundChannels;

public class ApuChannel4
{
	private byte internalSoundLengthRegister;

	//NR41
	public byte SoundLengthRegister
	{
		set
		{
			internalSoundLengthRegister = (byte)(value & 0b0011_1111);
			SoundLengthWritten();
		}
	}

	//NR42
	public byte VolumeEnvelopeRegister { get; set; }

	//NR43
	public byte PolynomialCounterRegister { get; set; }

	private byte internalCounterConsecutiveRegister;

	//NR44
	public byte CounterConsecutiveRegister
	{
		get => (byte)(internalCounterConsecutiveRegister | 0b1011_1111);
		set
		{
			internalCounterConsecutiveRegister = (byte)(value & 0b1100_0000);
			TriggerWritten();
		}
	}

	private byte SoundLength => (byte)(internalSoundLengthRegister & 0b0011_1111);

	private byte InitialVolume           => (byte)((VolumeEnvelopeRegister & 0b1111_0000) >> 4);
	private bool VolumeEnvelopeDirection => Bits.Is(VolumeEnvelopeRegister, 3);
	private byte VolumeSweepPeriod       => (byte)(VolumeEnvelopeRegister & 0b0000_0111);

	private byte ShiftClockFrequency => (byte)((PolynomialCounterRegister & 0b1111_0000) >> 4);

	private bool CounterStepWidth => Bits.Is(PolynomialCounterRegister, 3);

	private byte DividingRatio => (byte)(PolynomialCounterRegister & 0b0000_0111);

	private byte Divisor
	{
		get
		{
            return DividingRatio switch
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

	private bool Trigger => Bits.Is(internalCounterConsecutiveRegister, 7);

	private bool EnableLength => Bits.Is(internalCounterConsecutiveRegister, 6);

	private bool LeftEnabled  => Bits.Is(_spu.SoundOutputTerminalSelectRegister, 7);
	private bool RightEnabled => Bits.Is(_spu.SoundOutputTerminalSelectRegister, 3);

	private int frequencyTimer;

	private int currentFrameSequencerTick;

	private int lengthTimer;

	private int currentEnvelopeVolume;
	private int volumePeriodTimer;

	private ushort shiftRegister;

	public bool Playing { get; private set; }

	private readonly SoundProcessor _spu;

	public ApuChannel4(SoundProcessor spu)
	{
		_spu = spu;
	}

	public void Update(int cycles)
	{
		CheckDacEnabled();

		if (!Playing) return;

		if (_spu.ShouldTickFrameSequencer) TickFrameSequencer();

		frequencyTimer -= cycles;
		if (frequencyTimer > 0) return;

		frequencyTimer += Divisor << ShiftClockFrequency;

		ShiftRight();
	}

	private void ShiftRight()
	{
		bool xor = Bits.Is(shiftRegister, 1) ^ Bits.Is(shiftRegister, 0);

		shiftRegister >>= 1;

		if (xor) Bits.Set(ref shiftRegister, 14);
		else Bits.Clear(ref shiftRegister, 14);

        if (CounterStepWidth)
		{
            if (xor) Bits.Set(ref shiftRegister, 6);
            else Bits.Clear(ref shiftRegister, 6);
        }
	}

	private void CheckDacEnabled()
	{
		bool dacEnabled          = InitialVolume != 0 || VolumeEnvelopeDirection;
		if (!dacEnabled) Playing = false;
	}

	private void SoundLengthWritten()
	{
		lengthTimer = 64 - SoundLength;
	}

	private void TriggerWritten()
	{
		if (!_spu.Enabled || !Trigger) return;

		Playing = true;

		if (lengthTimer == 0) lengthTimer = 64;

		volumePeriodTimer     = VolumeSweepPeriod;
		currentEnvelopeVolume = InitialVolume;

		shiftRegister = 0b0111_1111_1111_1111;

		CheckDacEnabled();
	}

	private void TickFrameSequencer()
	{
		//Only the length gets updated when the channel is disabled
		if (currentFrameSequencerTick % 2 == 0) UpdateLength();
		if (Playing && currentFrameSequencerTick == 7) UpdateVolume();

		currentFrameSequencerTick++;
		currentFrameSequencerTick %= 8;
	}

	private void UpdateLength()
	{
		if (!EnableLength) return;

		if (lengthTimer <= 0 || --lengthTimer != 0) return;

		Playing = false;
	}

	private void UpdateVolume()
	{
		if (volumePeriodTimer > 0) volumePeriodTimer--;
		if (volumePeriodTimer != 0) return;

		if (VolumeSweepPeriod == 0) return;

		volumePeriodTimer = VolumeSweepPeriod;

		int newVolume = currentEnvelopeVolume + (VolumeEnvelopeDirection ? 1 : -1);

		if (newVolume is >= 0 and < 16) currentEnvelopeVolume = newVolume;
	}

	public void Reset()
	{
		internalSoundLengthRegister = 0;

		VolumeEnvelopeRegister = 0;

		PolynomialCounterRegister = 0;

		internalCounterConsecutiveRegister = 0;

		frequencyTimer = 0;

		currentFrameSequencerTick = 0;

		lengthTimer = 0;

		currentEnvelopeVolume = 0;
		volumePeriodTimer     = 0;

		shiftRegister = 0;
	}

	public short GetCurrentAmplitudeLeft()
	{
		if (!_spu.Enabled || !LeftEnabled) return 0;

		double volume = currentEnvelopeVolume * _spu.LeftChannelVolume * SoundProcessor.VOLUME_MULTIPLIER;

		return (short)((Bits.Is(shiftRegister, 0) ? -1 : 1) * volume);
	}

	public short GetCurrentAmplitudeRight()
	{
		if (!_spu.Enabled || !RightEnabled) return 0;

		double volume = currentEnvelopeVolume * _spu.RightChannelVolume * SoundProcessor.VOLUME_MULTIPLIER;

		return (short)((Bits.Is(shiftRegister, 0) ? -1 : 1) * volume);
	}
}