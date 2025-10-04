using Microsoft.Xna.Framework.Audio;

namespace Minkowski.Rendering;

public static class Sound
{
    public static List<FunctionSynth> Synths = new List<FunctionSynth>();
    static void FillBuffer(byte[] buffer)
    {
        for (int i = 0; i < buffer.Length / 2; i++)
        {
            float sum = 0f;

            // Use ToArray() so you can remove completed synths inside the loop safely
            foreach (var synth in Synths.ToArray())
            {
                sum += synth.NextSample();

                if (synth.Done)
                    Synths.Remove(synth);
            }

            // Clamp, scale, write to buffer
            short s = (short)(Math.Clamp(sum, -1f, 1f) * 32767);
            buffer[2 * i] = (byte)(s & 0xFF);
            buffer[2 * i + 1] = (byte)((s >> 8) & 0xFF);
        }
    }
    
    public static void Update(float dt, DynamicSoundEffectInstance synthInstance)
    {
        // Always keep a couple of buffers queued to avoid audio dropouts
        while (synthInstance.PendingBufferCount < 2)
        {
            byte[] buffer = new byte[Config.bufferSize * 2];
            FillBuffer(buffer);
            synthInstance.SubmitBuffer(buffer);
        }
    }
}

public class FunctionSynth
{
    public Func<float, float> FrequencyFunc;
    public float SampleRate { get; }
    public float LengthSeconds { get; } // how long this synth lasts
    private float _phase = 0f;
    private int _sample = 0;
    public bool Done => LengthSeconds != -1 && (_sample / SampleRate) >= LengthSeconds;

    public FunctionSynth(Func<float, float> freqFunc, float sampleRate, float lengthSeconds)
    {
        FrequencyFunc = freqFunc;
        SampleRate = sampleRate;
        LengthSeconds = lengthSeconds;
    }

    public float NextSample()
    {
        float t = _sample / SampleRate;
        if (LengthSeconds != -1 && t >= LengthSeconds)
            return 0f; // silence after done

        float freq = FrequencyFunc(t);
        _phase += 2 * MathF.PI * freq / SampleRate;
        if (_phase > 2 * MathF.PI)
            _phase -= 2 * MathF.PI;

        _sample++;
        return MathF.Sin(_phase);
    }

    public void Reset()
    {
        _phase = 0f;
        _sample = 0;
    }
}