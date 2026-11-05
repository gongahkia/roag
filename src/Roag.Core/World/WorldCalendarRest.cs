using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private const string OvernightRestReason = "Sleeping at home overnight";

    private bool WildlifeScheduledActive(Actor actor, long tick) => Ecology(actor)?.Activity switch
    {
        "day" => !IsNightAt(tick),
        "night" => IsNightAt(tick),
        _ => true
    };

    // Sleeping never erases a real provocation or a recent private observation.
    // Close threats wake defenders; timid species retain their own flight range.
    private bool WildlifeAwake(Actor actor, long tick)
    {
        if (State.NativeCampaign == null || WildlifeScheduledActive(actor, tick) || actor.ProvokedUntil > tick) return true;
        if (actor.Tactics?.Enemy?.Awareness is { } memory && State.Tactics is { } tactics
            && tactics.Queue.Clock - memory.ObservedAt < (long)TacticalCombat.EnemyMemorySeconds * tactics.Tuning.UnitsPerSecond
            && State.Party.Contains(memory.Target) && ActorById(memory.Target).Life != LifeState.Dead) return true;
        if (Ecology(actor)?.Behavior == "flee") return NativeFleeThreat(actor) != null;
        int notice = Ecology(actor)?.SleepingNotice ?? 2;
        var nearby = Party.Where(p => p.Life != LifeState.Dead && p.Position.Distance(actor.Position) <= notice).ToArray();
        if (nearby.Length == 0) return false;
        var sight = VisionFrom(actor.Position);
        return nearby.Any(p => sight.Contains(p.Position));
    }

    internal bool CalendarResting(Actor actor, long tick)
    {
        if (State.NativeCampaign == null || actor.Recruited || actor.Life != LifeState.Alive || actor.Body.Hunger >= Simulation.MealAt) return false;
        if (actor.Kind == ActorKind.Person)
            return actor.Tactics?.Resident?.Target is not > 0 && IsNightAt(tick) && actor.Routine is { Kind: RoutineKind.Rest, Reason: OvernightRestReason } routine
                && actor.Position.Distance(routine.Target) <= 1;
        return NativeCreatureNeeds(actor) && Ecology(actor) != null && !WildlifeAwake(actor, tick)
            && actor.Migration == null && actor.Habitat is { } home && actor.Position.Distance(home) <= 2;
    }

    private string WildlifeActivityDescription(Actor actor)
    {
        string? period = Ecology(actor)?.Activity;
        if (State.NativeCampaign == null || period is not ("day" or "night")) return "";
        return $"Active by {period}; rests at home otherwise. "
            + (CalendarResting(actor, State.Tick) ? "Resting now. " : "Awake now. ")
            + (Ecology(actor)?.Behavior == "flee" ? "Nearby privately seen travellers wake it to flee. "
                : $"Privately seen threats within {Ecology(actor)!.SleepingNotice} tiles or provocation wake it. ");
    }
}
