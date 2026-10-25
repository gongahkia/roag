namespace Vagabonds.Core.World;

public sealed record ChunkPreparationStatus(int Requested, int Ready, int InFlight, long Hits, long Discarded, long Failed);

/// <summary>
/// One finite CPU worker, independent of the demand generator. Prepared chunks enter
/// the resident cache only when requested by Get, so completion timing cannot change
/// demand order, eviction order, visibility, random streams or saved state.
/// </summary>
internal sealed class ChunkPreparationQueue(Func<ChunkKey, GeneratedChunk> generate) : IDisposable
{
    public const int Capacity = 9;
    private sealed class Entry(ChunkKey key)
    {
        public ChunkKey Key { get; } = key;
        public bool Started { get; set; }
        public GeneratedChunk? Chunk { get; set; }
    }
    private readonly object _gate = new();
    private List<Entry> _wanted = [];
    private Entry? _current;
    private Task? _worker;
    private bool _disposed;
    private long _hits, _discarded, _failed;

    public void Request(IEnumerable<ChunkKey> keys)
    {
        var ordered = keys.Distinct().Take(Capacity).ToArray();
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var next = new List<Entry>(ordered.Length);
            foreach (var key in ordered) next.Add(_wanted.FirstOrDefault(e => e.Key == key) ?? new(key));
            foreach (var entry in _wanted) if (!next.Contains(entry) && entry != _current) _discarded++;
            _wanted = next;
            if (_worker == null && _wanted.Any(e => !e.Started)) _worker = Task.Run(Run);
        }
    }
    public GeneratedChunk? TakeReady(ChunkKey key)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            int index = _wanted.FindIndex(e => e.Key == key);
            if (index < 0) return null;
            var entry = _wanted[index]; _wanted.RemoveAt(index);
            if (entry.Chunk is not { } chunk) { if (entry != _current) _discarded++; return null; }
            _hits++; return chunk;
        }
    }
    private void Run()
    {
        while (true)
        {
            Entry entry;
            lock (_gate)
            {
                if (_disposed || _wanted.FirstOrDefault(e => !e.Started) is not { } next)
                { _worker = null; return; }
                entry = next; entry.Started = true; _current = entry;
            }
            GeneratedChunk? chunk = null;
            try { chunk = generate(entry.Key); }
            catch (Exception) { lock (_gate) _failed++; }
            lock (_gate)
            {
                if (!_disposed && _wanted.Contains(entry)) entry.Chunk = chunk;
                else _discarded++;
                _current = null;
            }
        }
    }
    public ChunkPreparationStatus Status
    {
        get { lock (_gate) return new(_wanted.Count, _wanted.Count(e => e.Chunk != null), _current == null ? 0 : 1, _hits, _discarded, _failed); }
    }
    // Captures the current finite batch. No polling/sleep or graphics dependency.
    public Task Completion { get { lock (_gate) return _worker ?? Task.CompletedTask; } }
    public void Dispose()
    {
        // An active bounded generation may finish, but it cannot publish or start
        // another chunk. Never block the game window while shutting down/reloading.
        lock (_gate) { _disposed = true; _wanted.Clear(); }
    }
}
