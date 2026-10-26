namespace Roag.Core.Tactics;

/// <summary>Integer action time from Roag's original scheduler, independent of render time.</summary>
public static class ActionTiming
{
    public static long Recovery(int baseCost, int speed)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseCost);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(speed);
        // Round half upward, exactly as game.lua effective_cost. Widen before multiplying.
        return Math.Max(1, ((long)baseCost * 200 + speed) / ((long)speed * 2));
    }
}
