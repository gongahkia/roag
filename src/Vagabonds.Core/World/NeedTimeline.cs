namespace Vagabonds.Core.World;

/// <summary>
/// A continuous need evaluated with the original ordered tick additions. The
/// read cache is ephemeral; querying or saving at different intervals cannot
/// change rounding, thresholds or the serialized value.
/// </summary>
internal struct NeedTimeline
{
    private double _value, _rate, _readValue;
    private long _epoch, _readTick;
    public readonly double Rate => _rate;

    public double At(long tick)
    {
        if (tick < _epoch) throw new ArgumentOutOfRangeException(nameof(tick), "Need time precedes its last change.");
        if (tick == _readTick) return _readValue;
        long from = tick >= _readTick ? _readTick : _epoch;
        double value = tick >= _readTick ? _readValue : _value;
        if (_rate != 0)
            for (long i = from; i < tick; i++)
            {
                value = Math.Clamp(value + _rate, 0, 100);
                if (value == 0 && _rate < 0 || value == 100 && _rate > 0) break;
            }
        if (tick >= _readTick) { _readTick = tick; _readValue = value; }
        return value;
    }
    public void Set(long tick, double value)
    {
        _epoch = _readTick = tick; _value = _readValue = value;
    }
    public void RateAt(long tick, double rate)
    {
        if (_rate == rate) return;
        Set(tick, At(tick)); _rate = rate;
    }
    public void Reanchor(long from, long to)
    {
        double value = At(from); Set(to, value);
    }
    public void Freeze(long tick)
    {
        double value = At(tick); _rate = 0; Set(0, value);
    }
}
