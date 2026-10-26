using Roag.Core.World;

namespace Roag.Core.Tactics;

/// <summary>Native predicate interpreter over a typed condition tree and explicit world/event facts.</summary>
public static class BoonConditions
{
    public static bool Observes(BoonActorView owner, BoonScope scope, BoonEvent e)
    {
        if (scope.Kind == BoonScopeKind.Owner) return e.SourceId == owner.Actor.Id || e.TargetId == owner.Actor.Id;
        if (e.ObservedPosition is not { } p || p.Floor != owner.Actor.Position.Floor) return false;
        return scope.Kind == BoonScopeKind.Floor || Distance(p, owner.Actor.Position) <= scope.Radius;
    }

    public static bool Evaluate(BoonPredicate? predicate, BoonEvaluationContext world, BoonActorView owner,
        string boon, BoonEvent e, BoonChainHistory history)
    {
        // Short-circuit order is observable: unique/group predicates reserve identities even if a later condition fails.
        switch (predicate)
        {
            case null: return true;
            case AllBoonPredicates all:
                foreach (var child in all.Children) if (!Evaluate(child, world, owner, boon, e, history)) return false;
                return true;
            case AnyBoonPredicates any:
                foreach (var child in any.Children) if (Evaluate(child, world, owner, boon, e, history)) return true;
                return false;
            case NotBoonPredicate not: return !Evaluate(not.Child, world, owner, boon, e, history);
            case BoonCondition c: return Leaf(c, world, owner, boon, e, history);
            default: throw new ArgumentException("Unknown boon predicate.", nameof(predicate));
        }
    }

