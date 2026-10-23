using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Vagabonds.Client;

internal sealed record PlaytestRequest
{
    public int SchemaVersion { get; init; }
    public required string Id { get; init; }
    public long Observation { get; init; }
    public required string Kind { get; init; }
    public string? Control { get; init; }
    public int? X { get; init; }
    public int? Y { get; init; }
    public int? EndX { get; init; }
    public int? EndY { get; init; }
    public int? Unit { get; init; }
    public long? Actor { get; init; }
    public int Button { get; init; } = 1;
    public double Seconds { get; init; }
    public string? Label { get; init; }
}

/// <summary>Explicit local test transport. The only gameplay output is ordinary mouse states.</summary>
internal sealed class PlaytestBridge
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    private readonly string _folder;
    private readonly Queue<MouseState> _mouse = [];
    private readonly Queue<string> _ids = [];
    private PlaytestRequest? _active;
    private double _wait, _poll;
    private MouseState _last;
    private bool _initial = true;
    private readonly System.Diagnostics.Stopwatch _idle = System.Diagnostics.Stopwatch.StartNew();
    public long Revision { get; private set; }
    public bool Holding => _active == null;
    public bool RunsFrames => _active is { Kind: not ("capture" or "mark" or "stop") };
    // A semantic click targets the held screenshot. Keep world-space hit boxes
    // stable until its mouse edges are delivered; explicit waits animate travel.
    public bool AdvancesWorldPresentation => _active is { Kind: "wait" };
    public bool NeedsObservation => _initial || _active != null && _mouse.Count == 0 && _wait <= 0;
    public bool StopRequested { get; private set; }

    public PlaytestBridge(string folder)
    {
        _folder = Path.GetFullPath(folder); Directory.CreateDirectory(_folder);
        if (File.Exists(Path.Combine(_folder, "observation.json"))) throw new ArgumentException("Playtest control directory must be fresh.");
    }

    public MouseState Next(double elapsed, Func<PlaytestRequest, Point[]> resolve, Action<string> mark)
    {
        if (_active != null)
        {
            if (_wait > 0) _wait = Math.Max(0, _wait - Math.Clamp(elapsed, 0, .2));
            if (_mouse.TryDequeue(out var mouse)) _last = mouse;
            return _last;
        }
        _poll -= elapsed;
        if (_idle.Elapsed > TimeSpan.FromMinutes(10)) StopRequested = true;
        if (_initial || _poll > 0) return _last;
        _poll = .025;
        string path = Path.Combine(_folder, "request.json");
        if (!File.Exists(path)) return _last;
        PlaytestRequest? request = null;
        try
        {
            if (new FileInfo(path).Length > 16384) throw new ArgumentException("Request exceeds 16 KiB.");
            request = JsonSerializer.Deserialize<PlaytestRequest>(File.ReadAllBytes(path), Json) ?? throw new ArgumentException("Empty request.");
            File.Delete(path);
            if (request.SchemaVersion != 1) throw new ArgumentException("Unsupported protocol version.");
            if (request.Kind is not ("control" or "tile" or "actor" or "unit" or "point" or "hover" or "drag" or "wait" or "capture" or "mark" or "stop")) throw new ArgumentException("Unknown mouse action.");
            if (string.IsNullOrWhiteSpace(request.Id) || request.Id.Length > 80 || request.Id.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_'))) throw new ArgumentException("Invalid request ID.");
            if (_ids.Contains(request.Id)) throw new ArgumentException("Duplicate request ID; actions are never replayed.");
            _ids.Enqueue(request.Id); if (_ids.Count > 1024) throw new ArgumentException("Playtest request budget exhausted.");
            if (request.Observation != Revision) throw new ArgumentException("Stale observation; observe again before acting.");
            if (request.Button is not (1 or 3)) throw new ArgumentException("Use mouse button 1 or 3.");
            var points = resolve(request);
            switch (request.Kind)
            {
                case "wait":
                    if (!double.IsFinite(request.Seconds) || request.Seconds is < .05 or > 30) throw new ArgumentException("Wait 0.05..30 real seconds.");
                    _wait = request.Seconds; break;
                case "mark": mark(request.Label ?? "Agent marked this moment"); break;
                case "stop": StopRequested = true; break;
                case "capture": break;
                case "hover": _mouse.Enqueue(State(points[0], false, request.Button)); break;
                case "drag":
                    _mouse.Enqueue(State(points[0], false, 1)); _mouse.Enqueue(State(points[0], true, 1));
                    for (int i = 1; i <= 6; i++) _mouse.Enqueue(State(new(points[0].X + (points[1].X - points[0].X) * i / 6,
                        points[0].Y + (points[1].Y - points[0].Y) * i / 6), true, 1));
                    _mouse.Enqueue(State(points[1], false, 1)); break;
                default:
                    _mouse.Enqueue(State(points[0], false, request.Button)); _mouse.Enqueue(State(points[0], true, request.Button));
                    _mouse.Enqueue(State(points[0], false, request.Button)); break;
            }
            _active = request;
            _idle.Restart();
            if (_mouse.TryDequeue(out var first)) _last = first;
        }
        catch (Exception e) when (e is ArgumentException or JsonException or IOException or UnauthorizedAccessException)
        {
            File.Delete(path);
            Atomic("response.json", new { schemaVersion = 1, id = request?.Id ?? "invalid", success = false,
                reason = e.Message, observation = Revision, utc = DateTimeOffset.UtcNow });
        }
        return _last;
    }

    internal static MouseState State(Point point, bool pressed, int button) => new(point.X, point.Y, 0,
        pressed && button == 1 ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released,
        pressed && button == 3 ? ButtonState.Pressed : ButtonState.Released, ButtonState.Released, ButtonState.Released);

    public void Publish(string ui, Action<string> capture)
    {
        if (!NeedsObservation) return;
        string? captureError = null;
        File.Delete(Path.Combine(_folder, "frame.png"));
        try { capture(Path.Combine(_folder, "frame.png")); }
        catch (Exception e) { captureError = e.Message; }
        Revision++;
        using var document = JsonDocument.Parse(ui);
        Atomic("observation.json", new { schemaVersion = 1, observation = Revision, utc = DateTimeOffset.UtcNow,
            agentHold = true, inputBackend = "client-mouse-states", screenshot = captureError == null ? "frame.png" : null,
            captureError, ui = document.RootElement });
        if (_active != null)
            Atomic("response.json", new { schemaVersion = 1, id = _active.Id, success = true, reason = "Mouse action delivered; inspect the observation for its gameplay result.",
                observation = Revision, utc = DateTimeOffset.UtcNow, captureError });
        _active = null; _initial = false;
    }
    private void Atomic(string name, object data)
    {
        string path = Path.Combine(_folder, name), temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(data, Json)); File.Move(temporary, path, true);
    }
}
