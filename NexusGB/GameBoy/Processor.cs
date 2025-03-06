namespace NexusGB.GameBoy;

using System.Collections.Immutable;

public sealed class Processor
{
    private const int JUMP_RELATIVE_TRUE = 12;
    private const int JUMP_RELATIVE_FALSE = 8;
    private const int JUMP_TRUE = 16;
    private const int JUMP_FALSE = 12;
    private const int RETURN_TRUE = 20;
    private const int RETURN_FALSE = 8;
    private const int CALL_TRUE = 24;
    private const int CALL_FALSE = 12;

    private static readonly ImmutableArray<int> _cyclesValues =
    [
        4, 12,  8,  8,  4,  4,  8,  4, 20,  8,  8,  8,  4,  4,  8,  4,
	    4, 12,  8,  8,  4,  4,  8,  4,  0,  8,  8,  8,  4,  4,  8,  4,
        0, 12,  8,  8,  4,  4,  8,  4,  0,  8,  8,  8,  4,  4,  8,  4,
        0, 12,  8,  8, 12, 12, 12,  4,  0,  8,  8,  8,  4,  4,  8,  4,

        4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4,
	    4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4,
        4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4,
        8,  8,  8,  8,  8,  8,  4,  8,  4,  4,  4,  4,  4,  4,  8,  4,

        4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4,
	    4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4,
        4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4,
        4,  4,  4,  4,  4,  4,  8,  4,  4,  4,  4,  4,  4,  4,  8,  4,

        0,  12,  0,  0,  0, 16, 8, 16,  0, -4,  0,  0,  0,  0,  8, 16,
	    0,  12,  0, 00,  0, 16, 8, 16,  0, -4,  0, 00,  0, 00,  8, 16,
        12, 12,  8, 00, 00, 16, 8, 16, 16,  4, 16, 00, 00, 00,  8, 16,
        12, 12,  8,  4, 00, 16, 8, 16, 12,  8, 16,  4, 00, 00,  8, 16
    ];

    private static readonly ImmutableArray<int> _cyclesFixedValues =
    [
        8,  8,  8,  8,  8,  8, 16,  8,  8,  8,  8,  8,  8,  8, 16,  8,
        8,  8,  8,  8,  8,  8, 16,  8,  8,  8,  8,  8,  8,  8, 16,  8,
        8,  8,  8,  8,  8,  8, 16,  8,  8,  8,  8,  8,  8,  8, 16,  8,
        8,  8,  8,  8,  8,  8, 16,  8,  8,  8,  8,  8,  8,  8, 16,  8,
        
        8,  8,  8,  8,  8,  8, 12,  8,  8,  8,  8,  8,  8,  8, 12,  8,
        8,  8,  8,  8,  8,  8, 12,  8,  8,  8,  8,  8,  8,  8, 12,  8,
        8,  8,  8,  8,  8,  8, 12,  8,  8,  8,  8,  8,  8,  8, 12,  8,
        8,  8,  8,  8,  8,  8, 12,  8,  8,  8,  8,  8,  8,  8, 12,  8,
        
        8,  8,  8,  8,  8,  8, 16,  8,  8,  8,  8,  8,  8,  8, 16,  8,
        8,  8,  8,  8,  8,  8, 16,  8,  8,  8,  8,  8,  8,  8, 16,  8,
        8,  8,  8,  8,  8,  8, 16,  8,  8,  8,  8,  8,  8,  8, 16,  8,
        8,  8,  8,  8,  8,  8, 16,  8,  8,  8,  8,  8,  8,  8, 16,  8,

        8,  8,  8,  8,  8,  8, 16,  8,  8,  8,  8,  8,  8,  8, 16,  8,
        8,  8,  8,  8,  8,  8, 16,  8,  8,  8,  8,  8,  8,  8, 16,  8,
        8,  8,  8,  8,  8,  8, 16,  8,  8,  8,  8,  8,  8,  8, 16,  8,
        8,  8,  8,  8,  8,  8, 16,  8,  8,  8,  8,  8,  8,  8, 16,  8
    ];

    private readonly MemoryManagement _mmu;

    private ushort programCounter;
    private ushort stackPointer;

    private byte regA, regB, regC, regD, regE, regF, regH, regL;

