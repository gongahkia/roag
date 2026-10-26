using System.Collections.Immutable;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    // Original player owners precede enemies. Persistent world storage order and leader
    // selection must not change proc order or random consumption; party ties use identity.
    private IReadOnlyList<BoonActorView> BoonBodies() => _world.State.Actors.Where(a => State.Participants.Contains(a.Id) && a.Life != LifeState.Dead)
        .OrderBy(a => Friendly(a) ? 0 : 1).ThenBy(a => a.Id)
        .Select(a => new BoonActorView(a, BoonFaction(a), RegularBoonTarget(a),
            State.Attacks.Where(w => w.Source == a.Id && !w.ClassGenerated).SelectMany(w => w.Cells).ToArray())).ToArray();
    private bool RegularBoonTarget(Actor actor) => !Friendly(actor)
        && actor.Tactics is { RegularEnemy: true, Invulnerable: false, RewardExempt: false };

    /// <summary>World-backed effect handler. The causal event pump calls this only after dispatching queued events.</summary>
    internal void ExecuteBoonEffect(BoonActivation activation, BoonEffect effect)
    {
        activation.Validate(State.BoonEffects.Dispatch.NextActivationId);
        BoonDefinitionValidation.ValidateEffect(effect);
        var owner = Actor(activation.OwnerId);
        int power = checked((int)activation.Power);
        var target = activation.Event.TargetId is { } targetId ? _world.State.Actors.FirstOrDefault(a => a.Id == targetId) : null;
        bool livingOwner = owner.Life == LifeState.Alive && owner.Health > 0;
        switch (effect.Kind)
        {
            case BoonEffectKind.EnrichDrop:
                EnrichLoot(owner, activation, power);
                return;
            case BoonEffectKind.GatherPickups:
                GatherLoot(owner, activation, power);
                return;
            case BoonEffectKind.Echo:
                var locked = effect.Pattern is { } pattern ? activation with { Event = activation.Event with
                {
                    TargetCells = BoonEffectTargets.Select(effect with { Kind = BoonEffectKind.Pattern, Pattern = pattern },
                        activation, BoonBodies(), ProjectileOpen).ToImmutableArray()
                } } : activation;
                long due = checked(Clock + effect.Delay!.Value);
                var work = State.Queue.Schedule(ScheduledKind.Effect, due, owner.Id, referenceId: activation.RootActionId ?? 0);
                State.BoonEffects.Echoes.Add(work.Sequence, new(work.Sequence, due, locked, effect.DamageType ?? DamageElement.Physical, effect.ProcCoefficient ?? 1));
                Emit("DelayedEffectScheduled", owner.Id, 0, activation.RootActionId ?? 0, activation.SourcePosition, ability: activation.BoonId);
                return;
            case BoonEffectKind.Prime:
            case BoonEffectKind.SpeedBoost:
            case BoonEffectKind.Ward:
                if (!livingOwner) return;
                if (effect.Scale == BoonScale.SupplyExcess)
                {
                    if (activation.Event.SupplyKind == "food") throw new InvalidOperationException("Overflow Chalice's food conversion awaits the recorded gameplay decision.");
                    power = Math.Min(power, Math.Max(0, (activation.Event.Amount ?? 0) - (activation.Event.ActualGain ?? 0)));
                }
                owner.Tactics!.Boons.ApplyTimed(activation.BoonId, effect.Kind, power, effect.Duration!.Value, Clock);
                Emit("BoonStateApplied", owner.Id, owner.Id, activation.RootActionId ?? 0, owner.Position, power, ability: activation.BoonId);
                return;
            case BoonEffectKind.HealOwner:
                if (!livingOwner) return;
                ConsumeEffectPrime(owner, activation, effect);
                int gain = HealBoonOwner(owner, activation, power);
                if (gain > 0 && effect.Cooldown is { } cooldown)
                    owner.Tactics!.Boons.Memory[activation.BoonId] = new(ExpiresAt: checked(Clock + cooldown));
                return;
            case BoonEffectKind.WardToHeal:
                if (!livingOwner || owner.Health == owner.MaxHealth) return;
                int absorbed = owner.Tactics!.Boons.ConsumeWards(Math.Min(power, owner.MaxHealth - owner.Health), Clock).Sum(w => w.Amount);
                if (absorbed > 0) HealBoonOwner(owner, activation, absorbed);
                return;
            case BoonEffectKind.MarkDamage:
                if (!livingOwner || target is not { Health: > 0 }) return;
                var memory = owner.Tactics!.Boons.Memory.GetValueOrDefault(activation.BoonId, new());
                int count = memory.MarkTarget != target.Id || memory.ExpiresAt == null || memory.ExpiresAt < Clock ? 0 : memory.MarkCount ?? 0;
                count = Math.Min(effect.Cap!.Value, count + 1);
                owner.Tactics.Boons.Memory[activation.BoonId] = memory with { MarkTarget = target.Id, MarkCount = count, ExpiresAt = checked(Clock + effect.Duration!.Value) };
                Emit("BoonStateApplied", owner.Id, target.Id, activation.RootActionId ?? 0, target.Position, count, ability: activation.BoonId);
                if (count >= effect.Threshold) ApplyBoonDamage(activation, effect, [target.Position], checked(power * (count - effect.Threshold.Value + 1)));
                return;
            case BoonEffectKind.ApplyStatus:
                if (effect.Target == BoonTarget.Nearest)
                {
                    var cells = BoonEffectTargets.Select(effect with { Kind = BoonEffectKind.DamageNearest }, activation, BoonBodies(), ProjectileOpen);
                    target = cells.Count > 0 ? BodyAt(cells[0], owner.Id) : null;
                }
                if (target is not { Health: > 0, Tactics: not null }) return;
                ApplyBurning(target, activation, power, effect.Interval!.Value, effect.Ticks!.Value);
                return;
            case BoonEffectKind.CleanseOwner:
                if (livingOwner) RemoveBurning(owner, activation);
                return;
            case BoonEffectKind.ConsumeBurning:
                if (target is not { Health: > 0, Tactics.Burning: { } burn }) return;
                RemoveBurning(target, activation);
                ApplyBoonDamage(activation, effect, [target.Position], (int)Math.Min(power, (long)burn.Power * burn.Remaining));
                return;
            case BoonEffectKind.ExtendBurning:
                if (target is not { Health: > 0, Tactics.Burning: { } burning }) return;
                int remaining = (int)Math.Min(effect.Cap!.Value, (long)burning.Remaining + power);
                target.Tactics.Burning = burning with { Remaining = remaining };
                Emit("BoonStateApplied", owner.Id, target.Id, activation.RootActionId ?? 0, target.Position, remaining, ability: activation.BoonId);
                return;
            case BoonEffectKind.DisplaceTarget:
                if (target is null || !RegularBoonTarget(target) || target.Life != LifeState.Alive
                    || !BoonGeometry.Axis(target.Position, owner.Position, BoonAxis.Cardinal)) return;
                int dx = Math.Sign(target.Position.X - owner.Position.X), dy = Math.Sign(target.Position.Y - owner.Position.Y);
                if (effect.Mode == BoonDisplacement.Pull) { dx = -dx; dy = -dy; }
                var next = target.Position.Offset(dx, dy);
                if (!_world.CanEnter(next, target.Id))
                {
                    if (effect.CollisionDamage == true) ApplyBoonDamage(activation, effect, [target.Position], power);
                    return;
                }
                int? interruptedRecovery = State.Attacks.FirstOrDefault(a => a.Source == target.Id && a.RecoveryCost != null)?.RecoveryCost;
                if (interruptedRecovery != null) CancelAttacks(target.Id, owner.Id, activation.RootActionId ?? 0, "displacement");
                var from = target.Position; _world.MoveTacticalActor(target, next);
                RecordCombatControl(owner.Id, target, ActionTiming.Recovery(target.Tactics!.MoveCost, EffectiveSpeed(target)));
                Emit("Displaced", owner.Id, target.Id, activation.RootActionId ?? 0, next, forced: true, from: from);
                Emit("TileEntered", target.Id, target.Id, activation.RootActionId ?? 0, next, forced: true, from: from);
                CollectLootAt(target, next, activation.RootActionId ?? 0);
                if (interruptedRecovery is { } cost) ScheduleReady(target, cost);
                if (_resolutionDepth == 0 && !_drainingBoons) TriggerSnares(target, activation.RootActionId ?? 0);
                return;
            case BoonEffectKind.StunTarget:
                if (target is null || !RegularBoonTarget(target) || target.Life != LifeState.Alive) return;
                long until = Math.Max(target.Tactics!.StunnedUntil, checked(Clock + Math.Min(effect.Cap!.Value, power)));
                long previousReady = State.Queue.Snapshot().Pending.Where(w => w.ActorId == target.Id && w.Kind == ScheduledKind.ActorReady)
                    .Select(w => w.Due).DefaultIfEmpty(Clock).Max();
                bool interrupted = State.Attacks.Any(a => a.Source == target.Id && !a.ClassGenerated);
                long ready = State.Queue.Snapshot().Pending.Where(w => w.ActorId == target.Id && w.Kind == ScheduledKind.ActorReady)
                    .Select(w => w.Due).Append(until).Max();
                RecordCombatControl(owner.Id, target, Math.Max(ready - previousReady,
                    interrupted ? ActionTiming.Recovery(target.Tactics.MoveCost, EffectiveSpeed(target)) : 0));
                CancelAttacks(target.Id, owner.Id, activation.RootActionId ?? 0, "stun"); State.Queue.CancelActor(target.Id, ScheduledKind.ActorReady);
                if (State.ActiveActor == target.Id) State.ActiveActor = 0;
                target.Tactics.StunnedUntil = until;
                State.Queue.Schedule(ScheduledKind.ActorReady, ready, target.Id, Friendly(target));
                owner.Tactics!.Boons.Memory[activation.BoonId] = new(ExpiresAt: checked(Clock + effect.Cooldown!.Value));
                Emit("Stunned", owner.Id, target.Id, activation.RootActionId ?? 0, target.Position, power, ability: activation.BoonId);
                return;
            case BoonEffectKind.RefundPurchase:
                if (!livingOwner || !Friendly(owner)) return;
                int refund = Math.Min(power, activation.Event.Price ?? 0);
                if (refund <= 0) return;
                _world.State.Inventory["gold"] = checked(_world.State.Inventory.GetValueOrDefault("gold") + refund);
                Emit("CurrencyRefunded", owner.Id, owner.Id, activation.RootActionId ?? 0, owner.Position, refund, ability: activation.BoonId);
                return;
            case BoonEffectKind.DamageNearest:
            case BoonEffectKind.DamageSource:
            case BoonEffectKind.DamageTarget:
            case BoonEffectKind.Area:
            case BoonEffectKind.AdjacentHostiles:
            case BoonEffectKind.PathAdjacentHostiles:
            case BoonEffectKind.StatusNeighbors:
            case BoonEffectKind.Pattern:
                var selected = BoonEffectTargets.Select(effect, activation, BoonBodies(), ProjectileOpen);
                ConsumeEffectPrime(owner, activation, effect);
                if (effect.Scale == BoonScale.AdjacentHostiles) power = checked(power * (activation.Event.AdjacentHostileCount ?? 0));
                ApplyBoonDamage(activation, effect, selected, power);
                return;
            default: throw new ArgumentOutOfRangeException(nameof(effect), "Unknown boon effect kind.");
        }
    }

    private static void ConsumeEffectPrime(Actor owner, BoonActivation activation, BoonEffect effect)
    {
        // Original effects consume a queued prime even if it expires between trigger and execution.
        if (effect.ConsumePrime == true && owner.Tactics?.Boons.Memory.TryGetValue(activation.BoonId, out var memory) == true)
            owner.Tactics.Boons.Memory[activation.BoonId] = memory with { Charges = 0 };
    }

    private int HealBoonOwner(Actor owner, BoonActivation activation, int requested)
    {
        int gain = Math.Min(requested, owner.MaxHealth - owner.Health);
        owner.Health += gain;
        RecordCombatHealing(owner.Id, owner.Id, gain);
        Emit("Healed", owner.Id, owner.Id, activation.RootActionId ?? 0, owner.Position, gain, ability: activation.BoonId);
        return gain;
    }

    private void ApplyBoonDamage(BoonActivation activation, BoonEffect effect, IReadOnlyList<WorldPosition> cells, int power,
        DamageCause cause = DamageCause.TriggeredEffect)
    {
        if (cells.Count == 0) return;
        var owner = Actor(activation.OwnerId);
        var origin = owner.Life == LifeState.Dead ? activation.SourcePosition : owner.Position;
        WithBoonCause(BoonCause.From(activation, effect.ProcCoefficient ?? 1), () => ResolveDamageBatch(new(activation.RootActionId ?? 0, activation.OwnerId, origin,
            cells.ToArray(), activation.BoonId, AttackDelivery.Area, effect.DamageType ?? DamageElement.Physical,
            power, Clock, cause, PersistsWithoutOwner: true, IgnoresTerrain: true)));
    }

    private void ApplyBurning(Actor target, BoonActivation activation, int power, int interval, int ticks)
    {
        var existing = target.Tactics!.Burning;
        if (existing != null)
            target.Tactics.Burning = existing with { Power = Math.Max(existing.Power, power), Remaining = ticks, Activation = activation };
        else
        {
            long due = checked(Clock + interval);
            var work = State.Queue.Schedule(ScheduledKind.Effect, due, target.Id, referenceId: 0);
            target.Tactics.Burning = new(power, ticks, interval, due, work.Sequence, activation);
        }
        Emit("StatusApplied", activation.OwnerId, target.Id, activation.RootActionId ?? 0, target.Position,
            target.Tactics.Burning.Power, ability: activation.BoonId);
    }

    private void RemoveBurning(Actor target, BoonActivation? activation = null)
    {
        if (target.Tactics?.Burning is not { } burn) return;
        State.Queue.Cancel(burn.Schedule); target.Tactics.Burning = null;
        if (activation != null) Emit("StatusCleansed", activation.OwnerId, target.Id, activation.RootActionId ?? 0, target.Position, ability: activation.BoonId);
    }

    private bool ResolveBoonWork(ScheduledAction work)
    {
        if (State.BoonEffects.Echoes.Remove(work.Sequence, out var echo))
        {
            ResolveAtomically(BoonCause.From(echo.Activation, echo.ProcCoefficient), () =>
            {
                Emit("DelayedEffectResolved", echo.Activation.OwnerId, 0, echo.Activation.RootActionId ?? 0,
                    echo.Activation.SourcePosition, ability: echo.Activation.BoonId);
                ApplyBoonDamage(echo.Activation, new() { Kind = BoonEffectKind.Area, DamageType = echo.Element,
                    ProcCoefficient = echo.ProcCoefficient }, echo.Activation.Event.TargetCells, checked((int)echo.Activation.Power));
            });
            return true;
        }
        var target = Actor(work.ActorId);
        if (target.Tactics?.Burning is not { } burn || burn.Schedule != work.Sequence) return false;
        ResolveAtomically(BoonCause.From(burn.Activation), () =>
        {
            Emit("StatusTick", burn.Activation.OwnerId, target.Id, burn.Activation.RootActionId ?? 0, target.Position, burn.Power, ability: burn.Activation.BoonId);
            ApplyBoonDamage(burn.Activation, new() { Kind = BoonEffectKind.DamageTarget, DamageType = DamageElement.Fire },
                [target.Position], burn.Power, DamageCause.OngoingEffect);
            if (target.Life != LifeState.Dead && burn.Remaining > 1)
            {
                long due = checked(Clock + burn.Interval);
                var next = State.Queue.Schedule(ScheduledKind.Effect, due, target.Id);
                target.Tactics.Burning = burn with { Remaining = burn.Remaining - 1, Due = due, Schedule = next.Sequence };
            }
            else target.Tactics.Burning = null;
        });
        return true;
    }
}
