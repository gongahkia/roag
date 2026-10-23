using Microsoft.Xna.Framework.Audio;

namespace Vagabonds.Client;

/// <summary>Small original sound cues, cached once per family; playback never consults simulation state.</summary>
internal sealed class CombatAudio : IDisposable
{
    private readonly Dictionary<string, SoundEffect> _sounds = new(StringComparer.Ordinal);
    private SoundEffectInstance? _playing;
    private bool _disposed;
    public bool Unavailable { get; private set; }
    public int PlayedCues { get; private set; }

    public void Play(string cue, float volume)
    {
        if (_disposed || Unavailable || !float.IsFinite(volume) || volume <= 0) return;
        try
        {
            if (!_sounds.TryGetValue(cue, out var sound))
            {
                sound = new SoundEffect(CombatSoundSynthesis.Cue(cue), CombatSoundSynthesis.SampleRate, AudioChannels.Mono);
                _sounds.Add(cue, sound);
            }
            Stop(); _playing = sound.CreateInstance(); _playing.Volume = Math.Clamp(volume, 0, 1); _playing.Play(); PlayedCues++;
        }
        catch (Exception e) when (AudioFailure(e)) { Unavailable = true; Stop(); }
    }
    public void Stop()
    {
        var playing = _playing; _playing = null;
        if (playing == null) return;
        try { playing.Stop(); }
        catch (Exception e) when (AudioFailure(e)) { Unavailable = true; }
        finally { playing.Dispose(); }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true; Stop();
        foreach (var sound in _sounds.Values) sound.Dispose();
        _sounds.Clear();
    }
    private static bool AudioFailure(Exception e) => e is NoAudioHardwareException or InvalidOperationException or DllNotFoundException;
}

/// <summary>16-bit little-endian mono PCM. Fixed local noise is decorative, never a game RNG draw.</summary>
internal static class CombatSoundSynthesis
{
    public const int SampleRate = 22050;
    public static IReadOnlyList<string> Cues { get; } = Array.AsReadOnly(new[]
        { "slash", "arrow", "spell", "heal", "guard", "dread", "cooperative", "retreat" });

    public static byte[] Cue(string cue)
    {
        if (!Cues.Contains(cue)) throw new ArgumentException("Unknown combat sound: " + cue, nameof(cue));
        double duration = cue is "heal" or "dread" or "cooperative" ? .29 : .20;
        int length = (int)(SampleRate * duration); var pcm = new byte[length * 2];
        uint noiseState = 0x9e3779b9;
        for (int i = 0; i < length; i++)
        {
            double t = (double)i / SampleRate, u = (double)i / (length - 1);
            noiseState ^= noiseState << 13; noiseState ^= noiseState >> 17; noiseState ^= noiseState << 5;
            double noise = (noiseState & 65535) / 32767.5 - 1;
            // Integrate the linear pitch sweep so its frequency remains continuous.
            double Tone(double start, double end) => Math.Sin(2 * Math.PI * (start * t + .5 * (end - start) * t * t / duration));
            double signal = cue switch
            {
                "slash" => .65 * noise * (1 - u) + .35 * Tone(180, 55),
                "arrow" => .35 * noise * (1 - u) + .65 * Tone(1150, 230),
                "spell" => .6 * Tone(230, 850) + .25 * Tone(690, 1275) + .15 * noise,
                "heal" => .5 * Tone(440, 660) + .3 * Tone(660, 880) + .2 * Tone(880, 1320),
                "guard" => .55 * Tone(470, 400) + .3 * Tone(1175, 1000) + .15 * noise * (1 - u),
                "dread" => .6 * Tone(170, 60) + .25 * Tone(180, 63) + .15 * noise,
                "cooperative" => .4 * Tone(330, 110) + .35 * Tone(440, 660) + .25 * noise * (1 - u),
                _ => .65 * Tone(660, 220) + .35 * Tone(880, 293)
            };
            double envelope = Math.Sin(Math.PI * u) * Math.Exp(-2.5 * u);
            short sample = (short)Math.Clamp(signal * envelope * 22000, short.MinValue, short.MaxValue);
            pcm[i * 2] = (byte)(sample & 255); pcm[i * 2 + 1] = (byte)(sample >> 8);
        }
        return pcm;
    }
}
