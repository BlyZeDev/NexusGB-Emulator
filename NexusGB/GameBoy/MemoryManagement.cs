﻿namespace NexusGB.GameBoy;

using NexusGB.GameBoy.GamePaks;
using System.Runtime.CompilerServices;
using System.Text;

public sealed class MemoryManagement
{
    private readonly IGamePak _gamepak;
    private readonly SoundProcessor _spu;

    private readonly bool _supportsSaving;

    private readonly byte[] _vram;
    private readonly byte[] _wram0;
    private readonly byte[] _wram1;
    private readonly byte[] _oam;
    private readonly byte[] _io;
    private readonly byte[] _hram;

    public ref byte Divider => ref _io[0x04];
    public ref byte TimerCounter => ref _io[0x05];
    public ref byte TimerModulo => ref _io[0x06];
    public bool TimerControlEnabled => (_io[0x07] & 4) != 0;
    public byte TimerControlFrequency => (byte)(_io[0x07] & 3);

    public ref byte InterruptEnable => ref _hram[0x7F];
    public ref byte InterruptFlag => ref _io[0x0F];

    public byte LCDControl => _io[0x40];
    public ref byte LCDControlStatus => ref _io[0x41];

    public byte ScrollY => _io[0x42];
    public byte ScrollX => _io[0x43];
    public ref byte LCDControlY => ref _io[0x44];
    public byte LYCompare => _io[0x45];

    public byte BackgroundPalette => _io[0x47];
    public byte ObjectPalette0 => _io[0x48];
    public byte ObjectPalette1 => _io[0x49];

    public byte WindowY => _io[0x4A];
    public byte WindowX => _io[0x4B];

    public ref byte JoystickPad => ref _io[0x00];

    public string GameTitle { get; }

    private MemoryManagement(IGamePak gamepak, byte[]? eram, SoundProcessor spu, string title)
    {
        _gamepak = gamepak;
        _spu = spu;
        _supportsSaving = eram is not null;

        if (_supportsSaving) _gamepak.LoadSave(eram!);

        GameTitle = title;

        _vram = new byte[8192];
        _wram0 = new byte[4096];
        _wram1 = new byte[4096];
        _oam = new byte[160];
        _io = new byte[128];
        _hram = new byte[128];

        _io[0x4D] = 0xFF;

        _io[0x10] = 0x80;
        _io[0x11] = 0xBF;
        _io[0x12] = 0xF3;
        _io[0x14] = 0xBF;
        _io[0x16] = 0x3F;
        _io[0x19] = 0xBF;
        _io[0x1A] = 0x7F;
        _io[0x1B] = 0xFF;
        _io[0x1C] = 0x9F;
        _io[0x1E] = 0xBF;
        _io[0x20] = 0xFF;
        _io[0x23] = 0xBF;
        _io[0x24] = 0x77;
        _io[0x25] = 0xF3;
        _io[0x26] = 0xF1;
        _io[0x40] = 0x91;
        _io[0x47] = 0xFC;
        _io[0x48] = 0xFF;
        _io[0x49] = 0xFF;
    }

    public byte ReadByte(in ushort address)
    {
        return address switch
        {
            <= 0x3FFF => _gamepak.ReadLowROM(address),
            <= 0x7FFF => _gamepak.ReadHighROM(address),
            <= 0x9FFF => _vram[address & 0x1FFF],
            <= 0xBFFF => _gamepak.ReadERAM(address),
            <= 0xCFFF => _wram0[address & 0xFFF],
            <= 0xDFFF => _wram1[address & 0xFFF],
            <= 0xEFFF => _wram0[address & 0xFFF],
            <= 0xFDFF => _wram1[address & 0xFFF],
            <= 0xFE9F => _oam[address - 0xFE00],
            <= 0xFEFF => 0xFF,
            >= 0xFF10 and < 0xFF40 => _spu.ReadByte(address),
            <= 0xFF7F => _io[address & 0x7F],
            _ => _hram[address & 0x7F]
        };
    }

    public void WriteByte(in ushort address, in byte value)
    {
        switch (address)
        {
            case <= 0x7FFF: _gamepak.WriteROM(address, value); break;
            case <= 0x9FFF: _vram[address & 0x1FFF] = value; break;
            case <= 0xBFFF: _gamepak.WriteERAM(address, value); break;
            case <= 0xCFFF: _wram0[address & 0xFFF] = value; break;
            case <= 0xDFFF: _wram1[address & 0xFFF] = value; break;
            case <= 0xEFFF: _wram0[address & 0xFFF] = value; break;
            case <= 0xFDFF: _wram1[address & 0xFFF] = value; break;
            case <= 0xFE9F: _oam[address - 0xFE00] = value; break;
            case <= 0xFEFF: break;
            case >= 0xFF10 and < 0xFF40: _spu.WriteByte(address, value); break;
            case <= 0xFF7F:
                _io[address & 0x7F] = (byte)(address switch
                {
                    0xFF0F => value | 0xE0,
                    0xFF04 or 0xFF44 => 0,
                    0xFF46 => DirectMemoryAccess(value),
                    _ => value
                });
                break;
            default: _hram[address & 0x7F] = value; break;
        }
    }

    public ushort ReadWord(in ushort address)
        => (ushort)(ReadByte((ushort)(address + 1)) << 8 | ReadByte(address));

    public void WriteWord(in ushort address, in ushort word)
    {
        WriteByte((ushort)(address + 1), (byte)(word >> 8));
        WriteByte(address, (byte)word);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void RequestInterrupt(in byte value) => Bits.Set(ref InterruptFlag, value);

    public void SaveGame(string filepath)
    {
        if (_supportsSaving) _gamepak.SaveTo(filepath);
    }

    private byte DirectMemoryAccess(in byte value)
    {
        var address = (ushort)(value << 8);

        for (byte i = 0; i < _oam.Length; i++)
        {
            _oam[i] = ReadByte((ushort)(address + i));
        }

        return value;
    }

    public static MemoryManagement LoadGamePak(string filepath, string saveFilepath, SoundProcessor spu)
    {
        var rom = File.ReadAllBytes(filepath);
        var romType = rom[0x147];

        var romSave = romType is 0x03 or 0x06 or 0x0D or 0x10 or 0x13 or 0x1B or 0x1E ? Array.Empty<byte>() : null;
        if (File.Exists(saveFilepath)) romSave = File.ReadAllBytes(saveFilepath);

        Span<byte> title = stackalloc byte[16];
        rom.AsSpan(0x134, title.Length).CopyTo(title);

        return new MemoryManagement(romType switch
        {
            0x00 => new MemoryController0(rom),
            0x01 or 0x02 or 0x03 => new MemoryController1(rom),
            0x05 or 0x06 => new MemoryController2(rom),
            0x0F or 0x10 or 0x11 or 0x12 or 0x13 => new MemoryController3(rom),
            0x19 or 0x1A or 0x1B or 0x1C or 0x1D or 0x1E => new MemoryController5(rom),
            _ => throw new NotSupportedException($"MBC not supported: {rom[0x147]:X2}")
        }, romSave, spu, Encoding.ASCII.GetString(title));
    }
}