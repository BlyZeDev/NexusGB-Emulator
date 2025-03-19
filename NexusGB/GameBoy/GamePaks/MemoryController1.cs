namespace NexusGB.GameBoy.GamePaks;

using static System.Runtime.InteropServices.JavaScript.JSType;

public sealed class MemoryController1 : IGamePak
{
    private const int ROM_OFFSET = 0x4000;
    private const int ERAM_OFFSET = 0x2000;

    private readonly byte[] _rom;
    private readonly byte[] _eram;

    private readonly int _amountRomBanks;
    private readonly int _amountRamBanks;

    private bool eramEnabled;
    private byte romBankLower;
    private byte romBankUpper;
    private int ramBank;
    private int bankingMode;

    private byte RomBank
    {
        get => (byte)(((romBankUpper & 0b0000_0011) << 5) | (romBankLower & 0b0001_1111));
        init
        {
            romBankLower = (byte)(value & 0b0001_1111);
            romBankUpper = (byte)((value & 0b0110_0000) >> 5);
        }
    }

    public MemoryController1(byte[] rom)
    {
        _rom = rom;

        _amountRomBanks = (int)Math.Pow(2, rom[0x148] + 1);
        _amountRamBanks = rom[0x149] switch
        {
            0x00 or 0x01 => 0,
            0x02 => 1,
            0x03 => 4,
            0x04 => 16,
            0x05 => 8,
            _ => throw new NotSupportedException("Unsupported amount of RAM Banks")
        };

        _eram = new byte[32_768];

        RomBank = 1;
    }

    public byte ReadERAM(in ushort address)
        => eramEnabled ? _eram[ERAM_OFFSET * ramBank + (address & 0x1FFF)] : (byte)0xFF;

    public byte ReadHighROM(in ushort address) => _rom[ROM_OFFSET * (byte)(RomBank & (_amountRomBanks - 1)) + (address & 0x3FFF)];

    public byte ReadLowROM(in ushort address) => _rom[address];

    public void WriteERAM(in ushort address, in byte value)
    {
        if (eramEnabled) _eram[ERAM_OFFSET * ramBank + (address & 0x1FFF)] = value;
    }

    public void WriteROM(in ushort address, in byte value)
    {
        switch (address)
        {
            case < 0x2000: eramEnabled = (value & 0x0F) == 0x0A; break;

            case < 0x4000: romBankLower = (byte)(value & 0b0001_1111); break;

            case < 0x6000:
                if (_amountRomBanks >= 64) romBankUpper = (byte)(value & 0b0000_0011);

                if (bankingMode == 1 && _amountRamBanks >= 4) ramBank = (byte)(value & 0b0000_0011);
                break;

            case <= 0x8000: bankingMode = value & 0x01; break;
        }
    }
}