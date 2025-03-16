namespace NexusGB.GameBoy;

using NAudio.Wave;
using System.Runtime.InteropServices;

public sealed class WindowsSoundOut
{
    private readonly DirectSoundOut soundOut;
    private readonly BufferedWaveProvider _provider;
    private readonly byte[] _addSampleData;

    public int SampleRate => _provider.WaveFormat.SampleRate;

    public WindowsSoundOut()
    {
        _provider = new BufferedWaveProvider(new WaveFormat(44100, 16, 2));
        soundOut = new DirectSoundOut(100);
        soundOut.Init(_provider);
        soundOut.Play();

        _addSampleData = new byte[_provider.BufferLength];
    }

    public void BufferSoundSamples(in Span<float> sampleData, in int length)
    {
        var index = 0;
        foreach (var current in MemoryMarshal.Cast<float, byte>(sampleData))
        {
            _addSampleData[index++] = current;
        }

        _provider.AddSamples(_addSampleData, 0, length + sizeof(float));
    }
}