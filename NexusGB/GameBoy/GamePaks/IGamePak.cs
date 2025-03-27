namespace NexusGB.GameBoy.GamePaks;

public interface IGamePak
{
    public void LoadSave(byte[] eram);

    public byte ReadLowROM(in ushort address);
    public byte ReadHighROM(in ushort address);
    public void WriteROM(in ushort address, in byte value);
    public byte ReadERAM(in ushort address);
    public void WriteERAM(in ushort address, in byte value);

    public void SaveTo(string filepath);
}