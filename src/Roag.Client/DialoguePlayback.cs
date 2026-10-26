using System.Text;
using Roag.Core.World;

namespace Roag.Client;

internal sealed class DialoguePlayback
{
    private DialogueLine[] _pages = [];
    private int _page, _revealed;
    private double _clock;
    public DialogueLine? Line => _pages.ElementAtOrDefault(_page);
    public bool Finished { get; private set; }
    public bool Speaking => !Finished && Line is { } line && _revealed < line.Text.Length;
    public string Text => Line?.Text[.._revealed] ?? "";
    public void Start(IEnumerable<DialogueLine> lines, int columns, int rows)
    {
        _pages = lines.SelectMany(line => Paginate(line.Text, columns, rows).Select(text => line with { Text = text })).ToArray();
        _page = _revealed = 0; _clock = 0; Finished = _pages.Length == 0;
    }
    public void Advance()
    {
        if (Finished || Line is not { } line) return;
        if (Speaking) { _revealed = line.Text.Length; return; }
        if (_page + 1 == _pages.Length) { Finished = true; return; }
        _page++; _revealed = 0; _clock = 0;
    }
    public void Update(double seconds, Action<long, char> blip)
    {
        if (!Speaking || seconds <= 0) return;
        _clock += Math.Min(.1, seconds); bool sounded = false;
        while (_clock >= 0 && Speaking)
        {
            char character = Line!.Text[_revealed++];
            _clock -= character is '.' or '?' or '!' ? .26 : character is ',' or ';' or ':' ? .13 : .035;
            if (!sounded && char.IsLetterOrDigit(character) && _revealed % 2 == 1) { blip(Line.Speaker, character); sounded = true; }
        }
    }
    internal static IEnumerable<string> Paginate(string text, int columns, int rows)
    {
        columns = Math.Max(12, columns); rows = Math.Max(1, rows);
        var line = new StringBuilder(); var page = new List<string>();
        foreach (string raw in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string word = raw;
            if (line.Length > 0 && line.Length + 1 + word.Length > columns)
            {
                page.Add(line.ToString()); line.Clear();
                if (page.Count == rows) { yield return string.Join('\n', page); page.Clear(); }
            }
            while (word.Length > columns)
            {
                page.Add(word[..columns]); word = word[columns..];
                if (page.Count == rows) { yield return string.Join('\n', page); page.Clear(); }
            }
            if (line.Length > 0) line.Append(' '); line.Append(word);
        }
        if (line.Length > 0) page.Add(line.ToString());
        if (page.Count > 0) yield return string.Join('\n', page);
    }
}

/// <summary>Original short voiced syllables: a glottal pulse plus vowel formants, 16-bit mono PCM.</summary>
internal static class SpeechSynthesis
{
    public const int SampleRate = 22050;
    public static byte[] Syllable(long speaker, char vowel)
    {
        uint voice = WorldRandom.Hash(7919, (int)speaker, (int)(speaker >> 32));
        double fundamental = 145 + voice % 165;
        double formant = "aeiou".IndexOf(char.ToLowerInvariant(vowel)) switch { 0 => 800, 1 => 530, 2 => 320, 3 => 600, _ => 400 };
        int length = (int)(SampleRate * (.065 + voice % 4 * .005)); var pcm = new byte[length * 2];
        for (int i = 0; i < length; i++)
        {
            double t = (double)i / SampleRate, u = (double)i / (length - 1);
            double envelope = Math.Sin(Math.PI * u) * Math.Sin(Math.PI * u);
            double pitch = fundamental * (1 + .035 * Math.Sin(u * Math.PI * 2));
            double signal = .5 * Math.Sin(2 * Math.PI * pitch * t) + .2 * Math.Sin(4 * Math.PI * pitch * t)
                + .2 * Math.Sin(2 * Math.PI * formant * t) * (.5 + .5 * Math.Sin(2 * Math.PI * pitch * t));
            short sample = (short)(signal * envelope * 14000);
            pcm[2 * i] = (byte)(sample & 255); pcm[2 * i + 1] = (byte)(sample >> 8);
        }
        return pcm;
    }
}
