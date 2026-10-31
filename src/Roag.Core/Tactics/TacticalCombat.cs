using System.Collections.Immutable;
using Roag.Core.Expeditions;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record TacticalIntent(TacticalAction Action, Facing Facing = Facing.South, long Target = 0, bool Vow = false, int Choice = 0, bool ReplaceUtility = false);
public sealed record TacticalPreview(bool Valid, string Reason, long Recovery, IReadOnlyList<WorldPosition> Cells,
    bool Bump = false, bool Swap = false, int? ReturnDistance = null)
{
    public int? Damage { get; init; }
    public long? FatePower { get; init; }
    public BoonVowPayment? Vow { get; init; }
    public BoonWeaponForm? WeaponForm { get; init; }
    public MirrorReflection? Reflection { get; init; }
    public WorldPosition? StairLanding { get; init; }
    public IReadOnlyList<BoonPreviewCell> BoonCells { get; init; } = [];
}

/// <summary>
/// Native action execution against the real world actors and chunk terrain. World AI and
/// sustained activities supply decisions at readiness; this engine never invents an AI fallback.
/// The production adapter is switched only after its remaining class/effect handlers are ported.
/// </summary>
public sealed partial class TacticalCombat
{
    private readonly WorldSession _world;
    private readonly Action<long> _advanceWorld;
    public TacticalWorldState State { get; }
    public TacticalTuning Tuning { get; }
    public long Clock => State.Queue.Clock;
    public Actor? Active => State.ActiveActor == 0 ? null : Actor(State.ActiveActor);

    public TacticalCombat(WorldSession world, Action<long> advanceWorld, TacticalTuning? tuning = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(advanceWorld);
        _world = world; _advanceWorld = advanceWorld;
        tuning?.Validate();
        State = world.State.Tactics ??= new()
        {
            RandomState = (int)(WorldRandom.Hash(world.Seed, 0, salt: 9306) % 2147483646) + 1,
            Tuning = tuning ?? new(),
            Timeline = new(checked(world.State.Tick * (tuning ?? new()).UnitsPerWorldTick), 1, [])
        };
        Tuning = State.Tuning; Tuning.Validate();
        if (tuning is not null && tuning != Tuning) throw new ArgumentException("Saved tactical tuning differs from the requested tuning.", nameof(tuning));
    }

    private Actor Actor(long id) => _world.State.Actors.FirstOrDefault(a => a.Id == id)
        ?? throw new InvalidDataException("Tactical actor is missing from the world.");
    private bool Friendly(Actor actor) => _world.State.Party.Contains(actor.Id);
    private int EffectiveSpeed(Actor actor) => actor.Tactics?.Enemy != null ? actor.Speed
        : checked(actor.Speed + (actor.Tactics?.Boons.SpeedBonus(Clock) ?? 0));
    private bool ProjectileOpen(WorldPosition p) => _world.Plan.Contains(p)
        && (_world.State.Dungeon == null || _world.State.Dungeon.Contains(p))
        && !Materials.Opaque(_world.Chunks.At(p)) && _world.ObjectAt(p) is not { Solid: true };

    public void Register(Actor actor, ActorTactics tactics, long? readyAt = null)
    {
        if (!ReferenceEquals(Actor(actor.Id), actor)) throw new ArgumentException("Use the persistent world actor.", nameof(actor));
        tactics.Validate();
        if (Friendly(actor) && tactics.Build is null) throw new ArgumentException("Party members require their own class build.", nameof(tactics));
        long due = readyAt ?? Clock;
        if (due < Clock) throw new ArgumentOutOfRangeException(nameof(readyAt));
        if (State.Participants.Contains(actor.Id)) throw new InvalidOperationException("Actor is already registered.");
        if (actor.Life != LifeState.Alive || actor.Health <= 0) throw new InvalidOperationException("Register a living actor.");
        int oldMaximum = actor.MaxHealth;
        actor.Tactics = tactics;
        actor.Health = Math.Clamp((int)Math.Ceiling((double)actor.Health * actor.MaxHealth / oldMaximum), 1, actor.MaxHealth);
        State.Participants.Add(actor.Id);
        State.Queue.Schedule(ScheduledKind.ActorReady, due, actor.Id, Friendly(actor));
        if (Friendly(actor)) EnsureSurvivalScheduled();
    }