    private ushort AF
    {
        get => (ushort)(regA << 8 | regF);
        set
        {
            regA = (byte)(value >> 8);
            regF = (byte)(value & 0xF0);
        }
    }

    private ushort BC
    {
        get => (ushort)(regB << 8 | regC);
        set
        {
            regB = (byte)(value >> 8);
            regC = (byte)value;
        }
    }

    private ushort DE
    {
        get => (ushort)(regD << 8 | regE);
        set
        {
            regD = (byte)(value >> 8);
            regE = (byte)value;
        }
    }

    private ushort HL
    {
        get => (ushort)(regH << 8 | regL);
        set
        {
            regH = (byte)(value >> 8);
            regL = (byte)value;
        }
    }

    private bool FlagZ
    {
        get => (regF & 0x80) != 0;
        set => regF = value ? (byte)(regF | 0x80) : (byte)(regF & ~0x80);
    }

    private bool FlagN
    {
        get => (regF & 0x40) != 0;
        set => regF = value ? (byte)(regF | 0x40) : (byte)(regF & ~0x40);
    }

    private bool FlagH
    {
        get => (regF & 0x20) != 0;
        set => regF = value ? (byte)(regF | 0x20) : (byte)(regF & ~0x20);
    }

    private bool FlagC
    {
        get => (regF & 0x10) != 0;
        set => regF = value ? (byte)(regF | 0x10) : (byte)(regF & ~0x10);
    }

    private bool ime;
    private bool imeEnabler;
    private bool halted;
    private bool haltBug;
    private int cycles;

    public Processor(MemoryManagement mmu)
    {
        _mmu = mmu;

        AF = 0x01B0;
        BC = 0x0013;
        DE = 0x00D8;
        HL = 0x014d;
        stackPointer = 0xFFFE;
        programCounter = 0x100;
    }

