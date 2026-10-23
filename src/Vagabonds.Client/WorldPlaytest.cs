using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Vagabonds.Core.World;

namespace Vagabonds.Client;

internal sealed partial class WorldGame
{
    private readonly PlaytestBridge? _playtest;
    // Test sessions freeze between observations. Explicit waits advance the normal
    // clock/animation path; no controller injects world ticks or combat results.
    private bool InputFocused => _playtest == null ? IsActive : _playtest.RunsFrames;

    private Point[] ResolvePlaytest(PlaytestRequest request)
    {
        if (request.Kind is "wait" or "capture" or "stop" or "mark") return [];
        if (request.Kind == "control")
        {
            var control = _controls.LastOrDefault(c => c.Id == request.Control) ?? throw new ArgumentException("Control is not visible; observe the current controls.");
            if (!control.Enabled) throw new ArgumentException("Control unavailable: " + control.DisabledReason);
            if (WorldMouseControl.Hit(_controls, control.Bounds.Center)?.Id != control.Id) throw new ArgumentException("Control is covered by another control.");
            return [control.Bounds.Center];
        }
        if (request.Kind == "unit")
        {
            if (Playback.Busy || _session.State.Battle is not { } b || !b.Active.Friendly || _panel.Length != 0) throw new ArgumentException("Wait for an available party combat turn.");
            var unit = b.Units.SingleOrDefault(u => u.Id == request.Unit) ?? throw new ArgumentException("No such displayed combat unit.");
            return [UnitRect(unit).Center];
        }
        if (request.Kind == "point")
        {
            var point = new Point(request.X ?? -1, request.Y ?? -1);
            if (!GraphicsDevice.Viewport.Bounds.Contains(point)) throw new ArgumentException("Pointer is outside the game window.");
            return [point];
        }
        if (request.Kind == "actor")
        {
            if (_panel.Length != 0 || CombatStageVisible) throw new ArgumentException("Close the panel or finish combat before targeting map actors.");
            var actor = _session.State.Actors.FirstOrDefault(a => a.Id == request.Actor && a.Life != LifeState.Dead
                && _session.Visible.Contains(a.Position)) ?? throw new ArgumentException("Actor is not visible.");
            if (!_actorMotions.TryGetValue(actor.Id, out var motion)) throw new ArgumentException("Actor is not drawn.");
            var point = ActorRect(motion.Position).Center;
            if (!_map.Contains(point) || WorldMouseControl.Hit(_controls, point) != null) throw new ArgumentException("Actor is outside the map or covered.");
            return [point];
        }
        if (request.Kind is not ("tile" or "hover" or "drag")) throw new ArgumentException("Unknown mouse action.");
        if (_panel.Length != 0 || CombatStageVisible) throw new ArgumentException("Close the panel or finish combat before targeting map tiles.");
        Point Tile(int? x, int? y)
        {
            var position = new WorldPosition(x ?? -1, y ?? -1, _session.State.Position.Floor);
            if (!_session.Plan.Contains(position) || !_session.Chunks.Seen(position)) throw new ArgumentException("Tile is unobserved.");
            var point = TileRect(position).Center;
            if (!_map.Contains(point) || WorldMouseControl.Hit(_controls, point) != null) throw new ArgumentException("Tile center is outside the map or covered by a control.");
            return point;
        }
        var start = Tile(request.X, request.Y);
        return request.Kind == "drag" ? [start, Tile(request.EndX, request.EndY)] : [start];
    }

    private void PublishPlaytest(int width, int height)
    {
        if (_playtest?.NeedsObservation != true) return;
        // Reflect the held boundary in diagnostics after this update has completed.
        _session.Focused = false;
        BuildMouseControls(width, height);
        _playtest.Publish(BuildUiReport(width, height, playerView: true), path =>
        {
            if (_frame == null) throw new IOException("The game has not rendered a frame.");
            using var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write);
            _frame.SaveAsPng(stream, _frame.Width, _frame.Height); stream.Dispose(); File.Move(path + ".tmp", path, true);
        });
        RecordSnapshot();
        if (_playtest.StopRequested) Exit();
    }
}