    private static bool Leaf(BoonCondition c, BoonEvaluationContext world, BoonActorView owner,
        string boon, BoonEvent e, BoonChainHistory history)
    {
        var actor = owner.Actor;
        var at = actor.Position;
        var memory = actor.Tactics?.Boons.Memory.GetValueOrDefault(boon);
        bool Hostile(string? faction) => faction != null && faction != owner.Faction;
        bool Axis(WorldPosition? origin) => e.Position is { } p && BoonGeometry.Axis(p, origin,
            c.Text == "cardinal" ? BoonAxis.Cardinal : BoonAxis.Diagonal);
        bool Terrain(WorldPosition? p) => p is { } tile && world.Terrain(tile) == c.Text;
        bool OnOwnerFloor(WorldPosition? p) => p?.Floor == at.Floor;
        switch (c.Op)
        {
            case BoonPredicateKind.SourceOwner: return e.SourceId == actor.Id;
            case BoonPredicateKind.TargetOwner: return e.TargetId == actor.Id;
            case BoonPredicateKind.SourceNotOwner: return e.SourceId != actor.Id;
            case BoonPredicateKind.SourceHostile: return Hostile(e.SourceFaction);
            case BoonPredicateKind.TargetHostile: return Hostile(e.TargetFaction);
            case BoonPredicateKind.SourceId: return e.SourceId == c.Number;
            case BoonPredicateKind.TargetId: return e.TargetId == c.Number;
            case BoonPredicateKind.SourceFaction: return e.SourceFaction == c.Text;
            case BoonPredicateKind.TargetFaction: return e.TargetFaction == c.Text;
            case BoonPredicateKind.SourceTag: return e.SourceTags.Contains(c.Text!);
            case BoonPredicateKind.TargetTag: return e.TargetTags.Contains(c.Text!);
            case BoonPredicateKind.SourceTargetSameFaction:
                return e.SourceId != e.TargetId && e.SourceFaction != null && e.SourceFaction == e.TargetFaction;
            case BoonPredicateKind.DirectAttack: return e.DirectAttack;
            case BoonPredicateKind.Positive: return e.ActualLoss > 0;
            case BoonPredicateKind.OtherBoon: return e.BoonId != null && e.BoonId != boon;
            case BoonPredicateKind.AbilityId: return e.AbilityId == c.Text;
            case BoonPredicateKind.EventKind: return e.Kind.ToString() == c.Text;
            case BoonPredicateKind.EventStatus: return e.Status?.ToString().ToLowerInvariant() == c.Text;
            case BoonPredicateKind.DamageType: return e.DamageType?.ToString().ToLowerInvariant() == c.Text;
            case BoonPredicateKind.TerrainId: return e.TerrainId == c.Text;
            case BoonPredicateKind.SupplyKind: return e.SupplyKind == c.Text;
            case BoonPredicateKind.SupplyExcess: return (e.Amount ?? 0) > (e.ActualGain ?? 0);
            case BoonPredicateKind.EventAmount:
                return (c.Field switch { BoonEventField.Amount => e.Amount, BoonEventField.ActualGain => e.ActualGain,
                    BoonEventField.Price => e.Price, _ => null }) >= c.Min;
            case BoonPredicateKind.RewardedDrop:
                return !e.RewardExempt && e.TargetDefinitionId is { } id && world.CurrencyDrop(id) > 0;
            case BoonPredicateKind.RollFace: return e.FateFace == c.Number;
            case BoonPredicateKind.UniqueAction:
                return e.RootActionId is { } action && history.Actions.Add(new(actor.Id, boon, action));
            case BoonPredicateKind.UniqueWindup:
                return e.WindupId is { } windup && history.Windups.Add(new(actor.Id, boon, windup));
            case BoonPredicateKind.DistinctChainGroup: return history.Distinct(actor.Id, boon, c.Field, e, c.Size!.Value);
            case BoonPredicateKind.PairAdjacentDamage: return history.Candidates(owner, boon, e, c.Neighborhood!.Value).Count > 0;
            case BoonPredicateKind.OwnerMissingHp: return actor.Health > 0 && actor.Health < actor.MaxHealth;
            case BoonPredicateKind.OwnerHpPercent:
                return (long)(e.SourceHpAtAttack ?? actor.Health) * 100 <= (long)(e.SourceMaxHpAtAttack ?? actor.MaxHealth) * c.Max;
            case BoonPredicateKind.TargetWasFull: return e.HpBefore != null && e.HpBefore == e.TargetMaxHp;
            case BoonPredicateKind.TargetHpPercentBefore:
                return e.HpBefore != null && e.TargetMaxHp != null && (long)e.HpBefore * 100 <= (long)e.TargetMaxHp * c.Max;
            case BoonPredicateKind.ActiveWard: return actor.Tactics?.Boons.WardTotal(world.Clock) > 0;
            case BoonPredicateKind.Primed: return memory?.Charges > 0 && memory.ExpiresAt >= world.Clock;
            case BoonPredicateKind.CooldownReady: return memory?.ExpiresAt is not { } expiry || world.Clock >= expiry;
            case BoonPredicateKind.BurningTarget: return e.TargetBurning == true;
            case BoonPredicateKind.BurningPair: return e.TargetBurning == true && e.AdjacentBurningCount > 0;
            case BoonPredicateKind.BackToWall: return e.WallBacked;
            case BoonPredicateKind.BasicAttack:
                return actor.Tactics?.Build is { } basic && e.AbilityId == ClassCatalog.Get(basic.Class).BasicAbilityId;
            case BoonPredicateKind.SpecialAttack:
                return actor.Tactics?.Build is { } special && e.AbilityId == ClassCatalog.Get(special.Class).SpecialAbilityId;
            case BoonPredicateKind.RegularTarget: return world.Actors.Any(a => a.Actor.Id == e.TargetId && a.RegularTarget);
            case BoonPredicateKind.TargetEntityHostile:
                return world.Actors.Any(a => a.Actor.Id == e.TargetId && a.Actor.Health > 0 && Hostile(a.Faction));
            case BoonPredicateKind.PatternAxis: return Axis(e.PatternOrigin ?? e.SourcePosition);
            case BoonPredicateKind.OwnerAxis: return Axis(at);
            case BoonPredicateKind.ActualDash:
                return e.Kind == BoonTrigger.Moved && e.AbilityId == "core:ability/dash" && world.LastActionWasDash;
            case BoonPredicateKind.FromTerrain: return Terrain(e.From);
            case BoonPredicateKind.PathTerrain: return e.Path.Any(p => p.Floor == at.Floor && Terrain(p));
            case BoonPredicateKind.SourceOnTerrain: return OnOwnerFloor(e.SourcePosition) && Terrain(e.SourcePosition);
            case BoonPredicateKind.OnSpecialTerrain: return e.Position is { } sp && world.SpecialTerrain(sp) == c.Text;
            case BoonPredicateKind.TargetNearTerrain:
                if (e.Position is not { } center || center.Floor != at.Floor) return false;
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) if (Terrain(center.Offset(x, y))) return true;
                return false;
            case BoonPredicateKind.VisibleSource: return e.SourcePosition is { } source && world.Visible(actor.Id, source);
            case BoonPredicateKind.VisibleEvent: return e.ObservedPosition is { } observed && world.Visible(actor.Id, observed);
            case BoonPredicateKind.EventDistance:
                return e.ObservedPosition is { } pos && pos.Floor == at.Floor && Distance(pos, at) >= c.Min && Distance(pos, at) <= c.Max;
            case BoonPredicateKind.SourceInRadius:
                return e.SourcePosition is { } origin && origin.Floor == at.Floor && Distance(origin, at) <= c.Radius;
            case BoonPredicateKind.ApproachingOwner:
                return e.Position is { } to && e.From is { } from && to.Floor == at.Floor && from.Floor == at.Floor
                    && Distance(to, at) < Distance(from, at);
            case BoonPredicateKind.OwnerInLockedCells: return e.TargetCells.Contains(at);
            case BoonPredicateKind.OverlappingWindups:
                return world.Actors.Any(a => a.Actor.Id != e.SourceId && a.Actor.Health > 0 && Hostile(a.Faction)
                    && a.LockedCells != null && a.LockedCells.Any(e.TargetCells.Contains));
            case BoonPredicateKind.AlignedHostiles:
                if (e.Position is not { } alignment) return false;
                int row = 0, column = 0;
                foreach (var other in world.Actors)
                {
                    var p = other.Actor.Position;
                    if (other.Actor.Health <= 0 || !Hostile(other.Faction) || p.Floor != alignment.Floor) continue;
                    if (p.Y == alignment.Y) row++;
                    if (p.X == alignment.X) column++;
                }
                return Math.Max(row, column) >= c.Min;
            case BoonPredicateKind.AdjacentHostiles:
                if (e.AdjacentHostileCount is { } count) return count >= c.Min;
                if (e.Position is not { } adjacent) return false;
                return world.Actors.Count(a => a.Actor.Id != actor.Id && a.Actor.Health > 0 && Hostile(a.Faction)
                    && a.Actor.Position.Floor == adjacent.Floor
                    && Math.Max(Math.Abs(a.Actor.Position.X - adjacent.X), Math.Abs(a.Actor.Position.Y - adjacent.Y)) == 1) >= c.Min;
            default: throw new ArgumentOutOfRangeException(nameof(c), "Unimplemented boon predicate.");
        }
    }

    private static long Distance(WorldPosition a, WorldPosition b) => Math.Abs((long)a.X - b.X) + Math.Abs((long)a.Y - b.Y);
}
