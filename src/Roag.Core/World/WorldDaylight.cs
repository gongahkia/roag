namespace Roag.Core.World;

public sealed partial class WorldSession
{
    public int HourOfDay => (int)(State.Tick / TicksPerHour % 24);
    public bool IsNight => HourOfDay is < 6 or >= 20;

    /// <summary>One calendar and the observer's own weather region determine surface sight.</summary>
    public int SightRadiusAt(WorldPosition position)
    {
        if (!Plan.Contains(position)) throw new ArgumentOutOfRangeException(nameof(position));
        // Calendar effects belong to the attached world clock. Standalone
        // combat with a caller-supplied clock keeps its static geometry.
        if (position.Floor > 0 || State.Tactics is not { DrivesWorldClock: true, Tuning.EnableSurvival: true }) return 11;
        int weatherPenalty = WeatherAt(position).Weather switch
        {
            WeatherKind.Rain => 1,
            WeatherKind.Storm or WeatherKind.Snow => 3,
            _ => 0
        };
        return Math.Max(4, (IsNight ? 6 : 11) - weatherPenalty);
    }

    public string CalendarDescription => $"Day {State.Tick / TicksPerDay + 1} / {HourOfDay:00}:{State.Tick % TicksPerHour * 60 / TicksPerHour:00} / {(IsNight ? "night" : "daylight")}";
}
