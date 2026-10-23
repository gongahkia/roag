namespace Vagabonds.Core.Diagnostics;

/// <summary>Bounded recent samples plus totals for the entire reporting interval.</summary>
public sealed class PerformanceWindow
{
    private readonly double[] _frames = new double[600], _updates = new double[600];
    private int _count, _cursor, _slow;
    private double _seconds, _worst;
    public void Add(double frameMs, double updateMs)
    {
        if (!double.IsFinite(frameMs) || !double.IsFinite(updateMs) || frameMs < 0 || updateMs < 0) return;
        _frames[_cursor] = frameMs; _updates[_cursor] = updateMs; _cursor = (_cursor + 1) % _frames.Length;
        _count++; _seconds += frameMs / 1000; _worst = Math.Max(_worst, frameMs); if (frameMs > 50) _slow++;
    }
    public object? Take(bool force = false)
    {
        if (_count == 0 || !force && _seconds < 5) return null;
        int count = Math.Min(_count, _frames.Length);
        double Percentile(double[] values) => values.Take(count).Order().ElementAt((int)((count - 1) * .95));
        object result = new { frames = _count, seconds = _seconds, averageFps = _seconds == 0 ? 0 : _count / _seconds,
            slowFrames = _slow, slowFrameThresholdMs = 50, frameP95Ms = Percentile(_frames), updateP95Ms = Percentile(_updates),
            worstFrameMs = _worst, percentileSampleCount = count };
        _count = _cursor = _slow = 0; _seconds = _worst = 0;
        return result;
    }
}