    public int Execute()
    {
        var opCode = _mmu.ReadByte(programCounter++);
        if (haltBug)
        {
            programCounter--;
            haltBug = false;
        }

        cycles = 0;

        switch (opCode)
        {
            case 0x00: break;
            case 0x01: BC = _mmu.ReadWord(programCounter += 2); break;
            case 0x02: _mmu.WriteByte(BC, regA); break;
            case 0x03: BC++; break;
            case 0x04: Increment(ref regB); break;
            case 0x05: Decrement(ref regB); break;
            case 0x06: regB = _mmu.ReadByte(programCounter++); break;
            case 0x07:
                regF = 0;
                FlagC = (regA & 0x80) != 0;
                regA = (byte)((regA << 1) | (regA >> 7));
                break;
            case 0x08: _mmu.WriteWord(_mmu.ReadWord(programCounter += 2), stackPointer); break;
            case 0x09: DoubleAdd(BC); break;
            case 0x0A: regA = _mmu.ReadByte(BC); break;
            case 0x0B: BC--; break;
            case 0x0C: Increment(ref regC); break;
            case 0x0D: Decrement(ref regC); break;
            case 0x0E: regC = _mmu.ReadByte(programCounter++); break;
            case 0x0F:
                regF = 0;
                FlagC = (regA & 0x01) != 0;
                regA = (byte)((regA >> 1) | (regA << 7));
                break;

            case 0x10: Stop(); break;
            case 0x11: DE = _mmu.ReadWord(programCounter += 2); break;
            case 0x12: _mmu.WriteByte(DE, regA); break;
            case 0x13: DE++; break;
            case 0x14: Increment(ref regD); break;
            case 0x15: Decrement(ref regD); break;
            case 0x16: regD = _mmu.ReadByte(programCounter++); break;
            case 0x17:
                {
                    var prevC = FlagC ? 1 : 0;
                    regF = 0;
                    FlagC = (regA & 0x80) != 0;
                    regA = (byte)((regA << 1) | prevC);
                }
                break;
            case 0x18: JumpRelative(true); break;
            case 0x19: DoubleAdd(DE); break;
            case 0x1A: regA = _mmu.ReadByte(DE); break;
            case 0x1B: DE--; break;
            case 0x1C: Increment(ref regE); break;
            case 0x1D: Decrement(ref regE); break;
            case 0x1E: regE = _mmu.ReadByte(programCounter++); break;
            case 0x1F:
                {
                    var prevC = FlagC ? 0x80 : 0x00;
                    regF = 0;
                    FlagC = (regA & 0x01) != 0;
                    regA = (byte)((regA >> 1) | prevC);
                }
                break;

            case 0x20: JumpRelative(!FlagZ); break;
            case 0x21: HL = _mmu.ReadWord(programCounter += 2); break;
            case 0x22: _mmu.WriteByte(HL++, regA); break;
            case 0x23: HL++; break;
            case 0x24: Increment(ref regH); break;
            case 0x25: Decrement(ref regH); break;
            case 0x26: regH = _mmu.ReadByte(programCounter++); break;
            case 0x27:
                if (FlagN)
                {
                    if (FlagC) regA -= 0x60;
                    if (FlagH) regA -= 0x06;
                }
                else
                {
                    if (FlagC || regA > 0x99)
                    {
                        regA += 0x60;
                        FlagC = true;
                    }
                    if (FlagH || (regA & 0x0F) > 0x09) regA += 0x06;
                }
                break;
            case 0x28: JumpRelative(FlagZ); break;
            case 0x29: DoubleAdd(HL); break;
            case 0x2A: regA = _mmu.ReadByte(HL++); break;
            case 0x2B: HL--; break;
            case 0x2C: Increment(ref regL); break;
            case 0x2D: Decrement(ref regL); break;
            case 0x2E: regL = _mmu.ReadByte(programCounter++); break;
            case 0x2F:
                regA = (byte)~regA;
                FlagN = true;
                FlagH = true;
                break;

            case 0x30: JumpRelative(!FlagC); break;
            case 0x31: stackPointer = _mmu.ReadWord(programCounter += 2); break;
            case 0x32: _mmu.WriteByte(HL--, regA); break;
            case 0x33: stackPointer++; break;
            case 0x34:
                {
                    var read = _mmu.ReadByte(HL);
                    Increment(ref read);
                    _mmu.WriteByte(HL, read);
                }
                break;
            case 0x35:
                {
                    var read = _mmu.ReadByte(HL);
                    Decrement(ref read);
                    _mmu.WriteByte(HL, read);
                }
                break;
            case 0x36: _mmu.WriteByte(HL, _mmu.ReadByte(programCounter++)); break;
            case 0x37:
                FlagC = true;
                FlagN = false;
                FlagH = false;
                break;
            case 0x38: JumpRelative(FlagC); break;
            case 0x39: DoubleAdd(stackPointer); break;
            case 0x3A: regA = _mmu.ReadByte(HL--); break;
            case 0x3B: stackPointer--; break;
            case 0x3C: Increment(ref regA); break;
            case 0x3D: Decrement(ref regA); break;
            case 0x3E: regA = _mmu.ReadByte(programCounter++);break;
            case 0x3F:
                FlagC = !FlagC;
                FlagN = false;
                FlagH = false;
                break;

            case 0x40: break;

            case 0x41: regB = regC; break;
            case 0x42: regB = regD; break;
            case 0x43: regB = regE; break;
            case 0x44: regB = regH; break;
            case 0x45: regB = regL; break;
            case 0x46: regB = _mmu.ReadByte(HL); break;
            case 0x47: regB = regA; break;

            case 0x48: regC = regB; break;

            case 0x49: break;

            case 0x4A: regC = regD; break;
            case 0x4B: regC = regE; break;
            case 0x4C: regC = regH; break;
            case 0x4D: regC = regL; break;
            case 0x4E: regC = _mmu.ReadByte(HL); break;
            case 0x4F: regC = regA; break;

            case 0x50: regD = regB; break;
            case 0x51: regD = regC; break;

            case 0x52: break;

            case 0x53: regD = regE; break;
            case 0x54: regD = regH; break;
            case 0x55: regD = regL; break;
            case 0x56: regD = _mmu.ReadByte(HL); break;
            case 0x57: regD = regA; break;

            case 0x58: regE = regB; break;
            case 0x59: regE = regC; break;
            case 0x5A: regE = regD; break;

            case 0x5B: break;

            case 0x5C: regE = regH; break;
            case 0x5D: regE = regL; break;
            case 0x5E: regE = _mmu.ReadByte(HL); break;
            case 0x5F: regE = regA; break;

            case 0x60: regH = regB; break;
            case 0x61: regH = regC; break;
            case 0x62: regH = regD; break;
            case 0x63: regH = regE; break;

            case 0x64: break;

            case 0x65: regH = regL; break;
            case 0x66: regH = _mmu.ReadByte(HL); break;
            case 0x67: regH = regA; break;

            case 0x68: regL = regB; break;
            case 0x69: regL = regC; break;
            case 0x6A: regL = regD; break;
            case 0x6B: regL = regE; break;
            case 0x6C: regL = regH; break;

            case 0x6D: break;

            case 0x6E: regL = _mmu.ReadByte(HL); break;
            case 0x6F: regL = regA; break;

            case 0x70: _mmu.WriteByte(HL, regB); break;
            case 0x71: _mmu.WriteByte(HL, regC); break;
            case 0x72: _mmu.WriteByte(HL, regD); break;
            case 0x73: _mmu.WriteByte(HL, regE); break;
            case 0x74: _mmu.WriteByte(HL, regH); break;
            case 0x75: _mmu.WriteByte(HL, regL); break;
            case 0x76: Halt(); break;
            case 0x77: _mmu.WriteByte(HL, regA); break;

            case 0x78: regA = regB; break;
            case 0x79: regA = regC; break;
            case 0x7A: regA = regD; break;
            case 0x7B: regA = regE; break;
            case 0x7C: regA = regH; break;
            case 0x7D: regA = regL; break;
            case 0x7E: regA = _mmu.ReadByte(HL); break;

            case 0x7F: break;

            case 0x80: Add(regB); break;
            case 0x81: Add(regC); break;
            case 0x82: Add(regD); break;
            case 0x83: Add(regE); break;
            case 0x84: Add(regH); break;
            case 0x85: Add(regL); break;
            case 0x86: Add(_mmu.ReadByte(HL)); break;
            case 0x87: Add(regA); break;

            case 0x88: AddCarry(regB); break;
            case 0x89: AddCarry(regC); break;
            case 0x8A: AddCarry(regD); break;
            case 0x8B: AddCarry(regE); break;
            case 0x8C: AddCarry(regH); break;
            case 0x8D: AddCarry(regL); break;
            case 0x8E: AddCarry(_mmu.ReadByte(HL)); break;
            case 0x8F: AddCarry(regA); break;

            case 0x90: Subtract(regB); break;
            case 0x91: Subtract(regC); break;
            case 0x92: Subtract(regD); break;
            case 0x93: Subtract(regE); break;
            case 0x94: Subtract(regH); break;
            case 0x95: Subtract(regL); break;
            case 0x96: Subtract(_mmu.ReadByte(HL)); break;
            case 0x97: Subtract(regA); break;

            case 0x98: SubtractCarry(regB); break;
            case 0x99: SubtractCarry(regC); break;
            case 0x9A: SubtractCarry(regD); break;
            case 0x9B: SubtractCarry(regE); break;
            case 0x9C: SubtractCarry(regH); break;
            case 0x9D: SubtractCarry(regL); break;
            case 0x9E: SubtractCarry(_mmu.ReadByte(HL)); break;
            case 0x9F: SubtractCarry(regA); break;

            case 0xA0: And(regB); break;
            case 0xA1: And(regC); break;
            case 0xA2: And(regD); break;
            case 0xA3: And(regE); break;
            case 0xA4: And(regH); break;
            case 0xA5: And(regL); break;
            case 0xA6: And(_mmu.ReadByte(HL)); break;
            case 0xA7: And(regA); break;

            case 0xA8: Xor(regB); break;
            case 0xA9: Xor(regC); break;
            case 0xAA: Xor(regD); break;
            case 0xAB: Xor(regE); break;
            case 0xAC: Xor(regH); break;
            case 0xAD: Xor(regL); break;
            case 0xAE: Xor(_mmu.ReadByte(HL)); break;
            case 0xAF: Xor(regA); break;

            case 0xB0: Or(regB); break;
            case 0xB1: Or(regC); break;
            case 0xB2: Or(regD); break;
            case 0xB3: Or(regE); break;
            case 0xB4: Or(regH); break;
            case 0xB5: Or(regL); break;
            case 0xB6: Or(_mmu.ReadByte(HL)); break;
            case 0xB7: Or(regA); break;

            case 0xB8: Compare(regB); break;
            case 0xB9: Compare(regC); break;
            case 0xBA: Compare(regD); break;
            case 0xBB: Compare(regE); break;
            case 0xBC: Compare(regH); break;
            case 0xBD: Compare(regL); break;
            case 0xBE: Compare(_mmu.ReadByte(HL)); break;
            case 0xBF: Compare(regA); break;

            case 0xC0: Return(!FlagZ); break;
            case 0xC1: BC = Pop(); break;
            case 0xC2: Return(!FlagZ); break;
            case 0xC3: Return(true); break;
            case 0xC4: Call(!FlagZ); break;
            case 0xC5: Push(BC); break;
            case 0xC6: Add(_mmu.ReadByte(programCounter++)); break;
            case 0xC7: Restart(0x00); break;

            case 0xC8: Return(FlagZ); break;
            case 0xC9: Return(true); break;
            case 0xCA: Jump(FlagZ); break;
            case 0xCB: PrefixCB(_mmu.ReadByte(programCounter++)); break;
            case 0xCC: Call(FlagZ); break;
            case 0xCD: Call(true); break;
            case 0xCE: AddCarry(_mmu.ReadByte(programCounter++)); break;
            case 0xCF: Restart(0x08); break;

            case 0xD0: Return(!FlagC); break;
            case 0xD1: DE = Pop(); break;
            case 0xD2: Jump(!FlagC); break;
            case 0xD4: Call(!FlagC); break;
            case 0xD5: Push(DE); break;
            case 0xD6: Subtract(_mmu.ReadByte(programCounter++)); break;
            case 0xD7: Restart(0x10); break;

            case 0xD8: Return(FlagC); break;
            case 0xD9: Return(true); ime = true; break;
            case 0xDA: Jump(FlagC); break;
            case 0xDC: Call(FlagC); break;
            case 0xDE: SubtractCarry(_mmu.ReadByte(programCounter++)); break;
            case 0xDF: Restart(0x18); break;

            case 0xE0: _mmu.WriteByte((ushort)(0xFF00 + _mmu.ReadByte(programCounter++)), regA); break;
            case 0xE1: HL = Pop(); break;
            case 0xE2: _mmu.WriteByte((ushort)(0xFF00 + regC), regA); break;
            case 0xE5: Push(HL); break;
            case 0xE6: And(_mmu.ReadByte(programCounter++)); break;
            case 0xE7: Restart(0x20); break;

            case 0xE8: stackPointer = DoubleAddHL(stackPointer); break;
            case 0xE9: programCounter = HL; break;
            case 0xEA: _mmu.WriteWord(_mmu.ReadWord(programCounter += 2), regA); break;
            case 0xEE: Xor(_mmu.ReadByte(programCounter++)); break;
            case 0xEF: Restart(0x28); break;

            case 0xF0: regA = _mmu.ReadByte((ushort)(0xFF00 + _mmu.ReadByte(programCounter++))); break;
            case 0xF1: AF = Pop(); break;
            case 0xF2: regA = _mmu.ReadByte((ushort)(0xFF00 + regC)); break;
            case 0xF3: ime = false; break;
            case 0xF5: Push(AF); break;
            case 0xF6: Or(_mmu.ReadByte(programCounter++)); break;
            case 0xF7: Restart(0x30); break;

            case 0xF8: HL = DoubleAddHL(stackPointer); break;
            case 0xF9: stackPointer = HL; break;
            case 0xFA: regA = _mmu.ReadByte(_mmu.ReadWord(programCounter += 2)); break;
            case 0xFB: imeEnabler = true; break;
            case 0xFE: Compare(_mmu.ReadByte(programCounter++)); break;
            case 0xFF: Restart(0x38); break;

            default: UnsupportedOpCode(opCode); break;
        }
        cycles += _cyclesValues[opCode];
        return cycles;
    }

