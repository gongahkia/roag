using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum EnemyTargetPriority { NearestThreat, VulnerablePrey, ClearShot }

internal sealed record EnemyTargetCandidate(long Id, WorldPosition Position, int Health, int MaximumHealth, LifeState Life, bool ClearShot);

public static class EnemyTargeting
{
    public static string Description(EnemyTargetPriority priority) => priority switch
    {
        EnemyTargetPriority.NearestThreat => "Targets nearby standing threats first; attacks downed prey when no standing target is detected.",
        EnemyTargetPriority.VulnerablePrey => "Targets downed prey first, then the lowest health percentage; prefers nearer prey on ties.",
        EnemyTargetPriority.ClearShot => "Targets standing threats first, favoring a usable shot now, then proximity; can target downed prey when no standing target is detected.",
        _ => throw new ArgumentOutOfRangeException(nameof(priority))
    };

    internal static EnemyTargetCandidate? Choose(EnemyTargetPriority priority, WorldPosition source, IEnumerable<EnemyTargetCandidate> candidates)
    {
        if (!Enum.IsDefined(priority)) throw new ArgumentOutOfRangeException(nameof(priority));
        return candidates.Order(Comparer<EnemyTargetCandidate>.Create(Compare)).FirstOrDefault();

        int Compare(EnemyTargetCandidate a, EnemyTargetCandidate b)
        {
            int Rank(EnemyTargetCandidate c) => priority == EnemyTargetPriority.VulnerablePrey
                ? c.Life == LifeState.Downed ? 0 : 1 : c.Life == LifeState.Alive ? 0 : 1;
            int result = Rank(a).CompareTo(Rank(b));
            if (result == 0 && priority == EnemyTargetPriority.VulnerablePrey)
                result = ((long)a.Health * b.MaximumHealth).CompareTo((long)b.Health * a.MaximumHealth);
            if (result == 0 && priority == EnemyTargetPriority.ClearShot) result = b.ClearShot.CompareTo(a.ClearShot);
            if (result == 0) result = source.Distance(a.Position).CompareTo(source.Distance(b.Position));
            return result != 0 ? result : a.Id.CompareTo(b.Id);
        }
    }
}

public sealed record EnemyInspection(long ActorId, string Tendency, long? TargetId, string? TargetName,
    bool Committed, bool Support, IReadOnlyList<WorldPosition> Cells, long? Impact);

public sealed partial class TacticalCombat
{
    /// <summary>Local detected party targets only. World pursuit/routines handle a missing local target.</summary>
    public long? SelectEnemyTarget(long enemyId, Func<WorldPosition, bool>? mistBlocks = null)
    {
        var enemy = Actor(enemyId);
        if (enemy is not { Life: LifeState.Alive, Tactics.Enemy: not null } || Friendly(enemy))
            throw new InvalidOperationException("Target selection requires a living native enemy.");
        if (!_world.IsHostile(enemy)) return null;
        var priority = EnemyCatalog.Default.Get(enemy.Tactics.DefinitionId).TargetPriority;
        var sight = _world.VisionFrom(enemy.Position);
        var ecology = _world.Ecology(enemy);
        bool InTerritory(Actor p) => ecology == null || enemy.Habitat is not { } home
            || p.Position.Distance(home) <= (enemy.ProvokedUntil > _world.State.Tick ? ecology.Leash : ecology.Notice);
        var candidates = _world.Party.Where(p => p.Life != LifeState.Dead && State.Participants.Contains(p.Id)
                && p.Position.Floor == enemy.Position.Floor && sight.Contains(p.Position) && InTerritory(p))
            .Select(p => new EnemyTargetCandidate(p.Id, p.Position, p.Health, p.MaxHealth, p.Life,
                priority == EnemyTargetPriority.ClearShot && HasUsableShot(enemy, p, mistBlocks)));
        return EnemyTargeting.Choose(priority, enemy.Position, candidates)?.Id;
    }

    private bool HasUsableShot(Actor enemy, Actor target, Func<WorldPosition, bool>? mistBlocks)
    {
        var intent = PlanEnemyTurn(enemy, target, (_, _) => null, mistBlocks);
        if (intent.Kind != EnemyIntentKind.Windup || intent.Support != null) return false;
        var aim = EnemyAim(target, enemy.Position.Floor);
        var cells = intent.Advance == null ? RouteMirrors(enemy, enemy.Position, intent.Cells, intent.Delivery, knownOnly: true).Cells : intent.Cells;
        foreach (var cell in cells)
        {
            if (!ProjectileOpen(cell))
            {
                if (intent.Delivery is AttackDelivery.Projectile or AttackDelivery.PiercingLine) return false;
                continue;
            }
            if (cell == aim) return true;
            if (intent.Delivery == AttackDelivery.Projectile && BodyAt(cell, enemy.Id) != null) return false;
        }
        return false;
    }

    /// <summary>Chooses a local party target without a camera dependency, then uses the original controller.</summary>
    public EnemyIntent CommitEnemyTurn(Func<WorldPosition, IReadOnlySet<WorldPosition>, Facing?> nextStep,
        Func<WorldPosition, bool>? mistBlocks = null)
    {
        var target = SelectEnemyTarget((Active ?? throw new InvalidOperationException("No actor is ready.")).Id, mistBlocks)
            ?? throw new InvalidOperationException("No detected local target; world pursuit or routines must supply the action.");
        return CommitEnemyTurn(target, nextStep, mistBlocks);
    }

    public EnemyInspection? InspectEnemy(long observerId, long enemyId, Func<WorldPosition, bool>? mistBlocks = null)
    {
        var observer = Actor(observerId); var enemy = Actor(enemyId);
        if (!Friendly(observer) || observer.Life == LifeState.Dead || enemy is not { Life: LifeState.Alive, Tactics.Enemy: not null }
            || Friendly(enemy)) return null;
        var sight = _world.VisionFrom(observer.Position);
        if (!sight.Contains(enemy.Position)) return null;
        var attack = State.Attacks.SingleOrDefault(a => a.Source == enemyId);
        long? targetId = attack == null ? SelectEnemyTarget(enemyId, mistBlocks)
            : attack.Support is { } support ? support.Target > 0 ? support.Target : null : attack.IntendedTarget > 0 ? attack.IntendedTarget : null;
        var target = targetId is { } id ? _world.State.Actors.FirstOrDefault(a => a.Id == id && sight.Contains(a.Position)) : null;
        return new(enemyId, EnemyTargeting.Description(EnemyCatalog.Default.Get(enemy.Tactics.DefinitionId).TargetPriority),
            target?.Id, target?.Name, attack != null, attack?.Support != null,
            attack?.Cells.Where(sight.Contains).ToArray() ?? [], attack?.Due);
    }
}
