﻿namespace NexusGB.GameBoy.GamePaks;

public sealed class MemoryController2 : IGamePak
{
    private const int ROM_OFFSET = 0x4000;

    private readonly byte[] _rom;
    private readonly byte[] _eram;

    private bool eramEnabled;
    private int romBank;

    public MemoryController2(byte[] rom)
    {
        _rom = rom;

        _eram = new byte[512];
    }

    public void LoadSave(byte[] eram) => Buffer.BlockCopy(eram, 0, _eram, 0, eram.Length);

    public byte ReadERAM(in ushort address)
        => eramEnabled ? _eram[address & 0x1FFF] : (byte)0xFF;

    public byte ReadHighROM(in ushort address) => _rom[ROM_OFFSET * romBank + (address & 0x3FFF)];

    public byte ReadLowROM(in ushort address) => _rom[address];

    public void WriteERAM(in ushort address, in byte value)
    {
        if (eramEnabled) _eram[address & 0x1FFF] = value;
    }

    public void WriteROM(in ushort address, in byte value)
    {
        switch (address)
        {
            case <= 0x1FFF: eramEnabled = (value & 0x01) == 0x00; break;
            case <= 0x3FFF: romBank = value & 0x0F; break;
        }
    }

    public void SaveTo(string filepath) => File.WriteAllBytes(filepath, _eram);
}