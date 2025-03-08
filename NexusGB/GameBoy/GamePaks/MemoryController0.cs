namespace NexusGB.GameBoy.GamePaks;

public sealed class MemoryController0 : IGamePak
{
    private readonly byte[] _rom;

    public MemoryController0(byte[] rom) => _rom = rom;

    public byte ReadERAM(in ushort address) => 0xFF;
    public byte ReadHighROM(in ushort address) => _rom[address];
    public byte ReadLowROM(in ushort address) => _rom[address];
    public void WriteERAM(in ushort address, in byte value) { }
    public void WriteROM(in ushort address, in byte value) { }
}