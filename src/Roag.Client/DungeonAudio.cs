using Microsoft.Xna.Framework.Audio;

namespace Roag.Client;

/// <summary>Original built-in PCM samples, synthesized without a simulation RNG.</summary>
internal static class DungeonSoundSynthesis
{
    public const int SampleRate = 22050;
    public static byte[] Sample(string kind)
    {
        if (kind is "attack" or "chest" or "chest-open") return ActionSample(kind);
        (double duration, double frequency, double volume) = kind switch
        {
            "step" => (.085, 150, .2), "hit" => (.14, 85, .36), "boon" => (.28, 660, .26), "menu" => (.09, 480, .18),
            "enemy" => (.17, 110, .26), "terrain" => (.12, 190, .24), "music" => (2, 220, .13), "ambience" => (2, 96, .055),
            _ => throw new ArgumentException("Unknown built-in audio sample.", nameof(kind))
        };
        int count = (int)Math.Floor(SampleRate * duration + .5); var pcm = new byte[count * 2];
        for (int i = 0; i < count; i++)
        {
            double t = i / (double)SampleRate;
            double envelope = kind is "music" or "ambience" ? .5 - .5 * Math.Cos(2 * Math.PI * i / count) : Math.Pow(1 - i / (double)count, 2);
            double wave = Math.Sin(2 * Math.PI * frequency * t);
            if (kind is "hit" or "enemy") wave = wave * .7 + Math.Sin(2 * Math.PI * (frequency * 2.03) * t) * .3;
            else if (kind is "boon" or "music" or "ambience") wave = wave * .65 + Math.Sin(2 * Math.PI * (frequency * 1.5) * t) * .35;
            short value = (short)Math.Floor(Math.Clamp(wave * envelope * volume, -1, 1) * 32767 + .5);
            pcm[i * 2] = (byte)(value & 255); pcm[i * 2 + 1] = (byte)((value >> 8) & 255);
        }
        return pcm;
    }

    private static byte[] ActionSample(string kind)
    {
        double duration = kind switch { "attack" => .11, "chest-open" => .48, _ => .85 };
        int count = (int)(SampleRate * duration);
        var pcm = new byte[count * 2]; uint noise = 0x524F4147;
        for (int i = 0; i < count; i++)
        {
            double t = i / (double)SampleRate, wave;
            if (kind == "attack")
            {
                noise = unchecked(noise * 1664525 + 1013904223);
                wave = ((noise / (double)uint.MaxValue * 2 - 1) * .55 + Math.Sin(2 * Math.PI * (850 * t - 3000 * t * t)) * .45)
                    * Math.Pow(Math.Sin(Math.PI * t / duration), 2) * .23;
            }
            else if (kind == "chest-open")
            {
                wave = 0;
                for (int note = 0; note < 4; note++)
                {
                    double local = t - note * .065, frequency = note switch { 0 => 523.25, 1 => 659.25, 2 => 783.99, _ => 1046.5 };
                    if (local >= 0) wave += Math.Sin(2 * Math.PI * frequency * local) * Math.Exp(-local * 9) * .12;
                }
                wave *= Math.Clamp((duration - t) / .04, 0, 1);
            }
            else
            {
                // A short metallic "cha", followed by a brighter ringing "ching"
                // and a small coin echo. Local noise never consumes world RNG.
                wave = 0;
                for (int strike = 0; strike < 3; strike++)
                {
                    double local = t - (strike == 0 ? 0 : strike == 1 ? .16 : .27);
                    if (local < 0) continue;
                    double frequency = strike == 0 ? 1174.66 : strike == 1 ? 1760 : 2349.32;
                    double envelope = (1 - Math.Exp(-local * 900)) * Math.Exp(-local * (strike == 0 ? 24 : 8));
                    double bell = Math.Sin(2 * Math.PI * frequency * local) * .55
                        + Math.Sin(2 * Math.PI * frequency * 2.756 * local) * .25
                        + Math.Sin(2 * Math.PI * frequency * 4.07 * local) * .12
                        + Math.Sin(2 * Math.PI * frequency * 6.2 * local) * .08;
                    wave += bell * envelope * (strike == 2 ? .16 : .36);
                }
                noise = unchecked(noise * 1664525 + 1013904223);
                wave += (noise / (double)uint.MaxValue * 2 - 1) * Math.Exp(-t * 105) * (1 - Math.Exp(-t * 1800)) * .09;
                wave *= Math.Clamp((duration - t) / .06, 0, 1);
            }
            short sample = (short)Math.Clamp(Math.Round(wave * short.MaxValue), short.MinValue, short.MaxValue);
            pcm[i * 2] = (byte)(sample & 255); pcm[i * 2 + 1] = (byte)((sample >> 8) & 255);
        }
        return pcm;
    }
}