    private void PrefixCB(in byte opCode)
    {

    }

    private void Bit(in byte bit, in byte registry)
    {
        FlagZ = (registry & bit) == 0;
        FlagN = false;
        FlagH = true;
    }

    private void ShiftRightLog(in byte bit)
    {
        var result = (byte)(bit >> 1);
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = (result & 0x01) != 0;
    }

    private void Swap(ref byte bit)
    {
        bit = (byte)((bit & 0xF0) >> 4 | (bit & 0x0F) << 4);
        SetFlagZ(bit);
        FlagN = false;
        FlagH = false;
        FlagC = false;
    }

    private void ShiftRightAr(ref byte bit)
    {
        bit = (byte)((bit >> 1) | (bit & 0x80));
        SetFlagZ(bit);
        FlagN = false;
        FlagH = false;
        FlagC = (bit & 0x01) != 0;
    }

    private void ShiftLeft(ref byte bit)
    {
        bit = (byte)(bit << 1);
        SetFlagZ(bit);
        FlagN = false;
        FlagH = false;
        FlagC = (bit & 0x80) != 0;
    }

    private void RotateRight(ref byte bit)
    {
        bit = (byte)((bit >> 1) | (FlagC ? 0x80 : 0x00));
        SetFlagZ(bit);
        FlagN = false;
        FlagH = false;
        FlagC = (bit & 0x01) != 0;
    }

