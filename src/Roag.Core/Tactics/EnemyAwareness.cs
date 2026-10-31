using Roag.Core.World;

namespace Roag.Core.Tactics;

/// <summary>A remembered observation, never an alternate authority for a target's current position.</summary>
public sealed record EnemyAwareness(long Target, WorldPosition LastKnown, long ObservedAt);

/// <summary>Physical footprint and floors of a pursuer's owning dungeon; inclusive coordinates.</summary>
public sealed record EnemyPursuitBoundary(int Left, int Top, int Right, int Bottom, int FirstFloor, int LastFloor)
{
    public bool Contains(WorldPosition p) => p.X >= Left && p.X <= Right && p.Y >= Top && p.Y <= Bottom
        && p.Floor >= FirstFloor && p.Floor <= LastFloor;

    internal void Validate(WorldPlan plan)
    {
        if (Left > Right || Top > Bottom || FirstFloor > LastFloor
            || !plan.Contains(new(Left, Top, FirstFloor)) || !plan.Contains(new(Right, Bottom, LastFloor)))
            throw new InvalidDataException("Invalid pursuer dungeon boundary.");
    }
}

public sealed partial class TacticalCombat
{
    public const int EnemyMemorySeconds = 30;

    /// <summary>Timeline membership is global; engagement requires a nearby physical hostile.</summary>
    public bool IsLocallyEngaged(long actorId)
    {
        var actor = Actor(actorId);
        if (actor.Life == LifeState.Dead || !State.Participants.Contains(actorId)) return false;
        foreach (var other in _world.State.Actors)
        {
            if (other.Life == LifeState.Dead || !State.Participants.Contains(other.Id)
                || Friendly(actor) == Friendly(other) || actor.Position.Floor != other.Position.Floor) continue;
            var enemy = Friendly(actor) ? other : actor;
            var member = Friendly(actor) ? actor : other;
            if (!_world.IsHostile(enemy)) continue;
            int dx = member.Position.X - enemy.Position.X, dy = member.Position.Y - enemy.Position.Y;
            if (dx * dx + dy * dy > 121) continue;
            if (_world.VisionFrom(enemy.Position).Contains(member.Position)
                || _world.VisionFrom(member.Position).Contains(enemy.Position)) return true;
        }
        return false;
    }

    private bool PursuitAllowed(Actor enemy, WorldPosition p)
    {
        if (!_world.Plan.Contains(p)) return false;
        var memory = enemy.Tactics!.Enemy!;
        if (EnemyCatalog.Default.Get(enemy.Tactics.DefinitionId).Ai == EnemyBrainKind.Pursuer
            && (memory.PursuitBoundary is not { } boundary || !boundary.Contains(p))) return false;
        return _world.Ecology(enemy) is not { } ecology || enemy.Habitat is not { } home
            || p.Distance(home) <= (enemy.ProvokedUntil > _world.State.Tick ? ecology.Leash : ecology.Notice);
    }

    private EnemyAwareness? ObservedEnemyTarget(Actor enemy, Func<WorldPosition, bool>? mistBlocks = null)
        => SelectEnemyTarget(enemy.Id, mistBlocks) is { } target && PursuitAllowed(enemy, Actor(target).Position)
            ? new(target, Actor(target).Position, Clock) : null;

    private EnemyAwareness? RememberedEnemyTarget(Actor enemy)
    {
        var memory = enemy.Tactics!.Enemy!.Awareness;
        if (memory == null || !_world.IsHostile(enemy) || !PursuitAllowed(enemy, memory.LastKnown)
            || Clock - memory.ObservedAt >= (long)EnemyMemorySeconds * Tuning.UnitsPerSecond
            || memory.LastKnown == enemy.Position) return null;
        // Death/party membership invalidate an identity, but hidden current positions
        // never update an observation or turn a search into a tracking attack.
        var target = _world.State.Actors.FirstOrDefault(a => a.Id == memory.Target);
        return target is { Life: not LifeState.Dead } && Friendly(target) ? memory : null;
    }

