namespace NexusGB.GameBoy.SoundChannels;

public class ApuChannel2
{
	private byte internalSoundLengthWavePatternRegister;

	//NR21
	public byte SoundLengthWavePatternRegister
	{
		get => (byte)(internalSoundLengthWavePatternRegister | 0b0011_1111);
		set
		{
			internalSoundLengthWavePatternRegister = value;
			SoundLengthWritten();
		}
	}

	//NR22
	public byte VolumeEnvelopeRegister { get; set; }

	//NR23
	public byte FrequencyRegisterLo { get; set; }

	private byte internalFrequencyRegisterHi;

	//NR24
	public byte FrequencyRegisterHi
	{
		get => (byte)(internalFrequencyRegisterHi | 0b1011_1111);
		set
		{
			internalFrequencyRegisterHi = (byte)(value & 0b1100_0111);
			TriggerWritten();
		}
	}

	private byte SoundLength     => (byte)(internalSoundLengthWavePatternRegister & 0b0011_1111);
	private byte WavePatternDuty => (byte)((internalSoundLengthWavePatternRegister & 0b1100_0000) >> 6);

	private byte InitialVolume           => (byte)((VolumeEnvelopeRegister & 0b1111_0000) >> 4);
	private bool VolumeEnvelopeDirection => Bits.Is(VolumeEnvelopeRegister, 3);
	private byte VolumeSweepPeriod       => (byte)(VolumeEnvelopeRegister & 0b0000_0111);

	private bool Trigger => Bits.Is(internalFrequencyRegisterHi, 7);

	private bool EnableLength => Bits.Is(internalFrequencyRegisterHi, 6);

	//Only the lower 3 bits of internalFrequencyRegisterHi are used
	private ushort FrequencyRegister =>
		(ushort)(Bits.MakeWord(internalFrequencyRegisterHi, FrequencyRegisterLo) & 0x7FF);

	private bool LeftEnabled  => Bits.Is(_spu.SoundOutputTerminalSelectRegister, 5);
	private bool RightEnabled => Bits.Is(_spu.SoundOutputTerminalSelectRegister, 1);

	private int frequencyTimer;

	private int waveDutyPosition;

	private int currentFrameSequencerTick;

	private int lengthTimer;

	private int currentEnvelopeVolume;
	private int volumePeriodTimer;

	public bool Playing { get; private set; }

	private readonly SoundProcessor _spu;

	public ApuChannel2(SoundProcessor spu)
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

		frequencyTimer += (2048 - FrequencyRegister) * 4;

		waveDutyPosition++;
		waveDutyPosition %= 8;
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

		CheckDacEnabled();
	}

	private void CheckDacEnabled()
	{
		bool dacEnabled          = InitialVolume != 0 || VolumeEnvelopeDirection;
		if (!dacEnabled) Playing = false;
	}

	public void Reset()
	{
		internalSoundLengthWavePatternRegister = 0;

		VolumeEnvelopeRegister = 0;

		FrequencyRegisterLo = 0;

		internalFrequencyRegisterHi = 0;

		frequencyTimer = 0;

		waveDutyPosition = 0;

		currentFrameSequencerTick = 0;

		lengthTimer = 0;

		currentEnvelopeVolume = 0;
		volumePeriodTimer     = 0;
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

	public short GetCurrentAmplitudeLeft()
	{
		if (!_spu.Enabled || !LeftEnabled) return 0;

		double volume = currentEnvelopeVolume * _spu.LeftChannelVolume * SoundProcessor.VOLUME_MULTIPLIER;

		return (short)(SoundProcessor.WAVE_DUTY_TABLE[WavePatternDuty, waveDutyPosition] * volume);
	}

	public short GetCurrentAmplitudeRight()
	{
		if (!_spu.Enabled || !RightEnabled) return 0;

		double volume = currentEnvelopeVolume * _spu.RightChannelVolume * SoundProcessor.VOLUME_MULTIPLIER;

		return (short)(SoundProcessor.WAVE_DUTY_TABLE[WavePatternDuty, waveDutyPosition] * volume);
	}
}