    public Actor? AdvanceToDecision()
    {
        if (_world.State.AwaitingContinuation) return null;
        ActivateDungeonElites();
        if (_world.Party.Any(a => a.Life != LifeState.Dead && !State.Participants.Contains(a.Id)))
            throw new InvalidOperationException("Register every surviving party member before advancing the timeline.");
        if (Active is { Life: LifeState.Alive } current) return current;
        State.ActiveActor = 0;
        while (State.Queue.TakeNext() is { } work)
        {
            _advanceWorld(Clock);
            PruneWebs();
            PruneSilt();
            PruneEmbers();
            PruneMirrors();
            PruneOaths();
            if (work.Kind == ScheduledKind.Survival)
            {
                ResolveSurvival();
                if (_world.State.AwaitingContinuation) return null;
                continue;
            }
            if (work.Kind == ScheduledKind.Impact)
            {
                var attack = State.Attacks.Single(a => a.Id == work.ReferenceId);
                State.Attacks.Remove(attack);
                // A killed source's explicitly committed attack is cancelled on death.
                ResolveAttack(attack);
                if (_world.State.AwaitingContinuation) return null;
                continue;
            }
            if (work.Kind == ScheduledKind.EmberPulse)
            {
                ResolveEmberPulse(work);
                if (_world.State.AwaitingContinuation) return null;
                continue;
            }
            if (work.Kind == ScheduledKind.TerrainCycle)
            {
                ResolveTerrainCycle(work);
                if (_world.State.AwaitingContinuation) return null;
                continue;
            }
            if (work.Kind == ScheduledKind.Effect)
            {
                if (!ResolveBoonWork(work))
                    ResolveAtomically(RootCause(work.ReferenceId), () => ResolveClassEffect(work));
                if (_world.State.AwaitingContinuation) return null;
                continue;
            }
            if (work.Kind != ScheduledKind.ActorReady)
                throw new InvalidDataException("No handler for this scheduled tactical work.");
            var actor = Actor(work.ActorId);
            if (actor.Life == LifeState.Dead) continue;
            if (!Friendly(actor) && (actor.Tactics!.EnemySnareDelay > 0 || actor.Tactics.WebRecoveryDelay > 0))
            {
                int delay = checked(actor.Tactics.EnemySnareDelay + actor.Tactics.WebRecoveryDelay);
                actor.Tactics.EnemySnareDelay = 0; actor.Tactics.WebRecoveryDelay = 0;
                State.Queue.Schedule(ScheduledKind.ActorReady, checked(Clock + delay), actor.Id);
                continue;
            }
            if (actor.Life == LifeState.Downed)
            {
                ResolveAtomically(RootCause(0), () =>
                {
                    var tactics = actor.Tactics!;
                    tactics.BleedTurns--;
                    Emit("BleedOut", actor.Id, actor.Id, 0, actor.Position, tactics.BleedTurns);
                    if (tactics.BleedTurns == 0) Die(actor, 0, 0);
                    else ScheduleReady(actor, Tuning.WaitCost);
                });
                if (_world.State.AwaitingContinuation) return null;
                continue;
            }
            actor.Tactics!.Guarding = false;
            actor.Tactics.Resources.Parry = null;
            if (actor.Tactics.SnaredBy != 0)
            {
                long source = actor.Tactics.SnaredBy;
                actor.Tactics.SnaredBy = 0;
                Emit("SnareHeld", source, actor.Id, 0, actor.Position);
                RecordCombatControl(source, actor, ActionTiming.Recovery(actor.Tactics.MoveCost, EffectiveSpeed(actor)));
                ScheduleReady(actor, actor.Tactics.MoveCost);
                continue;
            }
            State.ActiveActor = actor.Id;
            if (Friendly(actor))
            {
                State.ControlledActor = actor.Id;
                _world.State.Position = actor.Position;
                _world.Reveal();
                if (Clock > 0) StartOptionalTerrain(0);
            }
            ObserveEnemyAwareness();
            return actor;
        }
        return null;
    }

