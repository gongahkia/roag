namespace Ithica.Core;

public enum CampaignPhase { Colony, Preparation, Expedition, Arrival }

public sealed class CampaignClock
{
    public const int TicksPerBeat = 1;
    public bool Paused { get; set; } = true;
    public int Speed { get; set; } = 1;
    public double Accumulator { get; set; }

    internal int Elapse(double seconds, bool colony)
    {
        if (!double.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        if (!colony || Paused) { Accumulator = 0; return 0; }
        Accumulator = Math.Min(8, Accumulator + Math.Min(seconds, .25) * 8 * Speed);
        int ticks = (int)Accumulator;
        Accumulator -= ticks;
        return ticks;
    }

    internal void Rebase() => Accumulator = 0;
}

public static class ActionCosts
{
    public const int Move = CampaignClock.TicksPerBeat, Wait = CampaignClock.TicksPerBeat;
    public const int Combat = CampaignClock.TicksPerBeat, Travel = 8, Rescue = 4;
    public const int Salvage = 4, Repair = 12, Treatment = 8, Rest = 32, Refill = 4, Clear = 4;
}
