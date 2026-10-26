using Microsoft.Xna.Framework;
using Roag.Core.World;

namespace Roag.Client;

/// <summary>Small, intermittent work gestures; authoritative positions and the camera stay still.</summary>
internal static class WorldWorkMotion
{
    public static Vector2 Offset(WorldSession session, Actor actor, bool moving, double seconds, bool reducedMotion)
    {
        if (reducedMotion || moving || session.Frozen || session.PartyTravelling && actor.Recruited || actor.Life != LifeState.Alive) return Vector2.Zero;
        var order = session.State.Work.FirstOrDefault(o => o.Worker == actor.Id && o.Kind == WorkKind.Dig && o.Output == null && !o.Suspended
            && o.Progress > 0 && o.Status.StartsWith("Working:", StringComparison.Ordinal) && actor.Position.Distance(o.Target) <= 1);
        bool occupation = !actor.Recruited && actor.Routine?.Kind == RoutineKind.Occupation
            && actor.Body.RestUntil <= session.State.Tick && actor.Body.Fatigue < 75 && actor.Body.Hunger < session.Simulation.StarveAt
            && actor.Health >= actor.MaxHealth / 4 && actor.Position.Floor == 0 && session.State.Battle?.ActorIds.Contains(actor.Id) != true
            && !session.State.Work.Any(o => o.Worker == actor.Id && !o.Suspended)
            && actor.Occupation is { Kind: OccupationKind.Fell or OccupationKind.Quarry, Phase: OccupationPhase.Working, Progress: > 0 } job
            && actor.Position.Distance(job.Target) <= 1;
        if (order == null && !occupation) return Vector2.Zero;
        double phase = (seconds + actor.Id % 11 * .137) % 1.7;
        if (phase >= .55) return Vector2.Zero;
        float envelope = (float)Math.Sin(phase / .55 * Math.PI);
        return new((float)Math.Sin(phase / .55 * Math.PI * 4) * envelope * .055f, -envelope * .025f);
    }
}
