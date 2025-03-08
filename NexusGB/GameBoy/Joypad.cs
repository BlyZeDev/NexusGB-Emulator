namespace NexusGB.GameBoy;

using ConsoleNexusEngine.IO;
using System.Collections.Immutable;

public sealed class Joypad
{
    private const int JOYPAD_INTERRUPT = 0x04;
    private const byte PAD_MASK = 0x10;
    private const byte BUTTON_MASK = 0x20;

    private byte pad;
    private byte buttons;

    public void HandleInputs(in ImmutableArray<NexusKey> keys)
    {
        pad = 0;
        buttons = 0;

        foreach (byte key in keys)
        {
            if ((key & PAD_MASK) == PAD_MASK)
                pad = (byte)(pad & ~(key & 0x0F));
            else if ((key & BUTTON_MASK) == BUTTON_MASK)
                buttons = (byte)(buttons & ~(key & 0x0F));
        }
    }

    public void Update(MemoryManagement mmu)
    {
        var joystickPad = mmu.JoystickPad;
    }
}