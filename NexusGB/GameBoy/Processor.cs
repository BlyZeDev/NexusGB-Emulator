namespace NexusGB.GameBoy;

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
            case 0x01:
                BC = _mmu.ReadWord(programCounter);
                programCounter += 2;
                break;
            case 0x02: _mmu.WriteByte(BC, regA); break;
            case 0x03: BC++; break;
            case 0x04: Increment(ref regB); break;
            case 0x05: Decrement(ref regB); break;
            case 0x06:
                regB = _mmu.ReadByte(programCounter);
                programCounter++;
                break;
            case 0x07:
                regF = 0;
                FlagC = (regA & 0x80) != 0;
                regA = (byte)((regA << 1) | (regA >> 7));
                break;
            case 0x08:
                _mmu.WriteWord(_mmu.ReadWord(programCounter), stackPointer);
                programCounter += 2;
                break;
            case 0x09: DoubleAdd(BC); break;
            case 0x0A: regA = _mmu.ReadByte(BC); break;
            case 0x0B: BC--; break;
            case 0x0C: Increment(ref regC); break;
            case 0x0D: Decrement(ref regC); break;
            case 0x0E:
                regC = _mmu.ReadByte(programCounter);
                programCounter++;
                break;
            case 0x0F:
                regF = 0;
                FlagC = (regA & 0x01) != 0;
                regA = (byte)((regA >> 1) | (regA << 7));
                break;

            case 0x10: Stop(); break;
            case 0x11:
                DE = _mmu.ReadWord(programCounter);
                programCounter += 2;
                break;
            case 0x12: _mmu.WriteByte(DE, regA); break;
            case 0x13: DE++; break;
            case 0x14: Increment(ref regD); break;
            case 0x15: Decrement(ref regD); break;
            case 0x16:
                regD = _mmu.ReadByte(programCounter);
                programCounter++;
                break;
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
            case 0x1E:
                regE = _mmu.ReadByte(programCounter);
                programCounter++;
                break;
            case 0x1F:
                {
                    var prevC = FlagC ? 0x80 : 0x00;
                    regF = 0;
                    FlagC = (regA & 0x01) != 0;
                    regA = (byte)((regA >> 1) | prevC);
                }
                break;

            case 0x20: JumpRelative(!FlagZ); break;
            case 0x21:
                HL = _mmu.ReadWord(programCounter);
                programCounter += 2;
                break;
            case 0x22: _mmu.WriteByte(HL++, regA); break;
            case 0x23: HL++; break;
            case 0x24: Increment(ref regH); break;
            case 0x25: Decrement(ref regH); break;
            case 0x26:
                regH = _mmu.ReadByte(programCounter);
                programCounter++;
                break;
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
            case 0x2E:
                regL = _mmu.ReadByte(programCounter);
                programCounter++;
                break;
            case 0x2F:
                regA = (byte)~regA;
                FlagN = true;
                FlagH = true;
                break;

            case 0x30: JumpRelative(!FlagC); break;
            case 0x31:
                stackPointer = _mmu.ReadWord(programCounter);
                programCounter += 2;
                break;
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
            case 0x36:
                _mmu.WriteByte(HL, _mmu.ReadByte(programCounter));
                programCounter++;
                break;
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
            case 0x3E:
                regA = _mmu.ReadByte(programCounter);
                programCounter++;
                break;
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
        }
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

    private void ShiftRightLog(ref byte bit)
    {
        bit = (byte)(bit >> 1);
        SetFlagZ(bit);
        FlagN = false;
        FlagH = false;
        FlagC = (bit & 0x01) != 0;
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

    private void DoubleAddHL(ref ushort registry)
    {
        var value = _mmu.ReadByte(programCounter++);
        FlagZ = false;
        FlagN = false;
        SetFlagH((byte)registry, value);
        SetFlagC((byte)registry + value);
        registry = (ushort)(registry + (sbyte)value);
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