namespace NexusGB;

using SFML.Audio;
using SFML.System;
using System.Collections.Concurrent;

public sealed class WindowsSoundOut : SoundStream, IDisposable
{
    public const int SAMPLE_RATE = 48000;
    public const int SAMPLE_BUFFER_SIZE_IN_MILLISECONDS = 50;
    public const int CHANNEL_COUNT = 2;
    public const int SAMPLE_BUFFER_SIZE = (int)(SAMPLE_RATE * CHANNEL_COUNT * (SAMPLE_BUFFER_SIZE_IN_MILLISECONDS / 1000d));

    private readonly ConcurrentQueue<short> _sampleBuffer;

    public WindowsSoundOut()
    {
        _sampleBuffer = new ConcurrentQueue<short>();

        Initialize(CHANNEL_COUNT, SAMPLE_RATE);
        Play();
    }

    public void AddSamples(in short leftSample, in short rightSample)
    {
        _sampleBuffer.Enqueue(leftSample);
        _sampleBuffer.Enqueue(rightSample);
    }

    protected override bool OnGetData(out short[] samples)
    {
        samples = new short[SAMPLE_BUFFER_SIZE];

        var copySize = _sampleBuffer.Count < SAMPLE_BUFFER_SIZE ? _sampleBuffer.Count : SAMPLE_BUFFER_SIZE;
        for (int i = 0; i < copySize; i++)
        {
            _sampleBuffer.TryDequeue(out samples[i]);
        }

        return true;
    }

    protected override void OnSeek(Time timeOffset) { }
}