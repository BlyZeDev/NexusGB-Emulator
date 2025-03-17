namespace NexusGB.GameBoy.SoundChannels;

public class ApuChannel3
{
	private bool internalSoundOnOffRegister;

	//NR30
	public byte SoundOnOffRegister
	{
		get => (byte)(internalSoundOnOffRegister ? 0b1000_0000 : 0b0000_0000);
		set => internalSoundOnOffRegister = (value & 0b1000_0000) != 0;
	}

	private byte internalSoundLengthRegister;

	//NR31
	public byte SoundLengthRegister
	{
		get => internalSoundLengthRegister;
		set
		{
			internalSoundLengthRegister = value;
			SoundLengthWritten();
		}
	}

	private byte internalSelectOutputLevelRegister;

	//NR32
	public byte SelectOutputLevelRegister
	{
		get => (byte)(internalSelectOutputLevelRegister & 0b0110_0000);
		set => internalSelectOutputLevelRegister = (byte)(value & 0b0110_0000);
	}

	//NR33
	public byte FrequencyRegisterLo { get; set; }

	private byte internalFrequencyRegisterHi;

	//NR34
	public byte FrequencyRegisterHi
	{
		get => (byte)(internalFrequencyRegisterHi | 0b1011_1111);
		set
		{
			internalFrequencyRegisterHi = (byte)(value & 0b1100_0111);
			TriggerWritten();
		}
	}

	//Only the lower 3 bits of internalFrequencyRegisterHi are used
	private ushort FrequencyRegister =>
		(ushort)(Bits.MakeWord(internalFrequencyRegisterHi, FrequencyRegisterLo) & 0x7FF);

	private byte OutputLevel => (byte)((SelectOutputLevelRegister & 0b0110_0000) >> 5);

	private bool Trigger => Bits.Is(internalFrequencyRegisterHi, 7);

	private bool EnableLength => Bits.Is(internalFrequencyRegisterHi, 6);

	private int CurrentVolumeShiftAmount
	{
		get
		{
            return OutputLevel switch
            {
                0b00 => 4,
                0b01 => 0,
                0b10 => 1,
                0b11 => 2,
                _ => 0,
            };
        }
	}

	private bool LeftEnabled  => Bits.Is(_spu.SoundOutputTerminalSelectRegister, 6);
	private bool RightEnabled => Bits.Is(_spu.SoundOutputTerminalSelectRegister, 2);

	private int frequencyTimer;

	private int waveRamPosition;

	private int currentFrameSequencerTick;

	private int lengthTimer;

	public bool Playing { get; private set; }

	private readonly SoundProcessor _spu;

	private readonly byte[] waveRam;

	public ApuChannel3(SoundProcessor spu)
	{
		_spu = spu;

		waveRam = new byte[0x10];
	}

	public void Update(int cycles)
	{
		CheckDacEnabled();

		if (!Playing) return;

		if (_spu.ShouldTickFrameSequencer) TickFrameSequencer();

		frequencyTimer -= cycles;
		if (frequencyTimer > 0) return;

		frequencyTimer += (2048 - FrequencyRegister) * 2;

		waveRamPosition++;
		waveRamPosition %= 0x20;
	}

	private void SoundLengthWritten()
	{
		lengthTimer = 256 - SoundLengthRegister;
	}

	private void TriggerWritten()
	{
		if (!_spu.Enabled || !Trigger) return;

		Playing = true;

		if (lengthTimer == 0) lengthTimer = 256;

		CheckDacEnabled();
	}

	private void CheckDacEnabled()
	{
		if (!internalSoundOnOffRegister) Playing = false;
	}

	public void Reset()
	{
		internalSoundOnOffRegister = false;

		internalSoundLengthRegister = 0;

		internalSelectOutputLevelRegister = 0;

		FrequencyRegisterLo = 0;

		internalFrequencyRegisterHi = 0;
	}

	private void TickFrameSequencer()
	{
		if (currentFrameSequencerTick % 2 == 0) UpdateLength();

		currentFrameSequencerTick++;
		currentFrameSequencerTick %= 8;
	}

	private void UpdateLength()
	{
		if (!EnableLength) return;

		if (lengthTimer <= 0 || --lengthTimer != 0) return;

		Playing = false;
	}

	private sbyte GetWaveRamSample(int index)
	{
		byte samplePair = waveRam[index / 2];

		int numberOfSample = index % 2;

		byte sample;
		if (numberOfSample == 0)
		{
			sample =   (byte)((samplePair & 0b1111_0000) >> 4);
			sample >>= CurrentVolumeShiftAmount;

			return (sbyte)(sample - 8);
		}

		sample =   (byte)(samplePair & 0b0000_1111);
		sample >>= CurrentVolumeShiftAmount;

		return (sbyte)(sample - 8);
	}

	public byte GetWaveRamSamplePair(int index)
	{
		//TODO implement actual behaviour for CH3 enabled
		return Playing ? (byte)0xFF : waveRam[index];
	}

	public void SetWaveRamSamplePair(int index, byte data)
	{
		//TODO implement actual behaviour for CH3 enabled
		if (Playing) return;

		waveRam[index] = data;
	}

	public short GetCurrentAmplitudeLeft()
	{
		if (!_spu.Enabled || !LeftEnabled) return 0;

		double volume = _spu.LeftChannelVolume * SoundProcessor.VOLUME_MULTIPLIER;

		return (short)(GetWaveRamSample(waveRamPosition) * volume);
	}

	public short GetCurrentAmplitudeRight()
	{
		if (!_spu.Enabled || !RightEnabled) return 0;

		double volume = _spu.RightChannelVolume * SoundProcessor.VOLUME_MULTIPLIER;

		//Console.WriteLine($"{(short)(GetWaveRamSample(waveRamPosition) * volume)}");

		return (short)(GetWaveRamSample(waveRamPosition) * volume);
	}
}