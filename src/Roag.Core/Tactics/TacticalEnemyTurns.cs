using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    public void RegisterEnemy(Actor actor, string definitionId, long? readyAt = null,
        bool guaranteedHealthDrop = false, bool rewardExempt = false, EnemyPursuitBoundary? pursuitBoundary = null)
    {
        if (Friendly(actor)) throw new ArgumentException("A party member cannot use an enemy controller.", nameof(actor));
        var definition = EnemyCatalog.Default.Get(definitionId);
        if (_world.State.Dungeon != null && !rewardExempt)
            guaranteedHealthDrop |= definition.Ai is EnemyBrainKind.Warden or EnemyBrainKind.BiomeElite
                || definition.Ai is not (EnemyBrainKind.HollowRegent or EnemyBrainKind.Pursuer)
                    && RoagRandom.Derive(_world.DungeonSeed, "supply:" + actor.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)) % 100 < 5;
        var tactics = definition.CreateTactics(guaranteedHealthDrop, rewardExempt);
        if (pursuitBoundary != null)
        {
            pursuitBoundary.Validate(_world.Plan);
            if (EnemyCatalog.Default.Get(definitionId).Ai != EnemyBrainKind.Pursuer || !pursuitBoundary.Contains(actor.Position))
                throw new ArgumentException("Only an owning dungeon can confine its pursuer.", nameof(pursuitBoundary));
            tactics.Enemy!.PursuitBoundary = pursuitBoundary;
        }
        Register(actor, tactics, readyAt);
    }

    /// <summary>
    /// World AI supplies its species-selected target and navigation policy. The controller
    /// queries actual terrain without using the camera or mutating time, memory or RNG.
    /// </summary>
    public EnemyIntent PreviewEnemyTurn(long targetId,
        Func<WorldPosition, IReadOnlySet<WorldPosition>, Facing?> nextStep,
        Func<WorldPosition, bool>? mistBlocks = null)
    {
        ArgumentNullException.ThrowIfNull(nextStep);
        var actor = Active;
        if (_world.State.AwaitingContinuation || actor is not { Life: LifeState.Alive, Tactics.Enemy: not null } || Friendly(actor))
            throw new InvalidOperationException("A registered living enemy must be ready.");
        var target = Actor(targetId);
        if (!Friendly(target) || target.Life == LifeState.Dead || target.Position.Floor != actor.Position.Floor)
            throw new ArgumentException("Select a surviving party member on this floor; world navigation handles other floors.", nameof(targetId));
        return PlanEnemyTurn(actor, target, nextStep, mistBlocks);
    }

    private static WorldPosition EnemyAim(Actor target, int floor)
        => target.Tactics?.Resources.Decoy is { } decoy && decoy.Floor == floor ? decoy : target.Position;

    private EnemyIntent PlanEnemyTurn(Actor actor, Actor? target,
        Func<WorldPosition, IReadOnlySet<WorldPosition>, Facing?> nextStep, Func<WorldPosition, bool>? mistBlocks)
    {
        var aim = target == null ? actor.Position : EnemyAim(target, actor.Position.Floor);
        var intent = EnemyPlanning.Choose(EnemyCatalog.Default.Get(actor.Tactics!.DefinitionId), actor.Tactics.Enemy!,
            new(actor.Position, aim, p => _world.CanEnter(p, actor.Id), ProjectileOpen, mistBlocks ?? (p => _world.DungeonTerrainAt(p) == 'm'),
                goals => nextStep(actor.Position, goals), p => _world.Chunks.At(p) == Material.Barricade,
                _world.State.Actors.Where(a => a.Id != actor.Id && State.Participants.Contains(a.Id))
                    .Select(a => new EnemyNeighbor(a.Id, a.Position, a.Health, a.MaxHealth, !HostileTo(actor, a),
                        a.Tactics!.DefinitionId, a.Tactics.Enemy?.Squad)).ToArray(),
                new(actor.Id, actor.Position, actor.Health, actor.MaxHealth, true, actor.Tactics.DefinitionId, actor.Tactics.Enemy!.Squad),
                State.EnemySnares, p => _world.CanEnter(p, target?.Id ?? actor.Id),
                p => _world.Plan.Contains(p) && _world.Chunks.At(p) is Material.Rock or Material.Wall,
                ProtectedForcedLanding, State.Loot.Drops,
                State.Corpses.Select(c => new RevivalCandidate(c.Actor, Actor(c.Actor).Position, c.ClaimedBy)).ToArray(),
                _world.GroundOpen, State.Webs,
                p => _world.Plan.Contains(p) && _world.Chunks.At(p) is not (Material.Rock or Material.Wall), SiltGround, State.Mirrors));
        // Local ally support has physical recipients even when no party member is
        // detected. Other recipes still require their actual hostile target.
        return target != null || intent.Support?.Kind is EnemySupportKind.Heal or EnemySupportKind.Ward or EnemySupportKind.Revive
            ? intent : new(EnemyIntentKind.Wait);
    }

    public EnemyIntent CommitEnemyTurn(long targetId,
        Func<WorldPosition, IReadOnlySet<WorldPosition>, Facing?> nextStep,
        Func<WorldPosition, bool>? mistBlocks = null)
    {
        var intent = PreviewEnemyTurn(targetId, nextStep, mistBlocks);
        return CommitEnemyIntent(Active!, intent, targetId);
    }

    private EnemyIntent CommitEnemyIntent(Actor actor, EnemyIntent intent, long targetId)
    {
        var definition = EnemyCatalog.Default.Get(actor.Tactics!.DefinitionId);
        var memory = actor.Tactics.Enemy!;
        if (intent.ShieldFacing is { } shield)
        {
            memory.ShieldFacing = shield;
            memory.ShieldLowered = intent.Kind == EnemyIntentKind.Windup;
        }
        if (intent.Kind == EnemyIntentKind.Windup)
        {
            // Validate before changing the pattern counter or committing recovery.
            if (intent.Cells.IsDefault || intent.Cells.Any(p => !_world.Plan.Contains(p)))
                throw new InvalidOperationException("Enemy controller produced invalid world cells.");
            if (definition.Ai is EnemyBrainKind.Warden or EnemyBrainKind.HollowRegent or EnemyBrainKind.BiomeElite)
                memory.Attacks = checked(memory.Attacks + 1);
            if (intent.ClearRetaliation) memory.Retaliate = false;
            CommitWindup(intent.Ability, intent.Cells, intent.Delivery, DamageElement.Physical,
                checked(actor.Power + intent.DamageBonus + (SquadRallySource(actor) != null ? 1 : 0)), definition.Windup, definition.Recovery, targetId, intent.Support, intent.Advance, intent.Shove, intent.SentinelFacing, intent.EmberLanding);
        }
        else if (intent.Kind is EnemyIntentKind.Move or EnemyIntentKind.UseStairs)
        {
            var move = new TacticalIntent(intent.Kind == EnemyIntentKind.UseStairs ? TacticalAction.UseStairs : TacticalAction.Move, intent.Facing);
            var preview = Preview(move);
            if (!preview.Valid) throw new InvalidOperationException("Navigation selected a blocked enemy step: " + preview.Reason);
            memory.LastMovement = intent.Facing;
            var result = Commit(move);
            if (!result.Success) throw new InvalidOperationException(result.Message);
        }
        else
        {
            long action = State.NextAction++;
            ResolveAtomically(RootCause(action), () =>
            {
                // Original enemies use their movement cost when they cannot advance.
                ScheduleReady(actor, definition.MoveCost); State.ActiveActor = 0;
                Emit("Waited", actor.Id, actor.Id, action, actor.Position);
            });
        }
        if (intent.Alert) memory.Alerted = true;
        return intent;
    }
}
