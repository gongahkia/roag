using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public sealed record StructuralSample(string Id, bool Rotate, bool Reflect, string[] Rows);
public sealed record CultureSample(string Name, string Sample, string Wall, string Floor);
public sealed class SampleLibrary
{
    private sealed record Document(int Version, int PatternSize, Dictionary<char, string> Legend, StructuralSample[] Samples, CultureSample[] Cultures);
    public static SampleLibrary Default { get; } = Load();
    public string Hash { get; }
    public int PatternSize { get; }
    public StructuralSample[] Samples { get; }
    public CultureSample[] Cultures { get; }
    private readonly Dictionary<string, OverlappingWfc> _models;
    private SampleLibrary(byte[] data)
    {
        var doc = JsonSerializer.Deserialize<Document>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Empty world samples.");
        if (doc.Version != 1 || doc.PatternSize is < 2 or > 5 || doc.Cultures.Length != 6 || doc.Samples.Length == 0) throw new InvalidDataException("Unsupported world sample manifest.");
        foreach (var sample in doc.Samples)
            if (sample.Rows.Length < doc.PatternSize || sample.Rows.Any(row => row.Length != sample.Rows[0].Length || row.Any(c => !doc.Legend.ContainsKey(c))))
                throw new InvalidDataException($"Invalid structural sample: {sample.Id}.");
        Hash = Convert.ToHexString(SHA256.HashData(data)); PatternSize = doc.PatternSize;
        Samples = doc.Samples; Cultures = doc.Cultures;
        _models = Samples.ToDictionary(s => s.Id, s => new OverlappingWfc(s.Rows, PatternSize, s.Rotate, s.Reflect));
        if (Cultures.Any(c => !_models.ContainsKey(c.Sample))) throw new InvalidDataException("Culture refers to missing sample.");
    }
    public OverlappingWfc Model(int culture) => _models[Cultures[culture].Sample];
    private static SampleLibrary Load()
    {
        using var input = typeof(SampleLibrary).Assembly.GetManifestResourceStream("Vagabonds.World.samples.json") ?? throw new InvalidDataException("World samples missing.");
        using var bytes = new MemoryStream(); input.CopyTo(bytes); return new(bytes.ToArray());
    }
}

public sealed record CollapseResult(char[] Cells, int[] Patterns, int Attempts, int Observations, long Propagations);

