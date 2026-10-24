namespace Vagabonds.Core.Diagnostics;

/// <summary>Bounded recent samples plus totals for the entire reporting interval.</summary>
public sealed class PerformanceWindow
{
    private readonly double[] _frames = new double[600], _updates = new double[600], _renders = new double[600], _terrain = new double[600];
    private int _count, _cursor, _slow;
    private double _seconds, _worst;
    private long _allocatedBytes, _drawCalls;
    public void Add(double frameMs, double updateMs, double renderMs = 0, double terrainMs = 0, long allocatedBytes = 0, long drawCalls = 0)
    {
        if (!double.IsFinite(frameMs) || !double.IsFinite(updateMs) || !double.IsFinite(renderMs) || !double.IsFinite(terrainMs)
            || frameMs < 0 || updateMs < 0 || renderMs < 0 || terrainMs < 0 || allocatedBytes < 0 || drawCalls < 0) return;
        _frames[_cursor] = frameMs; _updates[_cursor] = updateMs; _renders[_cursor] = renderMs; _terrain[_cursor] = terrainMs;
        _cursor = (_cursor + 1) % _frames.Length;
        _allocatedBytes += allocatedBytes; _drawCalls += drawCalls;
        _count++; _seconds += frameMs / 1000; _worst = Math.Max(_worst, frameMs); if (frameMs > 50) _slow++;
    }
    public object? Take(bool force = false)
    {
        if (_count == 0 || !force && _seconds < 5) return null;
        int count = Math.Min(_count, _frames.Length);
        double Percentile(double[] values) => values.Take(count).Order().ElementAt((int)((count - 1) * .95));
        object result = new { frames = _count, seconds = _seconds, averageFps = _seconds == 0 ? 0 : _count / _seconds,
            slowFrames = _slow, slowFrameThresholdMs = 50, frameP95Ms = Percentile(_frames), updateP95Ms = Percentile(_updates),
            renderP95Ms = Percentile(_renders), terrainP95Ms = Percentile(_terrain), allocatedBytes = _allocatedBytes,
            allocatedBytesPerFrame = _allocatedBytes / (double)_count, drawCallsPerFrame = _drawCalls / (double)_count,
            worstFrameMs = _worst, percentileSampleCount = count };
        _count = _cursor = _slow = 0; _seconds = _worst = 0;
        _allocatedBytes = _drawCalls = 0;
        return result;
    }
}
