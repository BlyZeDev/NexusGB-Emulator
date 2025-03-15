namespace NexusGB.GameBoy;

public sealed class SoundProcessor
{
    private byte number50;
    private byte number51;
    private byte number52;

    public SoundProcessor()
    {

    }

    public void Update(in int cycles)
    {
        if ((number52 & (1 << 7)) == 0) return;


    }

    public byte ReadByte(in ushort address)
    {
        switch (address)
        {
            case 0xFF24: return number50;
            case 0xFF25: return number51;
            case 0xFF26: return number52;

            case > 0xFF26 and < 0xFF30: return 0x00;
            case > 0xFF31 and < 0xFF40: return ???;
        }

        var relativeAddress = address - 0xFF10;
        var channel = _channels[relativeAddress / 5];

        return channel.Numbers[relativeAddress % 5];
    }

    public void WriteByte(in ushort address, in byte value)
    {
        switch (address)
        {
            case 0xFF24: number50 = value; return;
            case 0xFF25: number51 = value; return;
            case 0xFF26: number52 = value; return;

            case > 0xFF26 and < 0xFF30: return;
            case > 0xFF31 and < 0xFF40: ??? return;
        }

        var relativeAddress = address - 0xFF10;
        var channel = _channels[relativeAddress / 5];

        channel.Numbers[relativeAddress % 5] = value;
    }
}