using Microsoft.Xna.Framework.Audio;

namespace Roag.Client;

/// <summary>Original built-in PCM samples, synthesized without a simulation RNG.</summary>
internal static class DungeonSoundSynthesis
{
    public const int SampleRate = 22050;
    public static byte[] Sample(string kind)
    {
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
}

internal sealed class DungeonAudio : IDisposable
{
    private readonly Dictionary<string, SoundEffect> _samples = [];
    private readonly List<SoundEffectInstance> _voices = [];
    private SoundEffectInstance? _music, _ambience;
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

    public void Update(string scene, float music, float effects, bool paused)
    {
        if (Unavailable) return;
        try
        {
            Prune();
            foreach (var voice in _voices) voice.Volume = Math.Clamp(effects, 0, 1);
            if (_scene != scene)
            {
                _music?.Stop(); _music?.Dispose(); _ambience?.Stop(); _ambience?.Dispose(); _ambience = null;
                _scene = scene; _music = Sample("music").CreateInstance(); _music.IsLooped = true;
                if (!scene.EndsWith("victory", StringComparison.Ordinal) && !scene.EndsWith("defeat", StringComparison.Ordinal))
                { _ambience = Sample("ambience").CreateInstance(); _ambience.IsLooped = true; }
            }
            // The original user mix applies music volume to both looping beds.
            foreach (var voice in new[] { _music, _ambience })
            {
                if (voice == null) continue;
                voice.Volume = Math.Clamp(music, 0, 1);
                if (paused || music <= 0) { if (voice.State == SoundState.Playing) voice.Pause(); }
                else if (voice.State == SoundState.Paused) voice.Resume(); else if (voice.State == SoundState.Stopped) voice.Play();
            }
        }
        catch (Exception e) when (AudioFailure(e)) { Unavailable = true; Stop(); }
    }

    private void Prune()
    {
        for (int i = _voices.Count - 1; i >= 0; i--) if (_voices[i].State == SoundState.Stopped) { _voices[i].Dispose(); _voices.RemoveAt(i); }
    }
    public void Stop()
    {
        static void Release(SoundEffectInstance? voice)
        {
            if (voice == null) return;
            try { voice.Stop(); } catch (Exception e) when (AudioFailure(e)) { }
            finally { voice.Dispose(); }
        }
        foreach (var voice in _voices) Release(voice);
        _voices.Clear(); Release(_music); _music = null; Release(_ambience); _ambience = null; _scene = "";
    }
    public void Dispose() { Stop(); foreach (var sample in _samples.Values) sample.Dispose(); _samples.Clear(); }
    private static bool AudioFailure(Exception e) => e is NoAudioHardwareException or InvalidOperationException or DllNotFoundException;
}
