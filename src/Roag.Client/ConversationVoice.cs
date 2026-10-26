using Microsoft.Xna.Framework.Audio;

namespace Roag.Client;

internal sealed class ConversationVoice : IDisposable
{
    private readonly Dictionary<(long, char), SoundEffect> _sounds = [];
    private SoundEffectInstance? _playing;
    public bool Unavailable { get; private set; }
    public int PlayedSyllables { get; private set; }
    public void Play(long speaker, char character, float volume)
    {
        if (Unavailable || volume <= 0) return;
        try
        {
            char vowel = "aeiou"[char.ToLowerInvariant(character) % 5];
            if (!_sounds.TryGetValue((speaker, vowel), out var sound))
            {
                if (_sounds.Count >= 40) { Stop(); foreach (var cached in _sounds.Values) cached.Dispose(); _sounds.Clear(); }
                sound = new SoundEffect(SpeechSynthesis.Syllable(speaker, vowel), SpeechSynthesis.SampleRate, AudioChannels.Mono);
                _sounds[(speaker, vowel)] = sound;
            }
            Stop(); _playing = sound.CreateInstance(); _playing.Volume = Math.Clamp(volume, 0, 1); _playing.Play(); PlayedSyllables++;
        }
        catch (Exception e) when (e is NoAudioHardwareException or InvalidOperationException or DllNotFoundException)
        { Unavailable = true; Stop(); }
    }
    public void Stop() { _playing?.Stop(); _playing?.Dispose(); _playing = null; }
    public void Dispose() { Stop(); foreach (var sound in _sounds.Values) sound.Dispose(); _sounds.Clear(); }
}
