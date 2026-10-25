namespace Vagabonds.Graphics;

/// <summary>Bounded reusable line layouts; colors, positions and live text remain caller-owned.</summary>
public sealed class TextLayoutCache
{
    public const int Capacity = 128;
    public const int CharacterBudget = 65_536;
    public const int MaxTextLength = 4096;
    private sealed record Entry((string Text, int Columns) Key, IReadOnlyList<string> Lines, int Characters);
    private readonly Dictionary<(string Text, int Columns), LinkedListNode<Entry>> _entries = [];
    private readonly LinkedList<Entry> _order = [];
    public int Count => _entries.Count;
    public int Characters { get; private set; }
    public long Hits { get; private set; }
    public long Misses { get; private set; }

    public IReadOnlyList<string> Get(string text, int columns)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfLessThan(columns, 1);
        var key = (text, columns);
        if (_entries.TryGetValue(key, out var node))
        {
            _order.Remove(node); _order.AddLast(node); Hits++;
            return node.Value.Lines;
        }
        Misses++;
        var lines = Layout(text, columns);
        if (text.Length > MaxTextLength) return lines;
        int characters = text.Length;
        for (int i = 0; i < lines.Count; i++) characters += lines[i].Length;
        while (_order.First is { } oldest && (Count >= Capacity || Characters + characters > CharacterBudget))
        {
            _entries.Remove(oldest.Value.Key); Characters -= oldest.Value.Characters; _order.RemoveFirst();
        }
        node = _order.AddLast(new Entry(key, lines, characters));
        _entries.Add(key, node); Characters += characters;
        return lines;
    }

    private static IReadOnlyList<string> Layout(string text, int columns)
    {
        var lines = new List<string>();
        foreach (string paragraph in text.Split('\n'))
        {
            string line = "";
            foreach (string word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length > 0 && line.Length + word.Length + 1 > columns)
                { lines.Add(line); line = ""; }
                if (word.Length > columns)
                {
                    if (line.Length > 0) { lines.Add(line); line = ""; }
                    for (int i = 0; i < word.Length; i += columns) lines.Add(word.Substring(i, Math.Min(columns, word.Length - i)));
                }
                else line += (line.Length > 0 ? " " : "") + word;
            }
            // Preserve the existing paragraph advance, including the blank row
            // after a final overlong word and empty/trailing paragraphs.
            lines.Add(line);
        }
        return lines.AsReadOnly();
    }
}
