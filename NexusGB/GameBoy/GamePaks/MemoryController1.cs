namespace NexusGB.GameBoy.GamePaks;

public sealed class MemoryController1 : IGamePak
{
    private const int ROM_OFFSET = 0x4000;
    private const int ERAM_OFFSET = 0x2000;

    private readonly byte[] _rom;
    private readonly byte[] _eram;

    private bool eramEnabled;
    private int romBank;
    private int ramBank;
    private int bankingMode;

    public MemoryController1(byte[] rom)
    {
        _rom = rom;

        _eram = new byte[8192];

        romBank = 1;
    }

    public byte ReadERAM(in ushort address)
        => eramEnabled ? _eram[ERAM_OFFSET * ramBank + (address & 0x1FFF)] : (byte)0xFF;

    public byte ReadHighROM(in ushort address) => _rom[ROM_OFFSET * romBank + (address & 0x3FFF)];

    public byte ReadLowROM(in ushort address) => _rom[address];

    public void WriteERAM(in ushort address, in byte value)
    {
        if (eramEnabled) _eram[ERAM_OFFSET * ramBank + (address & 0x1FFF)] = value;
    }

    public void WriteROM(in ushort address, in byte value)
    {
        switch (address)
        {
            case < 0x2000: eramEnabled = value == 0x0A; break;

            case < 0x4000:
                romBank = value & 0x1F;
                if (romBank is 0x00 or 0x20 or 0x40 or 0x60) romBank++;
                break;

            case < 0x6000:
                if (bankingMode == 0)
                {
                    romBank |= value & 0x03;
                    if (romBank is 0x00 or 0x20 or 0x40 or 0x60) romBank++;
                }
                else ramBank = value & 0x03;
                break;

            case <= 0x8000: bankingMode = value & 0x01; break;
        }
    }
}