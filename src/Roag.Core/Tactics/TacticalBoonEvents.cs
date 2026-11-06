using System.Collections.Immutable;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private readonly record struct QueuedCombatEvent(TacticalEvent Display, BoonCause Cause, BoonEvent? Boon,
        ImmutableArray<WorldPosition> WebCuts = default);
    private readonly Queue<QueuedCombatEvent> _combatEvents = new();
    private readonly Queue<(BoonActivation Activation, BoonEffect Effect)> _boonWork = new();
    private BoonCause? _cause;
    private int _resolutionDepth;
    private bool _drainingBoons;
    private bool _lastWasDash;

    private BoonCause RootCause(long action, bool generated = false, double coefficient = 1) =>
        new(action > 0 ? action : State.NextAction++, action > 0 ? action : null, [], coefficient, generated);

    private void WithBoonCause(BoonCause cause, Action body)
    {
        var prior = _cause; _cause = cause;
        try { body(); }
        finally { _cause = prior; }
    }

    private void ResolveAtomically(BoonCause cause, Action body)
    {
        _resolutionDepth++;
        try { WithBoonCause(cause, body); }
        finally { _resolutionDepth--; }
        if (_resolutionDepth == 0 && !_drainingBoons) DrainBoons();
    }

    private string BoonFaction(Actor actor) => Friendly(actor) || actor.Kind is ActorKind.Person or ActorKind.Caravan ? "player" : "enemy";
    private static string TacticalDefinition(Actor actor) => actor.Tactics?.Build is { } build ? ClassCatalog.Get(build.Class).Id
        : actor.Tactics?.DefinitionId is { Length: > 0 } definition ? definition : actor.Archetype;

    private BoonEvent CaptureBoonEvent(BoonTrigger kind, long source, long target, long action, WorldPosition? position,
        string ability = "", WorldPosition? from = null)
    {
        var actor = _world.State.Actors.FirstOrDefault(a => a.Id == source);
        var victim = _world.State.Actors.FirstOrDefault(a => a.Id == target);
        var cause = _cause ?? RootCause(action);
        return new()
        {
            Id = State.NextEvent, ChainId = cause.ChainId, RootActionId = cause.RootActionId, Kind = kind,
            Ancestry = cause.Ancestry, ProcCoefficient = cause.ProcCoefficient, ClassGenerated = cause.ClassGenerated,
            SourceId = source, TargetId = target == 0 ? null : target, SourceFaction = actor is null ? source == 0 ? "environment" : null : BoonFaction(actor),
            TargetFaction = victim is null ? null : BoonFaction(victim), SourcePosition = actor?.Position,
            Position = position, From = from, AbilityId = ability.Length == 0 ? null : ability,
            SourceTags = actor is null ? [] : [TacticalDefinition(actor), BoonFaction(actor)],
            TargetTags = victim is null ? [] : [TacticalDefinition(victim), BoonFaction(victim)],
            TargetDefinitionId = victim is null ? null : TacticalDefinition(victim),
            HpBefore = victim?.Health, TargetMaxHp = victim?.MaxHealth,
            SourceHpAtAttack = actor?.Health, SourceMaxHpAtAttack = actor?.MaxHealth,
            RewardExempt = victim?.Tactics?.RewardExempt == true,
            Status = kind is BoonTrigger.StatusApplied or BoonTrigger.StatusCleansed ? BoonStatus.Burning : null
        };
    }

    private void QueueCombatEvent(TacticalEvent display, BoonEvent? snapshot, ImmutableArray<WorldPosition> webCuts)
    {
        // Internal effect-unit execution has no event transaction; public commands and scheduled work do.
        if (_resolutionDepth == 0 && !_drainingBoons) return;
        var cause = _cause ?? RootCause(display.Action);
        if (!Enum.TryParse<BoonTrigger>(display.Kind, out var kind))
        {
            if (display.Kind == "Died") _combatEvents.Enqueue(new(display, cause, null));
            return;
        }
        var e = snapshot ?? CaptureBoonEvent(kind, display.Source, display.Target, display.Action,
            display.Position, display.Ability, display.From) with
        { Amount = display.Amount };
        e = e with { Id = display.Sequence, Kind = kind, ChainId = cause.ChainId, RootActionId = cause.RootActionId,
            Ancestry = cause.Ancestry, ProcCoefficient = e.OathSharedOnly ? 0 : cause.ProcCoefficient, ClassGenerated = cause.ClassGenerated };
        _combatEvents.Enqueue(new(display, cause, e, webCuts));
    }

    private BoonEvaluationContext BoonWorld()
    {
        var visions = new Dictionary<long, HashSet<WorldPosition>>();
        bool Visible(long owner, WorldPosition p)
        {
            if (!visions.TryGetValue(owner, out var vision)) visions.Add(owner, vision = _world.VisionFrom(Actor(owner).Position));
            return vision.Contains(p);
        }
        string? Terrain(WorldPosition p) => !_world.Plan.Contains(p) ? null : _world.DungeonTerrainAt(p) is { } glyph ? glyph.ToString() : _world.Chunks.At(p) switch
        { Material.Barricade => "B", Material.Water => "~", _ => _world.GroundOpen(p) ? "." : "#" };
        return new(Clock, BoonBodies(), Terrain, p => _world.Plan.Contains(p) ? _world.ObjectAt(p)?.Kind : null, Visible,
            definition => _world.State.Actors.FirstOrDefault(a => TacticalDefinition(a) == definition)?.Tactics?.CurrencyDrop,
            _lastWasDash);
    }

    private void DrainBoons()
    {
        PruneWebs();
        PruneSilt();
        PruneEmbers();
        PruneMirrors();
        PruneOaths();
        _drainingBoons = true;
        try
        {
            int steps = 0;
            while (_combatEvents.Count > 0 || _boonWork.Count > 0 || _contractCompletionActor != 0)
            {
                if (++steps > 1_000_000) throw new InvalidOperationException("Boon resolution exceeded the original diagnostic limit.");
                if (_combatEvents.TryDequeue(out var queued))
                {
                    if (queued.Boon is not { } e)
                    {
                        WithBoonCause(queued.Cause, () => ResolveEnemyDeath(queued.Display));
                        continue;
                    }
                    WithBoonCause(BoonCause.From(e), () =>
                    {
                        BeforeBoonEvent(e);
                        if (e.ClassGenerated)
                        {
                            ResolveWebEvent(e, queued.WebCuts); ResolveKillRewards(e); ResolveEnemyPhase(e); ResolveRoomFeatureEvent(e); ResolveVaultEvent(e); ResolveCorridorQuestEvent(e); ResolveSecretEvent(e); ResolveTerrainEvent(e); return;
                        }
                        // A world's idle creatures also emit real Waited events.
                        // Avoid constructing/sorting every world body when none
                        // of the owned wait subscriptions can observe this one.
                        bool dispatch = _world.State.NativeCampaign == null || e.Kind != BoonTrigger.Waited || HasWaitSubscriber(e);
                        var random = new RoagRandom(State.RandomState);
                        if (!dispatch) e.Validate(BoonCatalog.Default);
                        else BoonDispatch.OnEvent(State.BoonEffects.Dispatch, BoonWorld(), e, random, activation =>
                        {
                            WithBoonCause(BoonCause.From(activation), () =>
                            {
                                var snapshot = CaptureBoonEvent(BoonTrigger.BoonActivated, activation.OwnerId, 0,
                                    activation.RootActionId ?? 0, e.Position ?? e.From) with { BoonId = activation.BoonId };
                                Emit("BoonActivated", activation.OwnerId, 0, activation.RootActionId ?? 0,
                                    snapshot.ObservedPosition ?? activation.SourcePosition, checked((int)activation.Power), ability: activation.BoonId, snapshot: snapshot);
                            });
                        }, (activation, effect) => _boonWork.Enqueue((activation, effect)));
                        State.RandomState = random.State;
                        ResolveWebEvent(e, queued.WebCuts);
                        ResolveKillRewards(e);
                        ResolveEnemyPhase(e);
                        ResolveRoomFeatureEvent(e);
                        ResolveVaultEvent(e);
                        ResolveCorridorQuestEvent(e);
                        ResolveSecretEvent(e);
                        ResolveTerrainEvent(e);
                    });
                }
                else if (_boonWork.TryDequeue(out var work))
                    WithBoonCause(BoonCause.From(work.Activation, work.Effect.ProcCoefficient ?? 1), () => ExecuteBoonEffect(work.Activation, work.Effect));
                else if (_contractCompletionActor != 0) CompleteContracts();
            }
        }
        finally { _drainingBoons = false; _terrainContacts.Clear(); _pushFloorCounts.Clear(); }
        SettleFragileTerrain();
        SettleOverdueGates();
        SettlePendingEliteRewards();
        SettleDungeonVictory();
        PruneWebs();
        PruneSilt();
        PruneEmbers();
        PruneMirrors();
        PruneOaths();
        // Idle native turns usually have no causal history at all. There is
        // nothing to retain/remove then; avoid constructing iterators and
        // scanning every world actor three times for the empty dictionary.
        if (State.BoonEffects.Dispatch.Chains.Count > 0)
        {
            var retained = State.BoonEffects.Echoes.Values.Select(e => e.Activation.ChainId)
                .Concat(_world.State.Actors.Where(a => a.Tactics?.Burning?.Activation != null).Select(a => a.Tactics!.Burning!.Activation!.ChainId))
                .Concat(_world.State.Actors.Where(a => a.Tactics?.Burning?.Native != null).Select(a => a.Tactics!.Burning!.Native!.Action))
                .Concat(_world.State.Actors.Where(a => a.Tactics != null).SelectMany(a => a.Tactics!.Conditions.Select(c => c.Action)))
                .Concat(State.Attacks.Where(a => !a.ClassGenerated).Select(a => a.Causality?.ChainId ?? a.Id));
            State.BoonEffects.Dispatch.Prune(retained);
        }
        SettleProfileProgress();
    }

    private bool HasWaitSubscriber(BoonEvent e)
    {
        var subscriptions = BoonCatalog.Default.Subscribers(BoonTrigger.Waited);
        if (subscriptions.All(d => d.Scope.Kind == BoonScopeKind.Owner))
        {
            foreach (long id in new[] { e.SourceId ?? 0, e.TargetId ?? 0 }.Distinct().Where(id => id > 0))
                if (Actor(id) is { Life: not LifeState.Dead, Tactics.Boons.Inventory: { } inventory }
                    && subscriptions.Any(d => inventory.Stacks.ContainsKey(d.Id))) return true;
            return false;
        }
        foreach (var actor in _world.State.Actors)
        {
            if (!State.Participants.Contains(actor.Id) || actor.Life == LifeState.Dead || actor.Tactics?.Boons.Inventory is not { Stacks.Count: > 0 } inventory) continue;
            var owner = new BoonActorView(actor, BoonFaction(actor), RegularBoonTarget(actor));
            if (subscriptions.Any(d => inventory.Stacks.ContainsKey(d.Id) && BoonConditions.Observes(owner, d.Scope, e))) return true;
        }
        return false;
    }

    private void BeforeBoonEvent(BoonEvent e)
    {
        ResolveContractEvent(e);
        if (e.SourceId is not { } id || id == 0) return;
        var source = Actor(id);
        if (e.Kind == BoonTrigger.AttackPerformed)
            foreach (var cell in e.TargetCells)
                if (_world.Chunks.At(cell) == Material.Barricade)
                {
                    _world.Chunks.Edit(cell, Material.Floor);
                    Emit("TerrainBroken", id, 0, e.RootActionId ?? 0, cell, ability: "core:terrain/barricade");
                }
        if (e.Kind == BoonTrigger.HitConfirmed && e.ActualLoss > 0 && e.DirectAttack && !e.ClassGenerated && e.RootActionId is { } action)
            ChargeStorm(source, new(action, source.Id, source.Position, [], e.AbilityId ?? "", AttackDelivery.Melee,
                e.DamageType ?? DamageElement.Physical, 0, Clock));
        if (e.Kind == BoonTrigger.Moved && source.Tactics?.Boons.Inventory.Stacks.ContainsKey("core:boon/ring_of_reversal") == true
            && e.From is { } from && e.Position is { } to)
        {
            foreach (var enemy in _world.State.Actors)
            {
                if (!HostileTo(source, enemy)) continue;
                if (!State.Attacks.Any(a => a.Source == enemy.Id && !a.ClassGenerated && a.Cells.Contains(from) && !a.Cells.Contains(to))) continue;
                source.Tactics.Boons.ReversalTarget = enemy.Id;
                Emit("RingReversalPrimed", source.Id, enemy.Id, e.RootActionId ?? 0, to);
                break;
            }
        }
        if (e.Kind == BoonTrigger.TileEntered && source.Life == LifeState.Alive && e.Position is { } entered)
        {
            ExpireUtilityMarker(source, entered, e.RootActionId ?? 0);
            TriggerSnares(source, e.RootActionId ?? 0, entered);
            TriggerEnemySnares(source, e.RootActionId ?? 0, entered);
        }
    }

    private static double AbilityCoefficient(string ability) => ability is "core:ability/pursuer_strike" or "core:ability/tinkerer_sentry" ? 0 : 1;
}
