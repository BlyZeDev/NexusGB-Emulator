namespace NexusGB.GameBoy;

using ConsoleNexusEngine.IO;
using System.Collections.Immutable;

public sealed class Joypad
{
    private const int JOYPAD_INTERRUPT = 4;
    private const byte PAD_MASK = 0x10;
    private const byte BUTTON_MASK = 0x20;

    private static readonly Dictionary<NexusXInput, byte> _buttonMappings = new Dictionary<NexusXInput, byte>
    {
        { NexusXInput.DirectionalPadUp, 0x14 },
        { NexusXInput.DirectionalPadLeft, 0x12 },
        { NexusXInput.DirectionalPadRight, 0x11 },
        { NexusXInput.DirectionalPadDown, 0x18 },
        { NexusXInput.ButtonA, 0x21 },
        { NexusXInput.ButtonB, 0x22 },
        { NexusXInput.Back, 0x24 },
        { NexusXInput.Start, 0x28 }
    };
    
    private readonly MemoryManagement _mmu;

    private byte pad;
    private byte buttons;

    public Joypad(MemoryManagement mmu) => _mmu = mmu;

    public void HandleInputs(in NexusGamepad gamepad, in ImmutableArray<NexusKey> keys)
    {
        pad = 0x0F;
        buttons = 0x0F;

        foreach (var key in GetKeyCodes(gamepad, keys))
        {
            if ((key & PAD_MASK) == PAD_MASK)
                pad = (byte)(pad & ~(key & 0x0F));
            else if ((key & BUTTON_MASK) == BUTTON_MASK)
                buttons = (byte)(buttons & ~(key & 0x0F));
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

    private static IEnumerable<byte> GetKeyCodes(NexusGamepad gamepad, ImmutableArray<NexusKey> keys)
    {
        if (gamepad.LeftThumbX < -100) yield return _buttonMappings[NexusXInput.DirectionalPadLeft];
        else if (gamepad.LeftThumbX > 100) yield return _buttonMappings[NexusXInput.DirectionalPadRight];

        if (gamepad.LeftThumbY < -100) yield return _buttonMappings[NexusXInput.DirectionalPadUp];
        else if (gamepad.LeftThumbY > 100) yield return _buttonMappings[NexusXInput.DirectionalPadDown];

        foreach (var button in Enum.GetValues<NexusXInput>())
        {
            if ((gamepad.Buttons & button) != 0
                && _buttonMappings.TryGetValue(button, out var key)) yield return key;
        }

        foreach (var key in keys)
        {
            switch (key)
            {
                case NexusKey.Up or NexusKey.W: yield return _buttonMappings[NexusXInput.DirectionalPadUp]; break;
                case NexusKey.Left or NexusKey.A: yield return _buttonMappings[NexusXInput.DirectionalPadLeft]; break;
                case NexusKey.Right or NexusKey.D: yield return _buttonMappings[NexusXInput.DirectionalPadRight]; break;
                case NexusKey.Down or NexusKey.S: yield return _buttonMappings[NexusXInput.DirectionalPadDown]; break;

                case NexusKey.E: yield return _buttonMappings[NexusXInput.ButtonA]; break;
                case NexusKey.Q: yield return _buttonMappings[NexusXInput.ButtonB]; break;

                case NexusKey.Tab or NexusKey.Back: yield return _buttonMappings[NexusXInput.Back]; break;
                case NexusKey.Return: yield return _buttonMappings[NexusXInput.Start]; break;
            }
        }
    }
}