internal sealed class DungeonAudio : IDisposable
{
    private readonly Dictionary<string, SoundEffect> _samples = [];
    private readonly List<SoundEffectInstance> _voices = [];
    private SoundEffectInstance? _music, _ambience;
    private SoundEffectInstance? _retiringMusic, _retiringAmbience;
    private double _transitionAge = 1;
    private string _scene = "";
    public bool Unavailable { get; private set; }
    public int PlayedCues { get; private set; }
    public string Scene => _scene;

    private SoundEffect Sample(string kind)
    {
        if (!_samples.TryGetValue(kind, out var sound))
        { sound = new(DungeonSoundSynthesis.Sample(kind), DungeonSoundSynthesis.SampleRate, AudioChannels.Mono); _samples.Add(kind, sound); }
        return sound;
    }

    public void Play(string kind, float volume)
    {
        if (Unavailable || volume <= 0) return;
        try
        {
            Prune();
            if (_voices.Count >= 24) { _voices[0].Stop(); _voices[0].Dispose(); _voices.RemoveAt(0); }
            var voice = Sample(kind).CreateInstance(); voice.Volume = Math.Clamp(volume, 0, 1); voice.Play(); _voices.Add(voice); PlayedCues++;
        }
        catch (Exception e) when (AudioFailure(e)) { Unavailable = true; Stop(); }
    }

    public void Update(string scene, float music, float effects, bool paused, double elapsed)
    {
        if (Unavailable) return;
        try
        {
            Prune();
            foreach (var voice in _voices) Mix(voice, effects, paused);
            if (_scene != scene)
            {
                Release(_retiringMusic); Release(_retiringAmbience);
                _retiringMusic = _music; _retiringAmbience = _ambience; _ambience = null; _transitionAge = 0;
                _scene = scene; _music = Sample("music").CreateInstance(); _music.IsLooped = true;
                if (!scene.EndsWith("victory", StringComparison.Ordinal) && !scene.EndsWith("defeat", StringComparison.Ordinal))
                { _ambience = Sample("ambience").CreateInstance(); _ambience.IsLooped = true; }
            }
            if (!paused) _transitionAge += Math.Clamp(elapsed, 0, .2);
            float blend = (float)Math.Clamp(_transitionAge / .8, 0, 1);
            // Preserve the built-in 800ms crossfade and music mix on both beds.
            Mix(_music, music * blend, paused); Mix(_ambience, music * blend, paused);
            Mix(_retiringMusic, music * (1 - blend), paused); Mix(_retiringAmbience, music * (1 - blend), paused);
            if (blend >= 1) { Release(_retiringMusic); Release(_retiringAmbience); _retiringMusic = _retiringAmbience = null; }
        }
        catch (Exception e) when (AudioFailure(e)) { Unavailable = true; Stop(); }
    }

    private void Prune()
    {
        for (int i = _voices.Count - 1; i >= 0; i--) if (_voices[i].State == SoundState.Stopped) { _voices[i].Dispose(); _voices.RemoveAt(i); }
    }
    private static void Mix(SoundEffectInstance? voice, float volume, bool paused)
    {
        if (voice == null) return;
        voice.Volume = Math.Clamp(volume, 0, 1);
        if (paused || volume <= 0) { if (voice.State == SoundState.Playing) voice.Pause(); }
        else if (voice.State == SoundState.Paused) voice.Resume(); else if (voice.State == SoundState.Stopped) voice.Play();
    }
    private static void Release(SoundEffectInstance? voice)
    {
        if (voice == null) return;
        try { voice.Stop(); } catch (Exception e) when (AudioFailure(e)) { }
        finally { voice.Dispose(); }
    }
    public void Stop()
    {
        foreach (var voice in _voices) Release(voice);
        _voices.Clear(); Release(_music); _music = null; Release(_ambience); _ambience = null; _scene = "";
        Release(_retiringMusic); Release(_retiringAmbience); _retiringMusic = _retiringAmbience = null;
    }
    public void Dispose() { Stop(); foreach (var sample in _samples.Values) sample.Dispose(); _samples.Clear(); }
    private static bool AudioFailure(Exception e) => e is NoAudioHardwareException or InvalidOperationException or DllNotFoundException;
}
