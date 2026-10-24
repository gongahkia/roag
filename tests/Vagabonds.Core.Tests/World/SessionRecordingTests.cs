using System.Text.Json;
using Vagabonds.Core.Diagnostics;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class SessionRecordingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-recording-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    private static JsonElement Read(string file) => JsonDocument.Parse(File.ReadAllText(file)).RootElement.Clone();

    [Fact]
    public void ObserversAndSnapshotsPreserveExactRulesRngAndPersistence()
    {
        var baseline = WorldSession.Create(42);
        var events = new List<WorldDiagnosticEvent>();
        var observed = WorldSession.Create(42, diagnostics: events.Add);
        foreach (var world in new[] { baseline, observed })
        {
            world.DesignateWork(WorldCommandKind.Dig, [world.State.Position.Offset(500, 0)]);
            world.Submit(new(WorldCommandKind.Move, world.State.Position.Offset(500, 0)));
            world.Submit(new(WorldCommandKind.Forage));
            world.Paused = false; world.AdvanceTicks(15); world.Paused = true;
        }
        string before = JsonSerializer.Serialize(observed.State);
        int resident = observed.Chunks.ResidentCount;
        for (int i = 0; i < 5; i++) JsonSerializer.Serialize(observed.DiagnosticSnapshot());
        Assert.Equal(before, JsonSerializer.Serialize(observed.State)); Assert.Equal(resident, observed.Chunks.ResidentCount);
        Assert.Equal(JsonSerializer.Serialize(baseline.State), JsonSerializer.Serialize(observed.State));
        Assert.Contains(events, e => e.Kind == "chunk-generation");
        Assert.Equal(2, events.Count(e => e.Kind == "command"));
        Assert.Contains(events, e => e.Kind == "work-designation");
        observed.DiagnosticSink = _ => throw new IOException("disk unavailable");
        baseline.Submit(new(WorldCommandKind.Move, baseline.State.Position.Offset(500, 0)));
        Assert.False(observed.Submit(new(WorldCommandKind.Move, observed.State.Position.Offset(500, 0))).Success);
        string slot = Path.Combine(_root, "slot.json"); observed.Save(slot);
        var loaded = WorldSession.Load(slot, diagnostics: _ => throw new IOException("observer unavailable"));
        Assert.Equal(JsonSerializer.Serialize(baseline.State), JsonSerializer.Serialize(loaded.State));
    }

    [Fact]
    public void RealCommandsRewardsAndCrashesAreRetainedWithCompactMetrics()
    {
        string folder;
        using (var recorder = new LocalSessionRecorder(_root, new { seed = 42, selectedSave = "isolated-slot" }))
        {
            folder = recorder.Folder;
            var world = WorldSession.Create(42, diagnostics: recorder.Observe);
            world.Submit(new(WorldCommandKind.Move, world.State.Position.Offset(500, 0)));
            recorder.Snapshot(world.DiagnosticSnapshot(), world.State.Tick);
            int marker = recorder.Mark("Confusing destination", world.DiagnosticSnapshot(), world.State.Tick);
            recorder.Attachment(marker, [137, 80, 78, 71]);
            recorder.Crash(new InvalidOperationException("fixture crash"));
        }
        var summary = Read(Path.Combine(folder, "summary.json"));
        Assert.Equal("crashed", summary.GetProperty("status").GetString());
        Assert.Equal(1, summary.GetProperty("metrics").GetProperty("commandsRejected").GetDouble());
        Assert.True(summary.GetProperty("counts").GetProperty("chunk-generation").GetInt32() >= 9);
        Assert.Single(summary.GetProperty("markers").EnumerateArray());
        Assert.True(File.Exists(Path.Combine(folder, "latest.json")));
        Assert.True(File.Exists(Path.Combine(folder, "marker-00.png")));
        Assert.Contains("fixture crash", File.ReadAllText(Path.Combine(folder, "crash.json")));
        var lines = Directory.GetFiles(folder, "events-*.jsonl").SelectMany(File.ReadLines).Select(s => JsonDocument.Parse(s).RootElement.Clone()).ToArray();
        Assert.Contains(lines, e => e.GetProperty("kind").GetString() == "command" && e.GetProperty("data").GetProperty("input").GetProperty("kind").GetString() == "move");
        Assert.Equal(lines.Length, lines.Select(e => e.GetProperty("sequence").GetInt64()).Distinct().Count());
    }

    [Fact]
    public void RotationMarkersAndOversizedPayloadsRespectConfiguredBounds()
    {
        string folder;
        var policy = new RecordingPolicy(EventFiles: 2, EventFileBytes: 2048, QueueCapacity: 4096, MarkerSlots: 2, AttachmentBytes: 128);
        using (var recorder = new LocalSessionRecorder(_root, new { }, policy))
        {
            folder = recorder.Folder;
            for (int i = 0; i < 80; i++) recorder.Record("fixture", new { i, text = new string('x', 100) });
            recorder.Record("oversized", new string('x', 3000));
            for (int i = 0; i < 5; i++)
            {
                int slot = recorder.Mark("Moment " + i, new { i }, i);
                recorder.Attachment(slot, new byte[128]);
            }
            recorder.Attachment(0, new byte[129]);
        }
        var files = Directory.GetFiles(folder, "events-*.jsonl");
        Assert.Equal(2, files.Length); Assert.All(files, p => Assert.InRange(new FileInfo(p).Length, 1, 2048));
        Assert.Equal(2, Directory.GetFiles(folder, "marker-*.json").Length);
        Assert.Equal(2, Directory.GetFiles(folder, "marker-*.png").Length);
        var summary = Read(Path.Combine(folder, "summary.json"));
        Assert.Equal(80, summary.GetProperty("counts").GetProperty("fixture").GetInt32());
        Assert.Equal(1, summary.GetProperty("droppedEvents").GetInt32());
        Assert.Equal(2, summary.GetProperty("markers").GetArrayLength());
        Assert.Equal(1, summary.GetProperty("counts").GetProperty("attachment-skipped").GetInt32());
    }

    [Fact]
    public void RetentionNeverRemovesLiveOrForeignDirectories()
    {
        var policy = new RecordingPolicy(RetainedSessions: 2);
        using var active = new LocalSessionRecorder(_root, new { active = true }, policy);
        string foreign = Path.Combine(_root, "keep-me"); Directory.CreateDirectory(foreign); File.WriteAllText(Path.Combine(foreign, "save.json"), "untouched");
        for (int i = 0; i < 4; i++) using (var closed = new LocalSessionRecorder(_root, new { i }, policy)) { }
        LocalSessionRecorder.Prune(_root, policy);
        Assert.True(Directory.Exists(active.Folder)); Assert.True(Directory.Exists(foreign));
        Assert.InRange(Directory.GetFiles(_root, "session.json", SearchOption.AllDirectories).Length, 1, 3);
    }

    [Fact]
    public void RecordingInitializationAndSerializationFailuresAreNonfatal()
    {
        Directory.CreateDirectory(_root); string blocked = Path.Combine(_root, "file"); File.WriteAllText(blocked, "fixture");
        Assert.Null(LocalSessionRecorder.TryStart(blocked, new { }));
        using var recorder = new LocalSessionRecorder(_root, new { });
        recorder.Record("invalid-json-number", double.PositiveInfinity);
        var loop = new List<object>(); loop.Add(loop);
        Assert.Equal(-1, recorder.Mark("loop", loop, 0));
        Assert.Equal(2, recorder.Dropped);
    }

    [Fact]
    public void FatalContextSurvivesAnOverloadedEventQueue()
    {
        string folder;
        using (var recorder = new LocalSessionRecorder(_root, new { }, new RecordingPolicy(QueueCapacity: 1)))
        {
            folder = recorder.Folder;
            for (int i = 0; i < 2000; i++) recorder.Record("fixture", new { i });
            recorder.Crash(new IOException("fatal fixture"));
            Assert.True(File.Exists(Path.Combine(folder, "crash.json")));
        }
        Assert.Equal("crashed", Read(Path.Combine(folder, "summary.json")).GetProperty("status").GetString());
        Assert.Contains("fatal fixture", File.ReadAllText(Path.Combine(folder, "crash.json")));
    }

    [Fact]
    public void PerformanceKeepsIntervalTotalsAndABoundedRecentPercentile()
    {
        var window = new PerformanceWindow(); Assert.Null(window.Take());
        for (int i = 0; i < 1000; i++) window.Add(i < 300 ? 80 : 10, 2, i < 300 ? 50 : 6, 3, 1024, 12);
        window.Add(double.NaN, 0);
        var report = JsonSerializer.SerializeToElement(window.Take());
        Assert.Equal(1000, report.GetProperty("frames").GetInt32()); Assert.Equal(300, report.GetProperty("slowFrames").GetInt32());
        Assert.Equal(600, report.GetProperty("percentileSampleCount").GetInt32()); Assert.Equal(10, report.GetProperty("frameP95Ms").GetDouble());
        Assert.Equal(80, report.GetProperty("worstFrameMs").GetDouble()); Assert.Null(window.Take(true));
        Assert.Equal(6, report.GetProperty("renderP95Ms").GetDouble()); Assert.Equal(3, report.GetProperty("terrainP95Ms").GetDouble());
        Assert.Equal(1_024_000, report.GetProperty("allocatedBytes").GetInt64()); Assert.Equal(1024, report.GetProperty("allocatedBytesPerFrame").GetDouble());
        Assert.Equal(12, report.GetProperty("drawCallsPerFrame").GetDouble());
        window.Add(20, 1, 2, 1, 32, 4);
        report = JsonSerializer.SerializeToElement(window.Take(true));
        Assert.Equal(32, report.GetProperty("allocatedBytes").GetInt64()); Assert.Equal(4, report.GetProperty("drawCallsPerFrame").GetDouble());
    }
}