/// <summary>Overlapping sample model: every output N by N window is a learned pattern.
/// Domains use arbitrarily many 64-bit words; the legacy socket solver is independent.</summary>
public sealed class OverlappingWfc
{
    private static readonly (int X, int Y)[] Directions = [(1, 0), (0, 1), (-1, 0), (0, -1)];
    private readonly char[][] _patterns;
    private readonly int[] _weights;
    private readonly ulong[][][] _compatible;
    private readonly int _n, _words;
    public int PatternCount => _patterns.Length;
    public int PatternSize => _n;
    public OverlappingWfc(string[] sample, int patternSize = 3, bool rotations = false, bool reflections = false)
    {
        _n = patternSize;
        if (_n is < 2 or > 5 || sample.Length < _n || sample[0].Length < _n || sample.Any(row => row.Length != sample[0].Length)) throw new ArgumentException("Invalid overlapping sample.");
        var patterns = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int y = 0; y <= sample.Length - _n; y++) for (int x = 0; x <= sample[0].Length - _n; x++)
        {
            var p = new char[_n * _n];
            for (int py = 0; py < _n; py++) for (int px = 0; px < _n; px++) p[py * _n + px] = sample[y + py][x + px];
            for (int rot = 0; rot < (rotations ? 4 : 1); rot++)
            {
                Add(p);
                if (reflections) Add(Transform(p, false));
                p = Transform(p, true);
            }
        }
        _patterns = patterns.Keys.Select(s => s.ToCharArray()).ToArray(); _weights = patterns.Values.ToArray();
        _words = (PatternCount + 63) / 64;
        _compatible = new ulong[4][][];
        for (int d = 0; d < 4; d++)
        {
            _compatible[d] = new ulong[PatternCount][];
            for (int a = 0; a < PatternCount; a++)
            {
                var bits = _compatible[d][a] = new ulong[_words];
                for (int b = 0; b < PatternCount; b++) if (Compatible(a, b, d)) bits[b / 64] |= 1UL << (b % 64);
            }
        }
        void Add(char[] p) { string key = new(p); patterns[key] = patterns.GetValueOrDefault(key) + 1; }
    }
    private char[] Transform(char[] p, bool rotate)
    {
        var result = new char[p.Length];
        for (int y = 0; y < _n; y++) for (int x = 0; x < _n; x++) result[y * _n + x] = rotate ? p[(_n - x - 1) * _n + y] : p[y * _n + _n - x - 1];
        return result;
    }
    public bool Compatible(int a, int b, int direction)
    {
        var (dx, dy) = Directions[direction];
        for (int y = 0; y < _n; y++) for (int x = 0; x < _n; x++)
        {
            int bx = x - dx, by = y - dy;
            if (bx >= 0 && by >= 0 && bx < _n && by < _n && _patterns[a][y * _n + x] != _patterns[b][by * _n + bx]) return false;
        }
        return true;
    }
    public CollapseResult? Solve(int width, int height, uint seed, IReadOnlyDictionary<int, char>? pins = null, int maxAttempts = 8)
    {
        if (width < _n || height < _n || (long)width * height > 16384 || maxAttempts is < 1 or > 8) throw new ArgumentException("WFC dimensions/attempt budget out of range.");
        int gw = width - _n + 1, gh = height - _n + 1, count = gw * gh;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var rng = new RandomStream(WorldRandom.Hash(unchecked((int)seed), attempt, salt: 810));
            var domains = new ulong[count][];
            var queue = new Queue<int>(); var queued = new bool[count];
            for (int i = 0; i < count; i++)
            {
                domains[i] = Enumerable.Repeat(ulong.MaxValue, _words).ToArray();
                if (PatternCount % 64 != 0) domains[i][^1] = (1UL << (PatternCount % 64)) - 1;
            }
            if (pins != null) foreach (var (cell, value) in pins)
            {
                if (cell < 0 || cell >= width * height) throw new ArgumentException("Pin outside output.");
                int px = cell % width, py = cell / width;
                for (int y = Math.Max(0, py - _n + 1); y <= Math.Min(gh - 1, py); y++)
                for (int x = Math.Max(0, px - _n + 1); x <= Math.Min(gw - 1, px); x++)
                {
                    int index = y * gw + x;
                    for (int p = 0; p < PatternCount; p++) if (_patterns[p][(py - y) * _n + px - x] != value) domains[index][p / 64] &= ~(1UL << (p % 64));
                    Enqueue(index);
                }
            }
            int observations = 0; long propagations = 0;
            bool valid = Propagate();
            while (valid)
            {
                int selected = -1; double minimum = double.PositiveInfinity;
                for (int i = 0; i < count; i++)
                {
                    int remaining = 0; double weight = 0, weightedLog = 0;
                    foreach (int p in Members(domains[i])) { remaining++; weight += _weights[p]; weightedLog += _weights[p] * Math.Log(_weights[p]); }
                    if (remaining == 0) { valid = false; break; }
                    if (remaining < 2) continue;
                    double entropy = Math.Log(weight) - weightedLog / weight + WorldRandom.Hash(unchecked((int)seed), i, attempt) / (double)uint.MaxValue * 1e-7;
                    if (entropy < minimum) { minimum = entropy; selected = i; }
                }
                if (!valid || selected == -1) break;
                int total = Members(domains[selected]).Sum(p => _weights[p]), choice = rng.Next(total), picked = -1;
                foreach (int p in Members(domains[selected])) { choice -= _weights[p]; if (choice < 0) { picked = p; break; } }
                Array.Clear(domains[selected]); domains[selected][picked / 64] = 1UL << (picked % 64); observations++;
                Enqueue(selected); valid = Propagate();
            }
            if (!valid) continue;
            var output = new char[width * height]; var choices = domains.Select(d => Members(d).First()).ToArray();
            for (int y = 0; y < gh; y++) for (int x = 0; x < gw; x++)
                for (int py = 0; py < _n; py++) for (int px = 0; px < _n; px++) output[(y + py) * width + x + px] = _patterns[choices[y * gw + x]][py * _n + px];
            return new(output, choices, attempt + 1, observations, propagations);

            void Enqueue(int i) { if (!queued[i]) { queued[i] = true; queue.Enqueue(i); } }
            bool Propagate()
            {
                var allowed = new ulong[_words];
                while (queue.TryDequeue(out int i))
                {
                    queued[i] = false;
                    if (domains[i].All(v => v == 0)) return false;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = i % gw + Directions[d].X, ny = i / gw + Directions[d].Y;
                        if (nx < 0 || ny < 0 || nx >= gw || ny >= gh) continue;
                        Array.Clear(allowed);
                        foreach (int p in Members(domains[i])) for (int word = 0; word < _words; word++) allowed[word] |= _compatible[d][p][word];
                        int neighbor = ny * gw + nx; bool changed = false;
                        for (int word = 0; word < _words; word++) { ulong next = domains[neighbor][word] & allowed[word]; changed |= next != domains[neighbor][word]; domains[neighbor][word] = next; }
                        propagations++;
                        if (changed) Enqueue(neighbor);
                    }
                }
                return true;
            }
        }
        return null;
    }
    private static IEnumerable<int> Members(ulong[] words)
    {
        for (int word = 0; word < words.Length; word++)
        {
            ulong bits = words[word];
            while (bits != 0) { int bit = BitOperations.TrailingZeroCount(bits); yield return word * 64 + bit; bits &= bits - 1; }
        }
    }
}