    /// <summary>Pure world decision: local controller when detected; physical search of the last observation otherwise.</summary>
    public EnemyIntent PreviewWorldEnemyTurn()
    {
        var enemy = Active;
        if (_world.State.AwaitingContinuation || enemy is not { Life: LifeState.Alive, Tactics.Enemy: not null } || Friendly(enemy))
            throw new InvalidOperationException("A living native enemy must be ready.");
        var observed = ObservedEnemyTarget(enemy);
        var awareness = observed ?? RememberedEnemyTarget(enemy);
        if (awareness == null) return new(EnemyIntentKind.Wait);
        WorldNavigationStep? planned = null;
        Facing? Step(WorldPosition _, IReadOnlySet<WorldPosition> goals)
        {
            planned = _world.Navigate(enemy, goals, p => PursuitAllowed(enemy, p));
            return planned?.Facing;
        }
        if (observed != null)
        {
            var intent = PlanEnemyTurn(enemy, Actor(observed.Target), Step, null);
            return intent.Kind == EnemyIntentKind.Move && planned?.Action == TacticalAction.UseStairs
                ? new(EnemyIntentKind.UseStairs) : intent;
        }
        var searchGoals = new HashSet<WorldPosition> { awareness.LastKnown };
        if (!_world.CanEnter(awareness.LastKnown, enemy.Id))
            foreach (var facing in new[] { Facing.North, Facing.East, Facing.South, Facing.West })
            {
                var (dx, dy) = AttackGeometry.Offset(facing);
                var adjacent = awareness.LastKnown.Offset(dx, dy);
                if (_world.CanEnter(adjacent, enemy.Id)) searchGoals.Add(adjacent);
            }
        planned = _world.Navigate(enemy, searchGoals, p => PursuitAllowed(enemy, p));
        return planned == null ? new(EnemyIntentKind.Wait)
            : new(planned.Action == TacticalAction.UseStairs ? EnemyIntentKind.UseStairs : EnemyIntentKind.Move, planned.Facing);
    }

    public EnemyIntent CommitWorldEnemyTurn()
    {
        var intent = PreviewWorldEnemyTurn();
        var enemy = Active!;
        var awareness = ObservedEnemyTarget(enemy) ?? RememberedEnemyTarget(enemy);
        enemy.Tactics!.Enemy!.Awareness = awareness;
        return CommitEnemyIntent(enemy, intent, awareness?.Target ?? 0);
    }

    /// <summary>Runs scheduled hostile decisions until a living party member needs input. No real-time pumping.</summary>
    public Actor? AdvanceToPartyDecision()
    {
        while (AdvanceToDecision() is { } actor)
        {
            if (Friendly(actor))
            {
                if (!ShouldEatAutomatically(actor)) return actor;
                var result = Commit(new(TacticalAction.Eat));
                if (!result.Success) throw new InvalidOperationException(result.Message);
                continue;
            }
            CommitWorldEnemyTurn();
        }
        return null;
    }

    private void ObserveEnemyAwareness()
    {
        foreach (var enemy in _world.State.Actors.Where(a => a is { Life: LifeState.Alive, Tactics.Enemy: not null } && !Friendly(a)))
            enemy.Tactics!.Enemy!.Awareness = ObservedEnemyTarget(enemy) ?? RememberedEnemyTarget(enemy);
    }

    private void ObserveStairDeparture(Actor member, WorldPosition from, WorldPosition landing)
    {
        if (!Friendly(member)) return;
        foreach (var enemy in _world.State.Actors.Where(a => a is { Life: LifeState.Alive, Tactics.Enemy: not null }
                     && !Friendly(a) && _world.IsHostile(a) && PursuitAllowed(a, landing)))
        {
            var memory = enemy.Tactics!.Enemy!;
            if (memory.Awareness?.Target == member.Id && _world.VisionFrom(enemy.Position).Contains(from))
                memory.Awareness = new(member.Id, landing, Clock);
        }
    }
}
