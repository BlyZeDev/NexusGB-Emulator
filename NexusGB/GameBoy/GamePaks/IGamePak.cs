namespace NexusGB.GameBoy.GamePaks;

public interface IGamePak : IDisposable
{
    public byte ReadLowROM(in ushort address);
    public byte ReadHighROM(in ushort address);
    public void WriteROM(in ushort address, in byte value);
    public byte ReadERAM(in ushort address);
    public void WriteERAM(in ushort address, in byte value);
}