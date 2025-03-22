namespace NexusGB;

using SFML.Audio;
using SFML.System;

public sealed class WindowsSoundOut : SoundStream
{
    public const int SAMPLE_RATE = 48000;
    public const int SAMPLE_BUFFER_SIZE_IN_MILLISECONDS = 50;
    public const int CHANNEL_COUNT = 2;
    public const int SAMPLE_BUFFER_SIZE = (int)(SAMPLE_RATE * CHANNEL_COUNT * (SAMPLE_BUFFER_SIZE_IN_MILLISECONDS / 1000d));

    private readonly Queue<short> _sampleBuffer;

    public WindowsSoundOut()
    {
        _sampleBuffer = new Queue<short>(SAMPLE_BUFFER_SIZE);

        Initialize(CHANNEL_COUNT, SAMPLE_RATE);
        Play();
    }

    public void AddSamples(short leftSample, short rightSample)
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
            samples[i] = _sampleBuffer.Dequeue();
        }

        return true;
    }

    protected override void OnSeek(Time timeOffset) { }
}