using Microsoft.Xna.Framework;
using Vagabonds.Core.World;

namespace Vagabonds.Client;

/// <summary>Small, intermittent work gestures; authoritative positions and the camera stay still.</summary>
internal static class WorldWorkMotion
{
    public static Vector2 Offset(WorldSession session, Actor actor, bool moving, double seconds, bool reducedMotion)
    {
        if (reducedMotion || moving || session.Frozen || session.PartyTravelling || actor.Life != LifeState.Alive) return Vector2.Zero;
        var order = session.State.Work.FirstOrDefault(o => o.Worker == actor.Id && o.Kind == WorkKind.Dig && !o.Suspended
            && o.Progress > 0 && o.Status.StartsWith("Working:", StringComparison.Ordinal) && actor.Position.Distance(o.Target) <= 1);
        if (order == null) return Vector2.Zero;
        double phase = (seconds + actor.Id % 11 * .137) % 1.7;
        if (phase >= .55) return Vector2.Zero;
        float envelope = (float)Math.Sin(phase / .55 * Math.PI);
        return new((float)Math.Sin(phase / .55 * Math.PI * 4) * envelope * .055f, -envelope * .025f);
    }
}
