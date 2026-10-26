using Roag.Core.World;

namespace Roag.Core.Tactics;

/// <summary>Resolve current bodies or locked geometry according to the authored effect, in original order.</summary>
public static class BoonEffectTargets
{
    public static IReadOnlyList<WorldPosition> Select(BoonEffect effect, BoonActivation activation,
        IReadOnlyList<BoonActorView> actors, Func<WorldPosition, bool> open)
    {
        var e = activation.Event;
        var owner = actors.FirstOrDefault(a => a.Actor.Id == activation.OwnerId);
        if (effect.Kind is BoonEffectKind.DamageSource or BoonEffectKind.DamageTarget)
        {
            long? id = effect.Kind == BoonEffectKind.DamageSource ? e.SourceId : e.TargetId;
            var target = actors.FirstOrDefault(a => a.Actor.Id == id && a.Actor.Health > 0);
            return target is null ? [] : [target.Actor.Position];
        }
        if (effect.Kind == BoonEffectKind.Pattern)
        {
            if ((e.Position ?? e.From) is not { } center) return [];
            if (e.Position is null && effect.Pattern is BoonPattern.Rook or BoonPattern.Bishop) return [];
            return BoonGeometry.Pattern(effect.Pattern!.Value, new(center, e.PatternOrigin ?? e.SourcePosition, e.From), open);
        }
        if (effect.Kind is BoonEffectKind.AdjacentHostiles or BoonEffectKind.PathAdjacentHostiles)
        {
            if (owner is null) return [];
            IReadOnlyList<WorldPosition> path = effect.Kind == BoonEffectKind.PathAdjacentHostiles ? e.Path
                : e.Position is { } center ? [center] : [];
            return actors.Where(a => a.Actor.Id != owner.Actor.Id && a.Actor.Health > 0 && a.Faction != owner.Faction
                && path.Any(p => Adjacent(a.Actor.Position, p))).OrderBy(a => a.Actor.Id).Select(a => a.Actor.Position).ToArray();
        }
        if (effect.Kind == BoonEffectKind.StatusNeighbors)
            return e.Position is not { } center ? [] : actors.Where(a => a.Actor.Id != e.TargetId && a.Actor.Health > 0
                && a.Faction != activation.SourceFaction && a.Actor.Tactics?.Burning != null && Adjacent(a.Actor.Position, center))
                .OrderBy(a => a.Actor.Id).Select(a => a.Actor.Position).ToArray();
        if (e.ObservedPosition is not { } origin) return [];
        if (effect.Kind == BoonEffectKind.DamageNearest)
        {
            if (owner is null) return [];
            var best = actors.Where(a => a.Actor.Id != owner.Actor.Id && a.Actor.Id != e.TargetId && a.Actor.Health > 0
                && a.Faction != owner.Faction && a.Actor.Position.Floor == origin.Floor
                && a.Actor.Position.Distance(origin) <= effect.Radius
                && (effect.ClearPath != true || BoonGeometry.ClearSegment(origin, a.Actor.Position, open)))
                .OrderBy(a => a.Actor.Position.Distance(origin)).ThenBy(a => a.Actor.Id).FirstOrDefault();
            return best is null ? [] : [best.Actor.Position];
        }
        if (effect.Kind == BoonEffectKind.Area)
        {
            var cells = new List<WorldPosition>();
            int radius = effect.Radius!.Value;
            for (int y = -radius; y <= radius; y++) for (int x = -radius; x <= radius; x++) cells.Add(origin.Offset(x, y));
            return cells;
        }
        throw new ArgumentException("This effect does not select damage cells.", nameof(effect));
    }

    private static bool Adjacent(WorldPosition a, WorldPosition b) => a.Floor == b.Floor
        && Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y)) == 1;
}