    private TacticalPreview PreviewAction(TacticalIntent intent)
    {
        if (_world.State.AwaitingContinuation) return Invalid("This campaign has ended.");
        var actor = Active;
        if (actor is not { Life: LifeState.Alive, Tactics: { } tactics }) return Invalid("Wait for an actor's turn.");
        if (!Enum.IsDefined(intent.Action) || !Enum.IsDefined(intent.Facing)) return Invalid("Choose a valid action and direction.");
        var definition = tactics.Build is { } build ? ClassCatalog.Get(build.Class) : null;
        int cost;
        IReadOnlyList<WorldPosition> cells = [];
        switch (intent.Action)
        {
            case TacticalAction.Move:
            case TacticalAction.Dash:
                return PreviewMovement(actor, definition, intent);
            case TacticalAction.UseStairs:
                return PreviewStairs(actor, definition);
            case TacticalAction.Eat:
                return PreviewEating(actor);
            case TacticalAction.ClaimChest:
                return PreviewChest(actor, intent);
            case TacticalAction.DungeonRoute:
                return PreviewDungeonRoute(actor, intent);
            case TacticalAction.UseUtility:
                return PreviewUtility(actor, intent);
            case TacticalAction.Interact:
                return PreviewService(actor, intent);
            case TacticalAction.Wait: cost = Tuning.WaitCost; break;
            case TacticalAction.Guard: cost = Tuning.GuardCost; break;
            case TacticalAction.Revive:
                var target = _world.State.Actors.FirstOrDefault(a => a.Id == intent.Target);
                if (target is not { Life: LifeState.Downed, Tactics: not null } || !Friendly(target)
                    || !Friendly(actor) || actor.Position.Distance(target.Position) != 1
                    || _world.State.Inventory.GetValueOrDefault("medicine") < 1)
                    return Invalid("Stand beside a downed companion with one medicine.");
                cost = Tuning.ReviveCost; cells = [target.Position]; break;
            case TacticalAction.BasicAttack:
            case TacticalAction.Special:
                if (definition is null) return Invalid("Use an authored enemy intent.");
                var footprint = ClassFootprint(actor, intent);
                if (intent.Action == TacticalAction.Special && PreviewSpecial(actor, intent, footprint) is { } special)
                    return special;
                cells = footprint.Cells; cost = definition.Costs.For(intent.Action); break;
            default: return Invalid("That action handler is not available.");
        }
        return new(true, "", ActionTiming.Recovery(cost, EffectiveSpeed(actor)), cells);
    }

    public ActionResult Commit(TacticalIntent intent)
    {
        var preview = Preview(intent);
        if (!preview.Valid) return new(false, preview.Reason);
        var actor = Active!;
        ObserveEnemyAwareness();
        long action = State.NextAction++;
        actor.Tactics!.Facing = intent.Facing;
        var executed = preview.Bump ? intent with { Action = TacticalAction.BasicAttack } : intent;
        _lastWasDash = executed.Action == TacticalAction.Dash;
        ResolveAtomically(RootCause(action), () =>
        {
            // Recovery is committed before reactive effects, so downing/stun cannot erase its cost.
            State.Queue.Schedule(ScheduledKind.ActorReady, checked(Clock + preview.Recovery), actor.Id, Friendly(actor));
            actor.Tactics.EnemySnareDelay = 0;
            actor.Tactics.WebRecoveryDelay = 0;
            State.ActiveActor = 0;
            if (preview.Vow is { } vow)
            {
                actor.Health -= vow.HealthCost;
                Emit("VowPaid", actor.Id, actor.Id, action, actor.Position, vow.HealthCost, ability: "core:boon/pilgrims_vow");
            }
            switch (executed.Action)
            {
                case TacticalAction.Move:
                case TacticalAction.Dash:
                    CommitMovement(actor, executed, preview, action); break;
                case TacticalAction.UseStairs:
                    CommitStairs(actor, preview.StairLanding!.Value, action); break;
                case TacticalAction.Eat:
                    Eat(actor, action); break;
                case TacticalAction.ClaimChest:
                    ClaimChest(actor, intent, action); break;
                case TacticalAction.UseUtility:
                    CommitUtility(actor, preview, action); break;
                case TacticalAction.Interact:
                    CommitService(actor, intent, action); break;
                case TacticalAction.DungeonRoute:
                    _world.ConnectDungeonRoute(this, intent.Choice);
                    Emit("RouteOpened", actor.Id, actor.Id, action, actor.Position); break;
                case TacticalAction.Guard:
                    actor.Tactics.Guarding = true;
                    Emit("GuardRaised", actor.Id, actor.Id, action, actor.Position); break;
                case TacticalAction.Revive:
                    var target = Actor(intent.Target);
                    _world.State.Inventory["medicine"]--;
                    target.Life = LifeState.Alive; target.Health = Math.Max(1, target.MaxHealth / 4);
                    target.Tactics!.BleedTurns = 0;
                    RecordCombatHealing(actor.Id, target.Id, target.Health);
                    State.Queue.CancelActor(target.Id, ScheduledKind.ActorReady);
                    ScheduleReady(target, Tuning.WaitCost);
                    Emit("Revived", actor.Id, target.Id, action, target.Position, target.Health); break;
                case TacticalAction.BasicAttack:
                case TacticalAction.Special:
                    CommitClassAttack(actor, executed, preview, action);
                    break;
                case TacticalAction.Wait: Emit("Waited", actor.Id, actor.Id, action, actor.Position); break;
            }
        });
        if (Friendly(actor))
        {
            var supplies = actor.Tactics.Supplies;
            supplies.FocusActions = Math.Max(0, supplies.FocusActions - 1);
            supplies.WardActions = Math.Max(0, supplies.WardActions - 1);
        }
        if (State.ControlledActor == actor.Id && _world.State.Position != actor.Position)
        {
            _world.State.Position = actor.Position;
            _world.Reveal();
        }
        ObserveEnemyAwareness();
        return new(true, "Action committed.");
    }

