using System.Collections.Immutable;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private bool HostileTo(Actor owner, Actor target) => Friendly(owner)
        ? !Friendly(target) && _world.IsHostile(target) : Friendly(target);
    private Actor? BodyAt(WorldPosition p, long except = 0) => _world.State.Actors
        .FirstOrDefault(a => a.Id != except && a.Life != LifeState.Dead && a.Position == p);
    private int SpecialPower(Actor actor) => Math.Max(0, actor.Tactics!.Stats.SpecialDamage
        + actor.Equipment.Values.Sum(e => e.Power) - actor.Body.Wounds / 2 - (actor.Body.Fatigue >= 80 ? 2 : 0));
    private AttackFootprint ClassFootprint(Actor actor, TacticalIntent intent, bool explicitAim = true)
        => AttackGeometry.For(actor.Tactics!.Build!.Class, intent.Action, actor.Position, intent.Facing,
            ProjectileOpen, p => BodyAt(p) != null, actor.Tactics.Resources.Spirit != null, explicitAim);

    private TacticalPreview PreviewMovement(Actor actor, TacticalClass? definition, TacticalIntent intent)
    {
        int cost = intent.Action == TacticalAction.Dash ? definition?.Costs.Dash ?? actor.Tactics!.MoveCost
            : definition?.Costs.Move ?? actor.Tactics!.MoveCost;
        long recovery = ActionTiming.Recovery(cost, EffectiveSpeed(actor));
        if (intent.Action == TacticalAction.Dash && actor.Tactics!.Resources.Beacon is { } beacon)
            return PreviewBeaconReturn(actor, beacon, recovery);
        if (intent.Action == TacticalAction.Dash && definition?.Kind == ClassKind.Shade)
        {
            var decoy = actor.Tactics!.Resources.Decoy;
            if (decoy is null || decoy.Value.Floor != actor.Position.Floor) return Invalid("Place a decoy on this floor first.");
            if (!_world.CanEnter(decoy.Value, actor.Id)) return Invalid("The decoy's tile is blocked.");
            return new(true, "", recovery, [decoy.Value], Swap: true);
        }
        if (!AttackGeometry.Cardinal(intent.Facing)) return Invalid("Movement follows cardinal tiles.");
        var (dx, dy) = AttackGeometry.Offset(intent.Facing);
        int distance = intent.Action == TacticalAction.Dash ? definition?.DashDistance ?? 1 : 1;
        int maximum = intent.Action == TacticalAction.Dash ? Math.Max(3, distance) : distance;
        var cells = new List<WorldPosition>();
        for (int step = 1; step <= maximum; step++)
        {
            if (step > distance && !cells.Take(2).Any(p => _world.DungeonTerrainAt(p) == 'i')) break;
            var cell = actor.Position.Offset(dx * step, dy * step);
            if (!_world.CanEnter(cell, actor.Id))
            {
                if (intent.Action == TacticalAction.Move && _world.DungeonDoorAt(cell) is { } door)
                    return PreviewDoor(actor, door.Id);
                if (intent.Action == TacticalAction.Move && definition != null && BodyAt(cell) is { Life: LifeState.Alive } target
                    && HostileTo(actor, target))
                    return new(true, "", ActionTiming.Recovery(definition.Costs.BasicAttack, EffectiveSpeed(actor)),
                        ClassFootprint(actor, intent with { Action = TacticalAction.BasicAttack }, explicitAim: false).Cells, Bump: true);
                break;
            }
            cells.Add(cell);
        }
        recovery = ActionTiming.Recovery(checked(cost + SiltMovementExtra(cells, intent.Action) + TerrainMovementExtra(cells, intent.Action)), EffectiveSpeed(actor));
        if (intent.Action == TacticalAction.Move && cells.Count == 1
            && _world.Chunks.At(cells[0]) is Material.StairsDown or Material.StairsUp)
        {
            var landing = _world.StairLanding(cells[0]);
            if (landing == null || !_world.CanEnter(landing.Value, actor.Id)) return Invalid("The stair is obstructed.");
            return new(true, "", recovery, cells) { StairLanding = landing };
        }
        return cells.Count == 0 ? Invalid("That tile is occupied or blocked.") : new(true, "", recovery, cells);
    }

    private TacticalPreview? PreviewSpecial(Actor actor, TacticalIntent intent, AttackFootprint footprint)
    {
        var kind = actor.Tactics!.Build!.Class;
        var resources = actor.Tactics.Resources;
        long cost = ActionTiming.Recovery(ClassCatalog.Get(kind).Costs.Special, EffectiveSpeed(actor));
        TacticalPreview Valid() => new(true, "", cost, footprint.Cells);
        switch (kind)
        {
            case ClassKind.Shade:
            case ClassKind.Trapper:
                return footprint.Cells.Count == 1 && _world.CanEnter(footprint.Cells[0], actor.Id)
                    ? Valid() : Invalid("Choose empty adjacent ground.");
            case ClassKind.Duelist: return Valid();
            case ClassKind.Stormcaller:
                return resources.StormCharge > 0 && footprint.Cells.Count > 0 && footprint.Cells.All(_world.Visible.Contains)
                    && !State.Attacks.Any(a => a.Source == actor.Id && a.Ability == footprint.AbilityId)
                    ? Valid() : Invalid("Needs charge, a visible clear cross and no pending discharge.");
            case ClassKind.Spiritbinder:
                if (footprint.Cells.Count != 1 || !_world.Visible.Contains(footprint.Cells[0])) return Invalid("Choose visible nearby ground.");
                var cell = footprint.Cells[0];
                if (BodyAt(cell) is { } target)
                {
                    if (resources.Spirit is not { } spirit || !HostileTo(actor, target) || target.Life != LifeState.Alive
                        || spirit.Floor != cell.Floor || Math.Max(Math.Abs(spirit.X - cell.X), Math.Abs(spirit.Y - cell.Y)) != 1
                        || spirit.X != cell.X && spirit.Y != cell.Y
                            && (!ProjectileOpen(new(spirit.X, cell.Y, cell.Floor)) || !ProjectileOpen(new(cell.X, spirit.Y, cell.Floor))))
                        return Invalid("Place the spirit beside the target with a clear strike route.");
                    return Valid();
                }
                return PlainPlacement(cell) ? Valid() : Invalid("The spirit needs empty plain floor away from stairs.");
            case ClassKind.Tinkerer:
                return footprint.Cells.Count == 1 && actor.Position.Distance(footprint.Cells[0]) == 1
                    && _world.Visible.Contains(footprint.Cells[0]) && PlainPlacement(footprint.Cells[0])
                    ? Valid() : Invalid("The sentry needs visible empty adjacent cardinal floor.");
            case ClassKind.Geomancer:
                return footprint.Cells.Count == 1 && actor.Position.Distance(footprint.Cells[0]) == 1
                    && PlainPlacement(footprint.Cells[0], actor.Id) && BulwarkPreservesRoutes(actor, footprint.Cells[0])
                    ? Valid() : Invalid("Choose empty adjacent plain ground without sealing an accessible route.");
            case ClassKind.Wayfinder:
                return footprint.Cells.Count == 1 && actor.Position.Distance(footprint.Cells[0]) == 1
                    && PlainPlacement(footprint.Cells[0]) && _world.Visible.Contains(footprint.Cells[0])
                    && _world.Chunks.Seen(footprint.Cells[0]) && _world.RoomAt(actor.Position) is { } room
                    && _world.RoomAt(footprint.Cells[0]) == room
                    ? Valid() : Invalid("Plant on visible explored plain ground in your current room, away from stairs.");
            default: return null;
        }
    }

    private bool PlainPlacement(WorldPosition p, long ignoredBulwarkOwner = 0) => _world.Plan.Contains(p)
        && _world.DungeonTerrainAt(p) == null
        && _world.Chunks.At(p) is Material.Floor or Material.Earth or Material.Grass or Material.Bridge or Material.Door
        && _world.GroundOpen(p, ignoredBulwarkOwner) && BodyAt(p) == null;

    private void CommitMovement(Actor actor, TacticalIntent intent, TacticalPreview preview, long action)
    {
        var origin = actor.Position;
        foreach (var cell in preview.Cells) _world.MoveTacticalActor(actor, cell);
        if (preview.ReturnDistance == null)
        {
            string ability = intent.Action == TacticalAction.Dash ? "core:ability/dash" : "";
            var moved = CaptureBoonEvent(BoonTrigger.Moved, actor.Id, 0, action, actor.Position, ability, origin)
                with { Path = intent.Action == TacticalAction.Dash ? preview.Cells.ToImmutableArray() : [] };
            Emit("Moved", actor.Id, actor.Id, action, actor.Position, ability: ability, from: origin, snapshot: moved);
        }
        var from = origin;
        foreach (var cell in preview.Cells)
        {
            if (preview.ReturnDistance != null)
                Emit("Displaced", actor.Id, actor.Id, action, cell, forced: true, from: from);
            Emit("TileEntered", actor.Id, actor.Id, action, cell, forced: preview.ReturnDistance != null, from: from,
                snapshot: CaptureBoonEvent(BoonTrigger.TileEntered, actor.Id, 0, action, cell, from: from) with { SourcePosition = cell });
            CollectLootAt(actor, cell, action);
            from = cell;
        }
        if (preview.Swap)
        {
            actor.Tactics!.Resources.Decoy = null;
            Emit("DecoySwapped", actor.Id, actor.Id, action, actor.Position);
        }
        if (preview.ReturnDistance is { } distance)
        {
            actor.Tactics!.Resources.Beacon = null;
            Emit("BeaconReturned", actor.Id, actor.Id, action, actor.Position, distance);
            return; // A return is displacement, not a path traversal or ordinary Moved proc.
        }
        if (intent.Action == TacticalAction.Dash && actor.Tactics!.Resources.StormCharge > 0)
        {
            actor.Tactics.Resources.StormCharge = 0;
            actor.Tactics.Resources.StormWard = true;
            Emit("StormGrounded", actor.Id, actor.Id, action, actor.Position);
        }
        if (preview.StairLanding is { } landing) CommitStairs(actor, landing, action);
    }

    private void CommitClassAttack(Actor actor, TacticalIntent intent, TacticalPreview preview, long action)
    {
        var resources = actor.Tactics!.Resources;
        var effective = ClassAttack(actor, intent, explicitAim: !preview.Bump, knownOnly: false);
        var footprint = effective.Footprint;
        if (intent.Action == TacticalAction.Special)
        {
            switch (actor.Tactics.Build!.Class)
            {
                case ClassKind.Shade:
                    resources.Decoy = footprint.Cells[0];
                    Emit("DecoyPlaced", actor.Id, actor.Id, action, footprint.Cells[0]); return;
                case ClassKind.Trapper:
                    resources.Snare = footprint.Cells[0];
                    Emit("SnarePlaced", actor.Id, actor.Id, action, footprint.Cells[0]); return;
                case ClassKind.Duelist:
                    resources.Parry = new(actor.Position, intent.Facing, checked(Clock + preview.Recovery));
                    Emit("DuelistGuardRaised", actor.Id, actor.Id, action, actor.Position); return;
                case ClassKind.Stormcaller:
                    int charge = resources.StormCharge; resources.StormCharge = 0;
                    var storm = new LockedAttack(action, actor.Id, actor.Position, footprint.Cells.ToArray(), footprint.AbilityId,
                        AttackDelivery.Area, DamageElement.Lightning, SpecialPower(actor) + 2 * charge, checked(Clock + 100), ClassGenerated: true);
                    State.Attacks.Add(storm);
                    State.Queue.Schedule(ScheduledKind.Impact, storm.Due, actor.Id, referenceId: action);
                    Emit("StormDischargeLocked", actor.Id, actor.Id, action, actor.Position, storm.Damage, ability: storm.Ability); return;
                case ClassKind.Spiritbinder:
                    if (BodyAt(footprint.Cells[0]) == null)
                    {
                        resources.Spirit = footprint.Cells[0];
                        Emit("SpiritPositioned", actor.Id, actor.Id, action, footprint.Cells[0]); return;
                    }
                    Strike(actor, action, footprint with { Delivery = AttackDelivery.Melee, Element = DamageElement.Spirit },
                        SpecialPower(actor), resources.Spirit!.Value, preview.Vow); return;
                case ClassKind.Tinkerer:
                    if (resources.Sentry is { Schedule: > 0 } old) State.Queue.Cancel(old.Schedule);
                    resources.Sentry = new() { Position = footprint.Cells[0], Action = action };
                    ScheduleSentry(actor, SentryPhase.Waiting, 100);
                    Emit("SentryDeployed", actor.Id, actor.Id, action, footprint.Cells[0], 3); return;
                case ClassKind.Geomancer:
                    if (resources.Bulwark is { } oldBlock) State.Queue.Cancel(oldBlock.Schedule);
                    long expiry = checked(Clock + 300);
                    var work = State.Queue.Schedule(ScheduledKind.Effect, expiry, actor.Id, referenceId: action);
                    resources.Bulwark = new(footprint.Cells[0], action, work.Sequence, expiry);
                    Emit("GeomancerBlockRaised", actor.Id, actor.Id, action, footprint.Cells[0]);
                    _world.Reveal(); return;
                case ClassKind.Wayfinder:
                    resources.Beacon = new(footprint.Cells[0], _world.RoomAt(footprint.Cells[0])!.Value);
                    Emit("BeaconPlanted", actor.Id, actor.Id, action, footprint.Cells[0]); return;
            }
        }
        int damage = effective.Damage;
        if (intent.Action == TacticalAction.BasicAttack && resources.Riposte)
        {
            resources.Riposte = false;
            Emit("DuelistRiposteSpent", actor.Id, actor.Id, action, actor.Position, 2);
        }
        Actor? hooked = intent.Action == TacticalAction.Special && actor.Tactics.Build!.Class == ClassKind.Hookrunner
            ? footprint.Cells.Select(p => BodyAt(p, actor.Id)).FirstOrDefault(a => a != null) : null;
        ConsumeMirror(actor, action, effective.Reflection, footprint.Cells);
        bool performed = Strike(actor, action, footprint, damage, actor.Position, preview.Vow);
        if (performed && hooked is { Life: LifeState.Alive } && hooked.Tactics is not { ImmuneToPull: true } and not { Invulnerable: true })
        {
            var (dx, dy) = AttackGeometry.Offset(intent.Facing);
            for (int i = 0; i < 2; i++)
            {
                var next = hooked.Position.Offset(-dx, -dy);
                if (!_world.CanEnter(next, hooked.Id)) break;
                var from = hooked.Position;
                _world.MoveTacticalActor(hooked, next);
                RecordCombatControl(actor.Id, hooked, ActionTiming.Recovery(hooked.Tactics!.MoveCost, EffectiveSpeed(hooked)));
                Emit("Displaced", actor.Id, hooked.Id, action, next, forced: true, from: from);
                Emit("TileEntered", hooked.Id, hooked.Id, action, next, forced: true, from: from);
                CollectLootAt(hooked, next, action);
                if (hooked.Life != LifeState.Alive) break;
            }
        }
    }

    private bool Strike(Actor actor, long action, AttackFootprint footprint, int damage, WorldPosition origin, BoonVowPayment? vow)
    {
        var random = new RoagRandom(State.RandomState);
        var fate = actor.Tactics!.Boons.RollDice(random, action);
        if (fate != null)
        {
            Emit("FateRolled", actor.Id, actor.Id, action, origin, fate.Face, ability: "core:boon/fateful_dice");
            if (fate.Face == 2) damage = checked((int)(damage + fate.Power));
        }
        if (vow != null) damage = checked((int)(damage + vow.DamageBonus));
        if (actor.Tactics.Supplies.FocusActions > 0)
        {
            actor.Tactics.Supplies.FocusActions = 0;
            damage = checked(damage + 2);
            Emit("SupplyFocusSpent", actor.Id, actor.Id, action, origin, 2);
        }
        bool miss = fate?.Face == 8;
        if (miss) { footprint = footprint with { Cells = [] }; damage = 0; }
        bool critical = false;
        if (!miss && actor.Tactics.Stats.CriticalChancePartsPerMillion > 0)
        {
            critical = random.NextOneBased(1_000_000) <= actor.Tactics.Stats.CriticalChancePartsPerMillion;
            if (critical) damage = checked((int)(((long)damage * actor.Tactics.Stats.CriticalDamagePercent + 50) / 100));
        }
        State.RandomState = random.State;
        Emit("AttackPerformed", actor.Id, 0, action, origin, ability: footprint.AbilityId,
            snapshot: CaptureBoonEvent(BoonTrigger.AttackPerformed, actor.Id, 0, action, origin, footprint.AbilityId)
                with { TargetCells = footprint.Cells.ToImmutableArray(), DamageType = footprint.Element, DirectAttack = true,
                    FateFace = fate?.Face, FateMiss = miss },
            webCuts: CaptureWebCuts(actor.Id, footprint.Cells, footprint.Delivery, damage));
        ResolveAttack(new(action, actor.Id, origin, footprint.Cells.ToArray(), footprint.AbilityId,
            footprint.Delivery, footprint.Element, damage, Clock, Critical: critical, FateFace: fate?.Face));
        return !miss;
    }

    private int ClassProtection(Actor target, Actor? source, LockedAttack attack, int damage)
    {
        if (damage <= 0 || target.Tactics is not { } tactics) return 0;
        var resources = tactics.Resources;
        if (resources.Parry is { } parry && parry.Expires >= Clock && target.Position == parry.Origin
            && attack.Cause == DamageCause.DirectAttack && !attack.ClassGenerated && attack.Delivery == AttackDelivery.Melee
            && source != null && HostileTo(target, source) && AttackGeometry.Arc(parry.Origin, parry.Facing).Contains(attack.Origin))
        {
            resources.Parry = null; resources.Riposte = true;
            Emit("DuelistParried", target.Id, source.Id, attack.Id, target.Position, damage); return damage;
        }
        if (resources.StormWard)
        {
            resources.StormWard = false;
            Emit("StormWardAbsorbed", target.Id, target.Id, attack.Id, target.Position, damage); return damage;
        }
        return 0;
    }

    private void ChargeStorm(Actor source, LockedAttack attack)
    {
        if (source.Tactics?.Build?.Class != ClassKind.Stormcaller || attack.ClassGenerated
            || attack.Cause != DamageCause.DirectAttack || attack.Ability != ClassCatalog.Get(ClassKind.Stormcaller).BasicAbilityId
            || source.Tactics.Resources.StormLastAction == attack.Id) return;
        var resources = source.Tactics.Resources;
        resources.StormLastAction = attack.Id; resources.StormCharge = Math.Min(3, resources.StormCharge + 1);
        Emit("StormChargeGained", source.Id, source.Id, attack.Id, source.Position, resources.StormCharge);
    }

    private void TriggerSnares(Actor victim, long action, WorldPosition? entered = null)
    {
        foreach (var owner in _world.State.Actors.OrderBy(a => a.Id))
        {
            if (owner.Tactics?.Resources.Snare != (entered ?? victim.Position) || !HostileTo(owner, victim)) continue;
            owner.Tactics.Resources.Snare = null;
            victim.Tactics!.SnaredBy = owner.Id;
            Emit("SnareTriggered", owner.Id, victim.Id, action, victim.Position);
            ResolveAttack(new(action, owner.Id, victim.Position, [victim.Position], "core:ability/trapper_snare",
                AttackDelivery.Melee, DamageElement.Physical, 3, Clock, DamageCause.GroundHazard,
                PersistsWithoutOwner: true));
            if (victim.Life != LifeState.Alive) break;
        }
    }

    private void ScheduleSentry(Actor owner, SentryPhase phase, int delay)
    {
        var sentry = owner.Tactics!.Resources.Sentry!;
        sentry.Phase = phase; sentry.Due = checked(Clock + delay);
        sentry.Schedule = State.Queue.Schedule(ScheduledKind.Effect, sentry.Due, owner.Id, referenceId: sentry.Action).Sequence;
    }

    private void ResolveClassEffect(ScheduledAction work)
    {
        var owner = Actor(work.ActorId);
        if (owner.Tactics!.Resources.Bulwark is { } block && block.Schedule == work.Sequence
            && block.Action == work.ReferenceId && block.Expires == Clock)
        {
            owner.Tactics.Resources.Bulwark = null;
            Emit("GeomancerBlockExpired", owner.Id, owner.Id, block.Action, block.Position);
            _world.Reveal(); return;
        }
        var sentry = owner.Tactics!.Resources.Sentry;
        if (sentry is null || sentry.Schedule != work.Sequence || sentry.Action != work.ReferenceId || sentry.Due != Clock)
            throw new InvalidDataException("Orphaned class effect.");
        sentry.Schedule = 0; sentry.Due = 0;
        if (sentry.Phase == SentryPhase.Impact)
        {
            ResolveAttack(SentryAttack(owner));
            sentry.Cells = []; sentry.Reflection = null;
            if (sentry.Charges > 0) ScheduleSentry(owner, SentryPhase.Waiting, 120);
            else sentry.Phase = SentryPhase.Spent;
            return;
        }
        sentry.Charges--;
        var target = _world.State.Actors.Where(a => a.Life == LifeState.Alive && a.Tactics?.Invulnerable != true
            && HostileTo(owner, a) && a.Position.Floor == sentry.Position.Floor
            && (a.Position.X == sentry.Position.X || a.Position.Y == sentry.Position.Y)
            && a.Position.Distance(sentry.Position) is >= 1 and <= 4)
            .OrderBy(a => a.Position.Distance(sentry.Position)).ThenBy(a => a.Id)
            .FirstOrDefault(a => SentryLine(sentry.Position, a.Position).All(ProjectileOpen));
        if (target != null)
        {
            var route = RouteMirrors(owner, sentry.Position, SentryLine(sentry.Position, target.Position),
                AttackDelivery.Projectile, ignoredBody: 0);
            sentry.Cells = route.Cells.ToArray(); sentry.Reflection = route.Reflection;
            ConsumeMirror(owner, sentry.Action, route.Reflection, route.Cells);
            ScheduleSentry(owner, SentryPhase.Impact, 80);
            Emit("SentryCommitted", owner.Id, target.Id, sentry.Action, sentry.Position, sentry.Charges);
        }
        else
        {
            Emit("SentryChargeSpent", owner.Id, owner.Id, sentry.Action, sentry.Position, sentry.Charges);
            if (sentry.Charges > 0) ScheduleSentry(owner, SentryPhase.Waiting, 200);
            else sentry.Phase = SentryPhase.Spent;
        }
    }

    private static WorldPosition[] SentryLine(WorldPosition from, WorldPosition to)
    {
        int dx = Math.Sign(to.X - from.X), dy = Math.Sign(to.Y - from.Y);
        return Enumerable.Range(1, from.Distance(to)).Select(i => from.Offset(dx * i, dy * i)).ToArray();
    }
    private LockedAttack SentryAttack(Actor owner)
    {
        var sentry = owner.Tactics!.Resources.Sentry!;
        return new(sentry.Action, owner.Id, sentry.Position, sentry.Cells.ToArray(), "core:ability/tinkerer_sentry",
            AttackDelivery.Projectile, DamageElement.Physical, 3, Clock, ClassGenerated: true,
            HitsOwner: true, PersistsWithoutOwner: true, Reflection: sentry.Reflection);
    }
    private IEnumerable<LockedAttack> SentryAttacks() => _world.State.Actors
        .Where(a => a.Tactics?.Resources.Sentry?.Phase == SentryPhase.Impact)
        .Select(a => SentryAttack(a) with { Due = a.Tactics!.Resources.Sentry!.Due });
}
