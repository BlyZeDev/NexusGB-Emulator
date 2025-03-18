namespace NexusGB;

using SFML.Audio;
using SFML.System;

public sealed class WindowsSoundOut : SoundStream
{
    public const int SAMPLE_RATE = 48000;
    public const int SAMPLE_BUFFER_SIZE_IN_MILLISECONDS = 50;
    public const int CHANNEL_COUNT = 2;
    public const int SAMPLE_BUFFER_SIZE = (int)(SAMPLE_RATE * CHANNEL_COUNT * (SAMPLE_BUFFER_SIZE_IN_MILLISECONDS / 1000f));

    private readonly List<short> _sampleBuffer;

    public WindowsSoundOut()
    {
        _sampleBuffer = new List<short>(SAMPLE_BUFFER_SIZE);

        Initialize(CHANNEL_COUNT, SAMPLE_RATE);
        Play();
    }

    public void AddSamples(in short leftSample, in short rightSample)
    {
        _sampleBuffer.Add(leftSample);
        _sampleBuffer.Add(rightSample);
    }

    protected override bool OnGetData(out short[] samples)
    {
        if (_sampleBuffer.Count >= SAMPLE_BUFFER_SIZE)
        {
            samples = _sampleBuffer.GetRange(0, SAMPLE_BUFFER_SIZE).ToArray();

            _sampleBuffer.RemoveRange(0, SAMPLE_BUFFER_SIZE);
        }
        else
        {
            if (_sampleBuffer.Count == 0)
                samples = new short[SAMPLE_BUFFER_SIZE];
            else
            {
                samples = new short[SAMPLE_BUFFER_SIZE];
                for (int i = 0; i < samples.Length; i++) samples[i] = _sampleBuffer[i % _sampleBuffer.Count];
            }
        }

        return true;
    }

    protected override void OnSeek(Time timeOffset) { }
}