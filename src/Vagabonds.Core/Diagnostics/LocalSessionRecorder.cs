using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Vagabonds.Core.World;

namespace Vagabonds.Core.Diagnostics;

public sealed record RecordingPolicy(int EventFiles = 4, int EventFileBytes = 1048576,
    int RetainedSessions = 20, long RetainedBytes = 268435456, int QueueCapacity = 512,
    int MarkerSlots = 8, int AttachmentBytes = 1048576);

/// <summary>Bounded, local, asynchronous evidence. Disk/queue failure never fails gameplay.</summary>
public sealed class LocalSessionRecorder : IDisposable
{
    private sealed record Entry(string Kind, long? Tick, JsonElement Data, int Slot = -1, byte[]? Attachment = null, DateTimeOffset Utc = default, double Elapsed = 0);
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };
    public static string DefaultRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".vagabonds", "sessions");
    public string Id { get; }
    public string Folder { get; }
    public string? Error { get; private set; }
    public long Dropped => Interlocked.Read(ref _dropped);
    public RecordingPolicy Policy { get; }
    private readonly string _root;
    private readonly Channel<Entry> _queue;
    private readonly FileStream _lease;
    private readonly Task _worker;
    private readonly Stopwatch _elapsed = Stopwatch.StartNew();
    private readonly DateTimeOffset _started = DateTimeOffset.UtcNow;
    private long _dropped;
    private int _disposed, _marker;

    public static LocalSessionRecorder? TryStart(string? root, object identity, RecordingPolicy? policy = null)
    {
        try { return new(root ?? DefaultRoot, identity, policy ?? new()); }
        catch (Exception e) { Console.Error.WriteLine("Session recording unavailable: " + e.Message); return null; }
    }

    public LocalSessionRecorder(string root, object identity, RecordingPolicy? policy = null)
    {
        Policy = policy ?? new();
        if (Policy.EventFiles is < 1 or > 16 || Policy.EventFileBytes is < 512 or > 4194304
            || Policy.RetainedSessions is < 1 or > 100 || Policy.RetainedBytes < 4096
            || Policy.QueueCapacity is < 1 or > 4096 || Policy.MarkerSlots is < 1 or > 16
            || Policy.AttachmentBytes is < 128 or > 4194304) throw new ArgumentException("Invalid recording bounds.");
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
        Prune(_root, Policy);
        Id = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8];
        Folder = Path.Combine(_root, Id); Directory.CreateDirectory(Folder);
        _lease = new(Path.Combine(Folder, "active.lock"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read);
        try
        {
            Atomic("session.json", new { schemaVersion = 1, id = Id, started = _started, processId = Environment.ProcessId,
                identity, policy = Policy, localOnly = true });
            _queue = Channel.CreateBounded<Entry>(new BoundedChannelOptions(Policy.QueueCapacity) { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
            _worker = Task.Run(WriteLoop);
            Record("session-start", identity);
        }
        catch { _lease.Dispose(); throw; }
    }

    public static object BuildIdentity(Assembly assembly) => new
    {
        version = assembly.GetName().Version?.ToString(),
        informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion,
        clientHash = Hash(assembly.Location), coreHash = Hash(typeof(WorldSession).Assembly.Location),
        runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
        platform = System.Runtime.InteropServices.RuntimeInformation.OSDescription
    };
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    public void Observe(WorldDiagnosticEvent e) => Record(e.Kind, e.Data, e.Tick);
    public void Record(string kind, object? data = null, long? tick = null)
    {
        if (_disposed != 0 || Error != null) return;
        try
        {
            var element = JsonSerializer.SerializeToElement(data, Json);
            if (Encoding.UTF8.GetByteCount(element.GetRawText()) > Math.Min(131072, Policy.EventFileBytes / 2))
            { Interlocked.Increment(ref _dropped); return; }
            Enqueue(new(kind, tick, element));
        }
        catch (Exception) { Interlocked.Increment(ref _dropped); }
    }
    public void Snapshot(object state, long tick) => Record("snapshot", state, tick);
    public int Mark(string label, object state, long tick)
    {
        try
        {
            if (_disposed != 0 || Error != null) return -1;
            int number = Interlocked.Increment(ref _marker), slot = (number - 1) % Policy.MarkerSlots;
            Enqueue(new("marker", tick, JsonSerializer.SerializeToElement(new { number, label = label[..Math.Min(500, label.Length)], state }, Json), slot));
            return slot;
        }
        catch (Exception) { Interlocked.Increment(ref _dropped); return -1; }
    }
    public void Attachment(int slot, byte[] png)
    {
        if (slot < 0 || slot >= Policy.MarkerSlots) return;
        if (png.Length > Policy.AttachmentBytes) { Record("attachment-skipped", new { slot, bytes = png.Length, reason = "size-limit" }); return; }
        Enqueue(new("attachment", null, JsonSerializer.SerializeToElement(new { slot, bytes = png.Length }), slot, png));
    }
    public void Crash(Exception e) => Record("crash", new { type = e.GetType().FullName,
        message = Short(e.Message, 4000), stack = Short(e.StackTrace ?? "", 12000), inner = Short(e.InnerException?.Message ?? "", 2000) });
    private static string Short(string value, int maximum) => value[..Math.Min(maximum, value.Length)];
    private void Enqueue(Entry e)
    {
        if (_disposed == 0 && Error == null && !_queue.Writer.TryWrite(e with { Utc = DateTimeOffset.UtcNow, Elapsed = _elapsed.Elapsed.TotalSeconds })) Interlocked.Increment(ref _dropped);
    }

    private async Task WriteLoop()
    {
        long sequence = 0, writtenBytes = 0;
        int fileIndex = 0;
        StreamWriter? stream = null;
        JsonElement? latest = null, crash = null;
        var counts = new Dictionary<string, long>();
        var commands = new Dictionary<string, long>();
        var metrics = new Dictionary<string, double>();
        var markers = new Dictionary<int, object>();
        double lastSummary = -1;
        try
        {
            await foreach (var e in _queue.Reader.ReadAllAsync())
            {
                if (e.Kind == "attachment") { File.WriteAllBytes(Path.Combine(Folder, $"marker-{e.Slot:00}.png"), e.Attachment!); continue; }
                counts[e.Kind] = counts.GetValueOrDefault(e.Kind) + 1;
                UpdateMetrics(e);
                string line = JsonSerializer.Serialize(new { schemaVersion = 1, sessionId = Id, sequence = ++sequence,
                    utc = e.Utc, elapsedSeconds = e.Elapsed, tick = e.Tick, kind = e.Kind, data = e.Data }, Json);
                int bytes = Encoding.UTF8.GetByteCount(line) + 1;
                if (bytes > Policy.EventFileBytes) { Interlocked.Increment(ref _dropped); continue; }
                if (stream == null || writtenBytes + bytes > Policy.EventFileBytes)
                {
                    if (stream != null) { await stream.DisposeAsync(); fileIndex = (fileIndex + 1) % Policy.EventFiles; }
                    stream = new(new FileStream(Path.Combine(Folder, $"events-{fileIndex:00}.jsonl"), FileMode.Create, FileAccess.Write, FileShare.Read), new UTF8Encoding(false)) { AutoFlush = true };
                    writtenBytes = 0;
                }
                await stream.WriteLineAsync(line); writtenBytes += bytes;
                if (e.Kind == "snapshot") { latest = e.Data; Atomic("latest.json", new { schemaVersion = 1, sessionId = Id, sequence, utc = DateTimeOffset.UtcNow, state = latest }); }
                if (e.Kind == "marker")
                {
                    markers[e.Slot] = new { slot = e.Slot, sequence, number = e.Data.GetProperty("number").GetInt32(), label = e.Data.GetProperty("label").GetString(), tick = e.Tick,
                        snapshot = $"marker-{e.Slot:00}.json", screenshot = $"marker-{e.Slot:00}.png" };
                    Atomic($"marker-{e.Slot:00}.json", new { schemaVersion = 1, sessionId = Id, sequence, utc = DateTimeOffset.UtcNow, data = e.Data });
                }
                if (e.Kind == "crash") { crash = e.Data; Atomic("crash.json", e.Data); }
                if (_elapsed.Elapsed.TotalSeconds - lastSummary >= 2 || e.Kind is "marker" or "crash" or "session-state")
                { Summary("running"); lastSummary = _elapsed.Elapsed.TotalSeconds; }
            }
            Summary(crash == null ? "closed" : "crashed");
        }
        catch (Exception e) { Error = e.Message; Console.Error.WriteLine("Session recording stopped: " + e.Message); }
        finally { if (stream != null) await stream.DisposeAsync(); }

        void Summary(string status) => Atomic("summary.json", new { schemaVersion = 1, id = Id, status,
            started = _started, updated = DateTimeOffset.UtcNow, elapsedSeconds = _elapsed.Elapsed.TotalSeconds,
            sequence, droppedEvents = Dropped, counts, commands, metrics, markers = markers.Values.ToArray(),
            crash, eventHistoryIsBounded = true, state = latest });
        void Add(string name, double amount = 1) => metrics[name] = metrics.GetValueOrDefault(name) + amount;
        void UpdateMetrics(Entry e)
        {
            var data = e.Data;
            if (e.Kind == "command" && data.TryGetProperty("input", out var input))
            {
                string key = input.GetProperty("kind").ToString(); commands[key] = commands.GetValueOrDefault(key) + 1;
                bool success = data.GetProperty("success").GetBoolean(); Add(success ? "commandsSucceeded" : "commandsRejected");
                if (success && key == "move") Add("tilesMoved");
            }
            if (e.Kind == "combat-feedback")
            {
                string kind = data.GetProperty("kind").GetString() ?? "";
                if (kind is "damage" or "heal") Add(kind, data.GetProperty("amount").GetDouble());
            }
            if (e.Kind == "encounter-start") Add("encounters");
            if (e.Kind == "encounter-end") Add(data.GetProperty("outcome").GetString() ?? "unknown");
            if (e.Kind == "chunk-generation")
            {
                Add("chunksGenerated"); Add("chunkGenerationMs", data.GetProperty("elapsedMs").GetDouble());
                metrics["slowestChunkMs"] = Math.Max(metrics.GetValueOrDefault("slowestChunkMs"), data.GetProperty("elapsedMs").GetDouble());
            }
            if (e.Kind == "performance")
            {
                Add("frames", data.GetProperty("frames").GetDouble()); Add("slowFrames", data.GetProperty("slowFrames").GetDouble());
                metrics["latestFrameP95Ms"] = data.GetProperty("frameP95Ms").GetDouble();
                metrics["latestUpdateP95Ms"] = data.GetProperty("updateP95Ms").GetDouble();
                metrics["worstFrameMs"] = Math.Max(metrics.GetValueOrDefault("worstFrameMs"), data.GetProperty("worstFrameMs").GetDouble());
            }
        }
    }

    private void Atomic(string name, object? value)
    {
        string path = Path.Combine(Folder, name), temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(value, Json)); File.Move(temporary, path, true);
    }

    public static void Prune(string root, RecordingPolicy policy)
    {
        if (!Directory.Exists(root)) return;
        var closed = new List<(string Path, long Bytes, DateTime Created)>();
        foreach (string folder in Directory.EnumerateDirectories(root))
        {
            try
            {
                if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0) continue;
                string manifest = Path.Combine(folder, "session.json");
                if (!File.Exists(manifest)) continue;
                using var doc = JsonDocument.Parse(File.ReadAllBytes(manifest));
                if (doc.RootElement.GetProperty("schemaVersion").GetInt32() != 1 || doc.RootElement.GetProperty("id").GetString() != Path.GetFileName(folder)) continue;
                using var lease = new FileStream(Path.Combine(folder, "active.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                long bytes = Directory.EnumerateFiles(folder).Sum(p => new FileInfo(p).Length);
                closed.Add((folder, bytes, Directory.GetCreationTimeUtc(folder)));
            }
            catch (Exception) { /* Active, foreign or damaged directory: leave it alone. */ }
        }
        long total = closed.Sum(f => f.Bytes); int count = closed.Count;
        foreach (var item in closed.OrderBy(f => f.Created))
        {
            if (count < policy.RetainedSessions && total <= policy.RetainedBytes) break;
            try
            {
                // Recheck the lease immediately before deletion.
                using (new FileStream(Path.Combine(item.Path, "active.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                Directory.Delete(item.Path, true); total -= item.Bytes; count--;
            }
            catch (Exception) { }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _queue.Writer.TryComplete();
        // A dead/full disk must not trap the game on exit. The background writer
        // retains a running manifest if hard termination prevents finalization.
        bool finished;
        try { finished = _worker.Wait(TimeSpan.FromSeconds(3)); }
        catch (Exception) { finished = true; }
        if (finished) { _lease.Dispose(); try { Prune(_root, Policy); } catch (Exception) { } }
    }
}