    private void RotateLeft(ref byte bit)
    {
        bit = (byte)((bit << 1) | (FlagC ? 0x01 : 0x00));
        SetFlagZ(bit);
        FlagN = false;
        FlagH = false;
        FlagC = (bit & 0x80) != 0;
    }

    private void RotateRightCarry(ref byte bit)
    {
        bit = (byte)((bit >> 1) | (bit << 7));
        SetFlagZ(bit);
        FlagN = false;
        FlagH = false;
        FlagC = (bit & 0x01) != 0;
    }

    private void RotateLeftCarry(ref byte bit)
    {
        bit = (byte)((bit << 1) | (bit >> 7));
        SetFlagZ(bit);
        FlagN = false;
        FlagH = false;
        FlagC = (bit & 0x80) != 0;
    }

    private ushort DoubleAddHL(in ushort registry)
    {
        var value = _mmu.ReadByte(programCounter++);
        FlagZ = false;
        FlagN = false;
        SetFlagH((byte)registry, value);
        SetFlagC((byte)registry + value);
        return (ushort)(registry + (sbyte)value);
    }

    private void JumpRelative(in bool flag)
    {
        if (flag)
        {
            var value = (sbyte)_mmu.ReadByte(programCounter);
            programCounter = (ushort)(programCounter + value);
            cycles += JUMP_RELATIVE_TRUE;
        }
        else cycles += JUMP_RELATIVE_FALSE;

        programCounter++;
    }

