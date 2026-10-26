namespace Roag.Client;

internal sealed partial class WorldGame
{
    private object RecordingSnapshot() => new { world = _session.DiagnosticSnapshot(), ui = new { panel = _panel, nativeFocused = IsActive,
        notice = _notice, routeCount = _route.Count, pendingInteraction = _pendingInteraction,
        camera = new { x = _camera.Center.X, y = _camera.Center.Y, cellSize = _camera.CellSize },
        combatPresentation = new { Playback.Busy, Playback.Phase, Playback.Progress, _combatSpeed, _reducedMotion } } };
    private void RecordSnapshot() => _recording?.Snapshot(RecordingSnapshot(), _session.State.Tick);
    private void MarkMoment(string label)
    {
        if (_recording == null) { _notice = "Session recording is disabled."; return; }
        _markerSlot = _recording.Mark(label, RecordingSnapshot(), _session.State.Tick);
        _notice = _markerSlot < 0 ? "Session recording is unavailable." : "Moment marked in session " + _recording.Id + ".";
    }
    private void CaptureMarker()
    {
        int slot = _markerSlot; _markerSlot = -1;
        try
        {
            if (_frame == null || _recording == null) return;
            using var stream = new MemoryStream(); _frame.SaveAsPng(stream, _frame.Width, _frame.Height);
            _recording.Attachment(slot, stream.ToArray());
        }
        catch (Exception e) { _recording?.Record("attachment-failed", new { type = e.GetType().Name, e.Message }); }
    }
}
