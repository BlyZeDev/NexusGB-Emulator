namespace NexusGB.GameBoy;

using ConsoleNexusEngine.IO;
using System.Collections.Immutable;

public sealed class Joypad
{
    private const int JOYPAD_INTERRUPT = 0x04;
    private const byte PAD_MASK = 0x10;
    private const byte BUTTON_MASK = 0x20;

    private readonly MemoryManagement _mmu;

    private byte pad;
    private byte buttons;

    public Joypad(MemoryManagement mmu) => _mmu = mmu;

    public void HandleInputs(in ImmutableArray<NexusKey> keys)
    {
        pad = 0x0F;
        buttons = 0x0F;

        byte keyBit;
        foreach (var key in keys)
        {
            keyBit = GetKeyCode(key);

            if ((keyBit & PAD_MASK) == PAD_MASK)
                pad = (byte)(pad & ~(keyBit & 0x0F));
            else if ((keyBit & BUTTON_MASK) == BUTTON_MASK)
                buttons = (byte)(buttons & ~(keyBit & 0x0F));
        }
    }

    public void Update()
    {
        var joystickPad = _mmu.JoystickPad;
        if (!Bits.Is(joystickPad, 4))
        {
            _mmu.JoystickPad = (byte)((joystickPad & 0xF0) | pad);
            if (pad != 0x0F) _mmu.RequestInterrupt(JOYPAD_INTERRUPT);
        }
        if (!Bits.Is(joystickPad, 5))
        {
            _mmu.JoystickPad = (byte)((joystickPad & 0xF0) | buttons);
            if (buttons != 0x0F) _mmu.RequestInterrupt(JOYPAD_INTERRUPT);
        }
        if ((joystickPad & 0b00110000) == 0b00110000) _mmu.JoystickPad = 0xFF;
    }

    private static byte GetKeyCode(in NexusKey key)
    {
        return key switch
        {
            NexusKey.D or NexusKey.Right => 0x11,
            NexusKey.A or NexusKey.Left => 0x12,
            NexusKey.W or NexusKey.Up => 0x14,
            NexusKey.S or NexusKey.Down => 0x18,
            NexusKey.J or NexusKey.Z => 0x21,
            NexusKey.K or NexusKey.X => 0x22,
            NexusKey.Space or NexusKey.C => 0x24,
            NexusKey.Return or NexusKey.V => 0x28,
            _ => 0x00
        };
    }
}