    private void Stop() { }

    private void Increment(ref byte bit)
    {
        var result = bit + 1;
        SetFlagZ(result);
        FlagN = false;
        SetFlagH(bit, 1);
        bit = (byte)result;
    }

    private void Decrement(ref byte bit)
    {
        var result = bit - 1;
        SetFlagZ(result);
        FlagN = true;
        SetFlagHSub(bit, 1);
        bit = (byte)result;
    }

    private void Add(in byte bit)
    {
        var result = regA + bit;
        SetFlagZ(result);
        FlagN = false;
        SetFlagH(regA, bit);
        SetFlagC(result);
        regA = (byte)result;
    }

    private void AddCarry(in byte bit)
    {
        var result = regA + bit + (FlagC ? 1 : 0);
        SetFlagZ(result);
        FlagN = false;

        if (FlagC) SetFlagHCarry(regA, bit);
        else SetFlagH(regA, bit);

        SetFlagC(result);
        regA = (byte)result;
    }

    private void Subtract(in byte bit)
    {
        var result = regA - bit;
        SetFlagZ(result);
        FlagN = true;
        SetFlagHSub(regA, bit);
        SetFlagC(result);
        regA = (byte)result;
    }

    private void SubtractCarry(in byte bit)
    {
        var result = regA - bit - (FlagC ? 1 : 0);
        SetFlagZ(result);
        FlagN = true;

        if (FlagC) SetFlagHSubCarry(regA, bit);
        else SetFlagHSub(regA, bit);

        SetFlagC(result);
        regA = (byte)result;
    }

