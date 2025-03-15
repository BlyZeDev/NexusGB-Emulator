namespace NexusGB.GameBoy.GamePaks;

public sealed class MemoryController3 : IGamePak
{
    private const int ROM_OFFSET = 0x4000;
    private const int ERAM_OFFSET = 0x2000;

    private readonly byte[] _rom;
    private readonly byte[] _eram;

    private bool eramEnabled;
    private int romBank;
    private int ramBank;

    private byte secondsRTC;
    private byte minutesRTC;
    private byte hoursRTC;
    private byte dayCounterLowRTC;
    private byte dayCounterHighRTC;

    public MemoryController3(byte[] rom)
    {
        _rom = rom;

        _eram = new byte[32_768];

        romBank = 1;
    }

    public byte ReadERAM(in ushort address)
    {
        if (!eramEnabled) return 0xFF;

        return ramBank switch
        {
            <= 0x03 => _eram[ERAM_OFFSET * ramBank + (address & 0x1FFF)],
            0x08 => secondsRTC,
            0x09 => minutesRTC,
            0x0A => hoursRTC,
            0x0B => dayCounterLowRTC,
            0x0C => dayCounterHighRTC,
            _ => 0xFF,
        };
    }

    public byte ReadHighROM(in ushort address) => _rom[ROM_OFFSET * romBank + (address & 0x3FFF)];

    public byte ReadLowROM(in ushort address) => _rom[address];

    public void WriteERAM(in ushort address, in byte value)
    {
        if (eramEnabled)
        {
            switch (ramBank)
            {
                case 0x00 or 0x01 or 0x02 or 0x03: _eram[ERAM_OFFSET * ramBank + (address & 0x1FFF)] = value; break;
                case 0x08: secondsRTC = value; break;
                case 0x09: minutesRTC = value; break;
                case 0x0A: hoursRTC = value; break;
                case 0x0B: dayCounterLowRTC = value; break;
                case 0x0C: dayCounterHighRTC = value; break;
            }
        }
    }

    public void WriteROM(in ushort address, in byte value)
    {
        switch (address)
        {
            case <= 0x1FFF: eramEnabled = value == 0x0A; break;

            case <= 0x3FFF:
                romBank = value & 0x7F;
                if (romBank == 0x00) romBank++;
                break;

            case <= 0x5FFF:
                if (value <= 0x03 || value >= 0x08 && value <= 0xC0) ramBank = value;
                break;

            case <= 0x7FFF:
                var now = DateTime.Now;
                secondsRTC = (byte)now.Second;
                minutesRTC = (byte)now.Minute;
                hoursRTC = (byte)now.Hour;
                break;
        }
    }
}