    /// <summary>AI/class handlers commit explicit locked terrain cells, never a tracking target.</summary>
    public LockedAttack CommitWindup(string ability, IReadOnlyList<WorldPosition> cells, AttackDelivery delivery,
        DamageElement element, int damage, int windupCost, int recoveryCost, long intendedTarget = 0,
        EnemySupport? support = null, EnemyAdvance? advance = null, EnemyShove? shove = null, Facing? sentinelFacing = null, WorldPosition? emberLanding = null)
    {
        if (_world.State.AwaitingContinuation) throw new InvalidOperationException("This campaign has ended.");
        var actor = Active ?? throw new InvalidOperationException("No actor is ready.");
        if (intendedTarget != 0) _ = Actor(intendedTarget);
        if (actor.Life != LifeState.Alive || damage < 0 || string.IsNullOrWhiteSpace(ability)
            || cells.Count == 0 && (actor.Tactics?.Enemy == null || delivery != AttackDelivery.Area) || cells.Distinct().Count() != cells.Count
            || cells.Any(p => p.Floor != actor.Position.Floor || !_world.Plan.Contains(p))
            || !Enum.IsDefined(delivery) || !Enum.IsDefined(element)) throw new ArgumentException("Invalid locked attack.");
        long due = checked(Clock + ActionTiming.Recovery(windupCost, EffectiveSpeed(actor)));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recoveryCost);
        EnemyAttackRules.Validate(support, advance, shove);
        if (!EnemyAttackRules.SpecialIdentityValid(actor.Tactics!.DefinitionId, ability, actor.Position, cells, support, shove))
            throw new ArgumentException("Special enemy attack identity or geometry is inconsistent.");
        if (!EnemyEmbers.ValidAttack(actor.Tactics, ability, actor.Position, cells, delivery, emberLanding,
            support, advance, shove, sentinelFacing, committing: true))
            throw new ArgumentException("Invalid moth commitment.");
        if (!EnemySilt.ValidAttack(actor.Tactics, ability, actor.Position, cells, delivery, advance, committing: true))
            throw new ArgumentException("Invalid silt commitment.");
        if (!EnemySentinel.ValidAttack(actor.Tactics, ability, actor.Position, cells, sentinelFacing, delivery,
            support, advance, shove, committing: true))
            throw new ArgumentException("Invalid sentinel commitment.");
        if (shove != null && !cells.Contains(shove.Cell)) throw new ArgumentException("Shove must reference a locked attack cell.");
        if (support is { Target: > 0 }) _ = Actor(support.Target);
        if (support is { Drop: > 0 } && support.Drop >= State.Loot.NextDrop)
            throw new ArgumentException("Gulp references unallocated physical loot.");
        if (support?.Kind == EnemySupportKind.Revive) ValidateRevivalCommit(actor, support.Corpse, cells[0]);
        if (support?.Kind == EnemySupportKind.Alarm && actor.Tactics?.Enemy is not { AlarmSpent: false })
            throw new ArgumentException("Alarm attempt is already spent.");
        if (support?.Kind == EnemySupportKind.Mirror && actor.Tactics?.Enemy is not { MirrorCasts: < 2 })
            throw new ArgumentException("Mirror attempts are already spent.");
        if (support?.Kind == EnemySupportKind.Web && actor.Tactics?.Enemy is not { WebCasts: < 2 })
            throw new ArgumentException("Web attempts are already spent.");
        if (support?.Kind == EnemySupportKind.Ward && actor.Tactics?.DefinitionId != "core:enemy/ward_weaver"
            || support?.Kind == EnemySupportKind.Snare && actor.Tactics?.DefinitionId != "core:enemy/snare_scribe"
            || support?.Kind == EnemySupportKind.Phase && (actor.Tactics?.DefinitionId != "core:enemy/hollow_wraith" || !EnemyAttackRules.PhaseGeometry(actor.Position, cells)))
            throw new ArgumentException("Support effect does not match the enemy role.");
        if ((support != null || advance != null || shove != null) && actor.Tactics?.Enemy == null)
            throw new ArgumentException("Enemy attack effects require a native enemy.");
        var routed = support == null && advance == null
            ? RouteMirrors(actor, actor.Position, cells, delivery) : new ReflectedPath(cells);
        cells = routed.Cells;
        bool deferredRecovery = actor.Tactics?.Enemy != null;
        long? ready = deferredRecovery ? null : checked(due + ActionTiming.Recovery(recoveryCost, EffectiveSpeed(actor)));
        long action = State.NextAction++;
        var cause = RootCause(action, coefficient: AbilityCoefficient(ability));
        var attack = new LockedAttack(action, actor.Id, actor.Position, cells.ToArray(), ability, delivery, element, damage, due,
            Causality: cause, IntendedTarget: intendedTarget, RecoveryCost: deferredRecovery ? recoveryCost : null,
            Support: support, Advance: advance, Shove: shove, SentinelFacing: sentinelFacing, EmberLanding: emberLanding, Reflection: routed.Reflection);
        ResolveAtomically(cause, () =>
        {
            ConsumeMirror(actor, action, routed.Reflection, cells);
            if (support?.Kind == EnemySupportKind.Mirror) actor.Tactics!.Enemy!.MirrorCasts++;
            if (emberLanding != null) actor.Tactics!.Enemy!.EmberDives++;
            if (ability == EnemySilt.Charge) actor.Tactics!.Enemy!.SiltCharges++;
            if (sentinelFacing is { } facing) EnemySentinel.Commit(actor.Tactics!.Enemy!, facing);
            if (support?.Kind == EnemySupportKind.Revive) ClaimRevival(actor, support.Corpse);
            if (support?.Kind == EnemySupportKind.Alarm) actor.Tactics!.Enemy!.AlarmSpent = true;
            if (support?.Kind == EnemySupportKind.Web) actor.Tactics!.Enemy!.WebCasts++;
            foreach (var owner in _world.State.Actors)
                if (owner.Tactics?.Resources.Decoy is { } decoy && cells.Contains(decoy) && HostileTo(owner, actor))
                {
                    owner.Tactics.Resources.Decoy = null;
                    Emit("DecoyTriggered", actor.Id, owner.Id, action, decoy);
                }
            State.Attacks.Add(attack);
            if (support?.Kind == EnemySupportKind.Revive) PruneRevivalCorpses();
            State.Queue.Schedule(ScheduledKind.Impact, due, actor.Id, referenceId: action);
            if (ready is { } committedReady) State.Queue.Schedule(ScheduledKind.ActorReady, committedReady, actor.Id, Friendly(actor));
            Emit("WindupStarted", actor.Id, 0, action, actor.Position, ability: ability,
                snapshot: CaptureBoonEvent(BoonTrigger.WindupStarted, actor.Id, 0, action, actor.Position, ability)
                    with { TargetCells = cells.ToImmutableArray(), WindupId = action });
            State.ActiveActor = 0;
        });
        return attack with { Cells = attack.Cells.ToArray() };
    }

    public IReadOnlyList<LockedAttack> VisibleAttacks(IReadOnlySet<WorldPosition> vision)
        => State.Attacks.Concat(SentryAttacks()).Concat(EmberWarnings()).Concat(TerrainWarnings()).Where(a => vision.Contains(a.Source == 0 || a.ClassGenerated || a.Ability == EnemyEmbers.Pulse ? a.Origin : Actor(a.Source).Position))
            .Select(a => a with { Cells = a.Cells.Where(vision.Contains).ToArray() }).Where(a => a.Cells.Length > 0).ToArray();

    public IReadOnlyList<TacticalTimelineEntry> VisibleTimeline()
    {
        bool Visible(long id) => _world.State.Party.Contains(id) || _world.Visible.Contains(Actor(id).Position);
        var actual = State.Queue.Snapshot().Pending.Where(e => e.Kind != ScheduledKind.Survival).Where(e => e.Kind == ScheduledKind.EmberPulse
            ? State.Embers.Any(f => f.Schedule == e.Sequence && _world.Visible.Contains(f.Position)) : e.Kind == ScheduledKind.TerrainCycle
                ? State.OptionalTerrain.Nodes.Any(n => n.Schedule == e.Sequence && n.Phase == OptionalTerrainPhase.Warning && _world.Visible.Contains(n.Position)) : Visible(e.ActorId))
            .Select(e => new TacticalTimelineEntry(e.Kind, e.ActorId, e.Due, e.ReferenceId, false, e.Sequence));
        var projected = State.Attacks.Where(a => a.RecoveryCost != null && Visible(a.Source))
            .Select(a => new TacticalTimelineEntry(ScheduledKind.ActorReady, a.Source,
                checked(a.Due + ActionTiming.Recovery(a.RecoveryCost!.Value, Actor(a.Source).Speed)), a.Id, true, null));
        return actual.Concat(projected).OrderBy(e => e.Due).ThenBy(e => e.Kind == ScheduledKind.ActorReady ? 1 : 0)
            .ThenBy(e => e.Kind == ScheduledKind.ActorReady && _world.State.Party.Contains(e.ActorId) ? 0 : 1)
            .ThenBy(e => e.Sequence ?? long.MaxValue).ThenBy(e => e.ActorId).ToArray();
    }

    internal void ResolveAttack(LockedAttack attack)
    {
        var cause = attack.Causality ?? _cause ?? RootCause(attack.Id);
        cause = cause with { ClassGenerated = attack.ClassGenerated,
            ProcCoefficient = attack.Cause == DamageCause.DirectAttack ? AbilityCoefficient(attack.Ability) : cause.ProcCoefficient };
        ResolveAtomically(cause, () =>
        {
            if (attack.Support != null) ResolveEnemySupport(attack);
            else
            {
                if (attack.RecoveryCost != null || attack.ClassGenerated && attack.Ability == "core:ability/tinkerer_sentry")
                    Emit("AttackPerformed", attack.Source, 0, attack.Id, attack.Origin, ability: attack.Ability,
                        snapshot: CaptureBoonEvent(BoonTrigger.AttackPerformed, attack.Source, 0, attack.Id, attack.Origin, attack.Ability)
                            with { TargetCells = attack.Cells.ToImmutableArray(), DamageType = attack.Element, DirectAttack = !attack.ClassGenerated,
                                WindupId = attack.RecoveryCost != null ? attack.Id : null },
                        webCuts: CaptureWebCuts(attack.Source, attack.Cells, attack.Delivery, attack.Damage));
                ResolveDamageBatch(attack);
            }
            if (attack.EmberLanding != null && !_world.State.AwaitingContinuation) ResolveMothLanding(Actor(attack.Source), attack);
            if (attack.Advance != null && !_world.State.AwaitingContinuation) ResolveEnemyAdvance(attack);
            if (attack.Shove is { } shove && !_world.State.AwaitingContinuation
                && _world.OccupantAt(shove.Cell, attack.Source) is { Life: LifeState.Alive, Health: > 0, Tactics: not null } victim)
                TryShove(victim, shove.Facing, attack.Source, attack.Id);
            // Original enemy recovery is committed at impact, before reactive boon work.
            // Death/stun reactions can then cancel or extend the committed readiness.
            if (attack.RecoveryCost is { } cost && Actor(attack.Source) is { Life: LifeState.Alive } actor)
                ScheduleReady(actor, cost);
        });
    }

    private sealed record DamageOutcome(Actor Target, int Damage, int Prevented, BoonEvent Snapshot, bool Invulnerable, EnemyProtection? Protection);

    private void ResolveDamageBatch(LockedAttack attack)
    {
        var source = attack.Source == 0 ? null : Actor(attack.Source);
        if (source != null && !attack.PersistsWithoutOwner && source.Life != LifeState.Alive) return;
        var hits = new List<DamageOutcome>();
        var hitIds = new HashSet<long>();
        bool direct = attack.Cause == DamageCause.DirectAttack && !attack.ClassGenerated;
        bool pattern = direct && source?.Tactics?.Boons.Inventory.Stacks.Keys.Any(id => BoonCatalog.Default.Get(id).PatternPreview != null) == true;
        // Capture every victim before committing any health change or draining a reactive event.
        foreach (var cell in attack.Cells)
        {
            if (!attack.IgnoresTerrain && !ProjectileOpen(cell))
            {
                if (attack.Delivery is AttackDelivery.Projectile or AttackDelivery.PiercingLine) break;
                continue;
            }
            var target = _world.State.Actors.FirstOrDefault(a => (a.Id != attack.Source || attack.HitsOwner) && a.Position == cell && a.Life != LifeState.Dead);
            if (target is null || !hitIds.Add(target.Id)) continue;
            bool skull = attack.FateFace == 7 && RegularBoonTarget(target);
            int attempted = skull ? Math.Max(attack.Damage, checked(target.Health + target.Armor)) : attack.Damage;
            if (direct && source?.Tactics?.Boons is { ReversalTarget: { } reversed } boons && reversed == target.Id)
            {
                int bonus = checked((int)boons.Inventory.Magnitude("core:boon/ring_of_reversal").Power);
                attempted = checked(attempted + bonus); boons.ReversalTarget = null;
                Emit("RingReversalSpent", source.Id, target.Id, attack.Id, target.Position, bonus);
            }
            bool invulnerable = target.Tactics?.Invulnerable == true;
            var protection = SquadProtection(target, source, direct);
            int afterDefense = invulnerable || protection != null ? 0 : Math.Max(0, attempted - target.Armor);
            int prevented = ClassProtection(target, source, attack, afterDefense);
            if (Guards(target, attack)) prevented += (int)((long)(afterDefense - prevented) * Tuning.GuardReductionPercent / 100);
            if (target.Tactics?.Supplies is { WardActions: > 0 } supply && afterDefense > prevented)
            {
                supply.WardActions = 0;
                Emit("SupplyWardAbsorbed", target.Id, target.Id, attack.Id, target.Position, afterDefense - prevented);
                prevented = afterDefense;
            }
            if (target.Tactics is { } targetTactics)
                prevented += targetTactics.Boons.ConsumeWards(afterDefense - prevented, Clock).Sum(ward => ward.Amount);
            int damage = afterDefense - prevented;
            var neighbors = _world.State.Actors.Where(a => a.Id != target.Id && a.Health > 0 && a.Position.Floor == cell.Floor
                && Math.Max(Math.Abs(a.Position.X - cell.X), Math.Abs(a.Position.Y - cell.Y)) == 1).ToArray();
            bool backed = direct && source?.Tactics?.Boons.Inventory.Stacks.ContainsKey("core:boon/stone_backstop") == true
                && BoonGeometry.Axis(cell, source.Position, BoonAxis.Cardinal)
                && !ProjectileOpen(cell.Offset(Math.Sign(cell.X - source.Position.X), Math.Sign(cell.Y - source.Position.Y)));
            var snapshot = CaptureBoonEvent(BoonTrigger.HitConfirmed, attack.Source, target.Id, attack.Id, cell, attack.Ability) with
            {
                DirectAttack = direct, DamageType = attack.Element, TargetCells = attack.Cells.ToImmutableArray(),
                FateFace = attack.FateFace,
                ActualLoss = Math.Min(target.Health, damage), Amount = attempted, TargetBurning = target.Tactics?.Burning != null,
                PatternOrigin = pattern ? attack.Origin : null, WallBacked = backed,
                AdjacentHostileCount = neighbors.Count(a => source == null || BoonFaction(a) != BoonFaction(source)),
                AdjacentBurningCount = neighbors.Count(a => BoonFaction(a) == BoonFaction(target) && a.Tactics?.Burning != null)
            };
            hits.Add(new(target, damage, prevented, snapshot, invulnerable, protection));
            if (attack.Delivery == AttackDelivery.Projectile) break;
        }
        RedistributeOathDamage(hits, attack);
        foreach (var hit in hits) hit.Target.Health -= hit.Snapshot.ActualLoss!.Value;
        if (source != null)
            foreach (var hit in hits)
                RecordCombatDamage(source, hit.Target, hit.Snapshot.ActualLoss!.Value, hit.Prevented, hit.Snapshot.HpBefore!.Value);
        foreach (var hit in hits)
        {
            var target = hit.Target;
            Emit(hit.Damage > 0 ? "DamageTaken" : "DamageBlocked", attack.Source, target.Id, attack.Id,
                target.Position, hit.Snapshot.ActualLoss!.Value, hit.Prevented, hit.Snapshot.AbilityId ?? attack.Ability, snapshot: hit.Snapshot, protection: hit.Protection);
            if (hit.Snapshot.ActualLoss > 0 && target.Health > 0 && source != null && HostileTo(source, target)
                && target.Tactics is { DefinitionId: "core:enemy/bastion_bulwark", Enemy: { } memory })
            {
                memory.Retaliate = true;
                Emit("RetaliationReadied", target.Id, target.Id, attack.Id, target.Position);
            }
            if (attack.FateFace == 7 && (!RegularBoonTarget(target) || hit.Snapshot.ActualLoss < hit.Snapshot.HpBefore))
                Emit("FateSkullResisted", attack.Source, target.Id, attack.Id, target.Position);
            if (hit.Damage <= 0) continue;
            if (target.Life == LifeState.Downed)
            {
                target.Tactics!.BleedTurns--;
                if (target.Tactics.BleedTurns == 0) Die(target, attack.Source, attack.Id, hit.Snapshot);
                else Emit("BleedShortened", attack.Source, target.Id, attack.Id, target.Position, target.Tactics.BleedTurns);
            }
            else if (target.Health == 0)
            {
                if (Friendly(target)) Down(target, attack.Source, attack.Id);
                else Die(target, attack.Source, attack.Id, hit.Snapshot);
            }
        }
        foreach (var hit in hits)
            if (!hit.Invulnerable && !hit.Snapshot.OathSharedOnly)
                Emit("HitConfirmed", attack.Source, hit.Target.Id, attack.Id, hit.Target.Position,
                    hit.Snapshot.ActualLoss!.Value, hit.Prevented, attack.Ability, snapshot: hit.Snapshot);
    }

    private bool Guards(Actor target, LockedAttack attack)
    {
        if (attack.Cause is not (DamageCause.DirectAttack or DamageCause.TriggeredEffect) || target.Tactics is not { Guarding: true } tactics
            || target.Position.Floor != attack.Origin.Floor) return false;
        int dx = Math.Sign(attack.Origin.X - target.Position.X), dy = Math.Sign(attack.Origin.Y - target.Position.Y);
        if (!AttackGeometry.TryFacing(dx, dy, out var incoming)) return false;
        int difference = ((int)incoming - (int)tactics.Facing + 8) % 8;
        return difference is 0 or 1 or 7;
    }

    private void Down(Actor actor, long source, long action)
    {
        actor.Life = LifeState.Downed; actor.Health = 0;
        actor.Tactics!.BleedTurns = 5; actor.Tactics.Guarding = false; actor.Tactics.Resources.Parry = null;
        CancelAttacks(actor.Id);
        // Preserve the member's own pending readiness; downing cannot buy a fresh recovery period.
        if (State.ActiveActor == actor.Id || !State.Queue.Snapshot().Pending.Any(e => e.Kind == ScheduledKind.ActorReady && e.ActorId == actor.Id))
        { State.ActiveActor = State.ActiveActor == actor.Id ? 0 : State.ActiveActor; ScheduleReady(actor, Tuning.WaitCost); }
        Emit("Downed", source, actor.Id, action, actor.Position, 5);
    }

    private void Die(Actor actor, long source, long action, BoonEvent? snapshot = null)
    {
        snapshot ??= CaptureBoonEvent(BoonTrigger.EntityKilled, source, actor.Id, action, actor.Position);
        RemoveBurning(actor);
        actor.Life = LifeState.Dead; actor.Health = 0;
        PruneOaths();
        ReleaseCarriedLoot(actor, action);
        RememberRevivalCorpse(actor);
        // Owned traps disappear on death, including traps on an offscreen floor.
        State.EnemySnares.RemoveAll(s => s.Source == actor.Id);
        State.Webs.RemoveAll(w => w.Source == actor.Id);
        if (actor.Tactics != null) { actor.Tactics.EnemySnareDelay = 0; actor.Tactics.WebRecoveryDelay = 0; }
        if (actor.Tactics is { } tactics) { tactics.BleedTurns = 0; tactics.Guarding = false; tactics.Resources.Parry = null; tactics.SnaredBy = 0; if (tactics.Enemy != null) tactics.Enemy.Dormant = false; }
        State.Queue.CancelActor(actor.Id, ScheduledKind.ActorReady); CancelAttacks(actor.Id, source, action, "death");
        if (State.ActiveActor == actor.Id) State.ActiveActor = 0;
        Emit("Died", source, actor.Id, action, actor.Position);
        Emit("EntityKilled", source, actor.Id, action, actor.Position, snapshot: snapshot);
        if (Friendly(actor) && !_world.Party.Any(a => a.Life != LifeState.Dead))
        {
            _world.State.AwaitingContinuation = true;
            foreach (var work in State.Queue.Snapshot().Pending.Where(w => w.Kind == ScheduledKind.Survival)) State.Queue.Cancel(work.Sequence);
        }
    }

    private void CancelAttacks(long actor, long source = 0, long action = 0, string? reason = null)
    {
        EnemySentinel.Interrupt(Actor(actor).Tactics);
        var cancelled = State.Attacks.Where(a => a.Source == actor && !a.ClassGenerated).Select(a => a.Id).ToHashSet();
        foreach (var work in State.Queue.Snapshot().Pending.Where(e => e.Kind == ScheduledKind.Impact && cancelled.Contains(e.ReferenceId)))
            State.Queue.Cancel(work.Sequence);
        if (reason != null)
            foreach (var attack in State.Attacks.Where(a => cancelled.Contains(a.Id)))
                Emit("WindupCancelled", source, actor, action, Actor(actor).Position, ability: reason);
        State.Attacks.RemoveAll(a => cancelled.Contains(a.Id));
        PruneRevivalCorpses();
    }
    private void ScheduleReady(Actor actor, int cost)
        => State.Queue.Schedule(ScheduledKind.ActorReady, checked(Clock + ActionTiming.Recovery(cost, EffectiveSpeed(actor))), actor.Id, Friendly(actor));
    private void Emit(string kind, long source, long target, long action, WorldPosition position, int amount = 0,
        int prevented = 0, string ability = "", bool forced = false, WorldPosition? from = null, BoonEvent? snapshot = null, LootChange? loot = null,
        EnemyProtection? protection = null, ImmutableArray<WorldPosition> webCuts = default, MirrorReflection? reflection = null)
    {
        var oath = snapshot is { OathGiven: > 0 } or { OathReceived: > 0 }
            ? new OathDamage(snapshot.OathGiven, snapshot.OathReceived, snapshot.OathSharedOnly) : null;
        var entry = new TacticalEvent(State.NextEvent++, Clock, kind, source, target, action, position, amount, prevented, ability, forced, from, loot, protection, oath, reflection);
        State.Events.Add(entry);
        if (State.Events.Count > 512) State.Events.RemoveRange(0, State.Events.Count - 512);
        QueueCombatEvent(entry, snapshot, webCuts);
    }
    private static TacticalPreview Invalid(string reason) => new(false, reason, 0, []);
}