    private void And(in byte bit)
    {
        var result = (byte)(regA & bit);
        SetFlagZ(result);
        FlagN = false;
        FlagH = true;
        FlagC = false;
        regA = result;
    }

    private void Xor(in byte bit)
    {
        var result = (byte)(regA ^ bit);
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = false;
        regA = result;
    }

    private void Or(in byte bit)
    {
        var result = (byte)(regA | bit);
        SetFlagZ(result);
        FlagN = false;
        FlagH = false;
        FlagC = false;
        regA = result;
    }

    private void Compare(in byte bit)
    {
        var result = regA - bit;
        SetFlagZ(result);
        FlagN = true;
        SetFlagHSub(regA, bit);
        SetFlagC(result);
    }

    private void DoubleAdd(in ushort registry)
    {
        var result = HL + registry;
        FlagN = false;
        SetFlagH(HL, registry);
        FlagC = result >> 16 != 0;
        HL = (ushort)result;
    }

    private void Return(in bool flag)
    {
        if (flag)
        {
            programCounter = Pop();
            cycles += RETURN_TRUE;
        }
        else cycles += RETURN_FALSE;
    }

    private void Call(in bool flag)
    {
        if (flag)
        {
            Push((ushort)(programCounter + 2));
            programCounter += _mmu.ReadWord(programCounter);
            cycles += CALL_TRUE;
        }
        else
        {
            programCounter += 2;
            cycles += CALL_FALSE;
        }
    }

    private void Jump(in bool flag)
    {
        if (flag)
        {
            programCounter = _mmu.ReadWord(programCounter);
            cycles += JUMP_TRUE;
        }
        else
        {
            programCounter += 2;
            cycles += JUMP_FALSE;
        }
    }

    private void Restart(in byte bit)
    {
        Push(programCounter);
        programCounter = bit;
    }

    private void Halt()
    {
        if (!ime)
        {
            if ((_mmu.InterruptEnable & _mmu.InterruptFlag & 0x1F) == 0)
            {
                halted = true;
                programCounter--;
            }
            else haltBug = true;
        }
    }

    private void UpdateIme()
    {
        ime |= imeEnabler;
        imeEnabler = false;
    }

    private void ExecuteInterrupt(in int value)
    {
        if (halted)
        {
            programCounter++;
            halted = false;
        }

        if (ime)
        {
            Push(programCounter);
            programCounter = (ushort)(0x40 + 8 * value);
            ime = false;
            Bits.Clear(ref _mmu.InterruptFlag, value);
        }
    }

    private void Push(in ushort word)
    {
        stackPointer -= 2;
        _mmu.WriteWord(stackPointer, word);
    }

    private ushort Pop()
    {
        var result = _mmu.ReadWord(stackPointer);
        stackPointer += 2;
        return result;
    }

    private static void SetFlag(ref byte registry, in byte setBit) => registry |= setBit;
    private static void UnsetFlag(ref byte registry, in byte unsetBit) => registry &= (byte)~unsetBit;

    private void SetFlagZ(in int value) => FlagZ = value == 0;
    private void SetFlagC(in int value) => FlagC = value >> 8 != 0;
    private void SetFlagH(in byte bit1, in byte bit2) => FlagH = (bit1 & 0x0F) + (bit2 & 0x0F) > 0x0F;
    private void SetFlagH(in ushort registry1, in ushort registry2) => FlagH = (registry1 & 0x0FFF) + (registry2 & 0x0FFF) > 0x0FFF;
    private void SetFlagHCarry(in byte byte1, in byte byte2) => FlagH = (byte1 & 0x0F) + (byte2 & 0x0F) >= 0x0F;
    private void SetFlagHSub(in byte byte1, in byte byte2) => FlagH = (byte1 & 0x0F) < (byte2 & 0x0F);
    private void SetFlagHSubCarry(in byte byte1, in byte byte2) => FlagH = (byte1 & 0x0F) < (byte2 & 0x0F) + (FlagC ? 1 : 0);

    private void UnsupportedOpCode(in byte opCode) => throw new NotSupportedException($"{programCounter - 1:X4} Unsupported Operation Code {opCode:X2}");
}