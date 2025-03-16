namespace NexusGB.GameBoy;

using NAudio.Wave;

public sealed class WindowsSoundOut : BufferedWaveProvider
{
    private static readonly byte[] _buffer = new byte[sizeof(float)];

    private readonly DirectSoundOut soundOut;
    private readonly byte[] _addSampleData;

    public WindowsSoundOut() : base(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2))
    {
        soundOut = new DirectSoundOut();
        soundOut.Init(this);
        soundOut.Play();

        _addSampleData = new byte[BufferLength];
    }

    public void BufferSoundSamples(in Span<float> sampleData, in int offset, in int length)
    {
        var finalLength = offset + length;
        for (int i = 0, j = 0; j < finalLength; j++)
        {
            GetBytes(sampleData[j], _buffer);
            foreach (var current in _buffer)
            {
                _addSampleData[i++] = current;
            }
        }

        AddSamples(_addSampleData, 0, length * sizeof(float));
    }

    private unsafe static void GetBytes(float floatValue, byte[] bytes)
    {
        var value = *(int*)&floatValue;

        fixed (byte* b = bytes)
        {
            *(int*)b = value;
        }
    }
}