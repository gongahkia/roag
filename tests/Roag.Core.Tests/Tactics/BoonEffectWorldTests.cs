using System.Collections.Immutable;
using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static BoonActivation Activation(Arena arena, Actor owner, string id, int power, Actor? target = null,
        WorldPosition[]? cells = null, WorldPosition? from = null, int? price = null)
    {
        if (!owner.Tactics!.Boons.Inventory.Stacks.ContainsKey(id)) Grant(owner, id);
        long action = arena.Combat.State.NextAction++;
        var e = new BoonEvent { Id = arena.Combat.State.NextEvent++, ChainId = action, RootActionId = action,
            Kind = BoonTrigger.HitConfirmed, SourceId = owner.Id, TargetId = target?.Id,
            SourceFaction = arena.World.State.Party.Contains(owner.Id) ? "player" : "enemy",
            SourcePosition = owner.Position, Position = target?.Position ?? owner.Position, From = from,
            TargetCells = (cells ?? []).ToImmutableArray(), Price = price };
        return new(arena.Combat.State.BoonEffects.Dispatch.NextActivationId++, id, owner.Id, e.SourceFaction,
            owner.Position, action, action, e.Id, [new(owner.Id, id)], power, e);
    }
    private static BoonEffect Effect(string id, BoonEffectKind kind) => BoonCatalog.Default.Get(id).Effects.Single(e => e.Kind == kind);
    private static void Apply(Arena arena, BoonActivation activation, BoonEffectKind kind) =>
        arena.Combat.ExecuteBoonEffect(activation, Effect(activation.BoonId, kind));

    [Fact]
    public void BurningRefreshKeepsCommittedTickAndIntervalButUpdatesOwnerAndStrongerPower()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        var first = Activation(a, a.Hero, "core:boon/emberbrand", 3, a.Enemy);
        Apply(a, first, BoonEffectKind.ApplyStatus);
        var before = a.Enemy.Tactics!.Burning!;
        Assert.Equal(100, before.Due);
        var second = Activation(a, a.Friend, "core:boon/emberbrand", 2, a.Enemy);
        a.Combat.ExecuteBoonEffect(second, Effect(second.BoonId, BoonEffectKind.ApplyStatus) with { Interval = 400, Ticks = 2 });
        var after = a.Enemy.Tactics.Burning!;
        Assert.Equal(before.Schedule, after.Schedule); Assert.Equal(100, after.Due); Assert.Equal(100, after.Interval);
        Assert.Equal(3, after.Power); Assert.Equal(2, after.Remaining); Assert.Equal(a.Friend.Id, after.Activation.OwnerId);
        int hp = a.Enemy.Health;
        a.Enemy.Position = a.Enemy.Position with { Floor = 1 };
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(hp - 3, a.Enemy.Health); Assert.Equal(1, a.Enemy.Tactics.Burning!.Remaining);
        Assert.Equal(a.Friend.Id, a.Combat.State.Events.Last(e => e.Kind == "StatusTick").Source);
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(hp - 6, a.Enemy.Health); Assert.Null(a.Enemy.Tactics.Burning);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void BurningContinuesOnDownedMembersAndDeathCancelsItsPendingTick()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        var burn = Activation(a, a.Enemy, "core:boon/emberbrand", 2, a.Hero);
        Apply(a, burn, BoonEffectKind.ApplyStatus);
        a.Hero.Health = 1; a.Hit(a.Hero, 5);
        Assert.Equal(LifeState.Downed, a.Hero.Life);
        Assert.Same(a.Friend, a.Combat.AdvanceToDecision());
        for (int i = 0; i < 3; i++) { a.Act(TacticalAction.Wait); a.Next(a.Friend.Id); }
        Assert.Equal(300, a.Combat.Clock); Assert.Equal(LifeState.Dead, a.Hero.Life);
        Assert.Null(a.Hero.Tactics!.Burning);
        Assert.DoesNotContain(a.Combat.State.Queue.Snapshot().Pending, w => w.ActorId == a.Hero.Id);
        Assert.False(a.World.State.AwaitingContinuation);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void BurningBypassesDirectionalGuardAndHealingDoesNotReviveDownedMembers()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Enemy.Position = new(101, 100);
        Apply(a, Activation(a, a.Enemy, "core:boon/emberbrand", 4, a.Hero), BoonEffectKind.ApplyStatus);
        a.Act(TacticalAction.Guard); a.Next();
        Assert.Equal(a.Hero.MaxHealth - 4, a.Hero.Health);
        var healing = Activation(a, a.Hero, "core:boon/grave_dividend", 10);
        a.Hit(a.Hero, 100);
        Apply(a, healing, BoonEffectKind.HealOwner);
        Assert.Equal(LifeState.Downed, a.Hero.Life); Assert.Equal(0, a.Hero.Health); Assert.Equal(5, a.Hero.Tactics!.BleedTurns);
    }

    [Fact]
    public void EchoLocksItsOwnPatternSnapshotAndHitsCurrentOccupantsAfterOwnerMoves()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.Friend.Position = new(99, 100);
        var activation = Activation(a, a.Hero, "core:boon/backtrail_mantle", 3, from: new(99, 100), cells: [new(104, 100)]);
        Apply(a, activation, BoonEffectKind.Echo);
        var echo = Assert.Single(a.Combat.State.BoonEffects.Echoes).Value;
        Assert.Equal(new WorldPosition(104, 100), Assert.Single(activation.Event.TargetCells));
        Assert.Contains(new WorldPosition(99, 100), echo.Activation.Event.TargetCells);
        a.Hero.Position = new(105, 105, 1);
        int hp = a.Friend.Health;
        a.Act(TacticalAction.Wait); a.Next(); a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(hp - 3, a.Friend.Health);
        Assert.Empty(a.Combat.State.BoonEffects.Echoes);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void CleansingAndConsumingBurnCancelExactlyThatStatusAndExtensionIsCapped()
    {
        using var a = new Arena(ClassKind.Adventurer);
        Apply(a, Activation(a, a.Hero, "core:boon/emberbrand", 2, a.Enemy), BoonEffectKind.ApplyStatus);
        long schedule = a.Enemy.Tactics!.Burning!.Schedule;
        Apply(a, Activation(a, a.Hero, "core:boon/long_wick", 100, a.Enemy), BoonEffectKind.ExtendBurning);
        Assert.Equal(8, a.Enemy.Tactics.Burning!.Remaining); Assert.Equal(schedule, a.Enemy.Tactics.Burning.Schedule);
        int hp = a.Enemy.Health;
        Apply(a, Activation(a, a.Hero, "core:boon/cinder_stopper", 20, a.Enemy), BoonEffectKind.ConsumeBurning);
        Assert.Equal(hp - 16, a.Enemy.Health); Assert.Null(a.Enemy.Tactics.Burning);
        Assert.DoesNotContain(a.Combat.State.Queue.Snapshot().Pending, w => w.Sequence == schedule);
        Apply(a, Activation(a, a.Enemy, "core:boon/emberbrand", 2, a.Hero), BoonEffectKind.ApplyStatus);
        Apply(a, Activation(a, a.Hero, "core:boon/field_dressing", 2), BoonEffectKind.CleanseOwner);
        Assert.Null(a.Hero.Tactics!.Burning);
        Assert.DoesNotContain(a.Combat.State.Queue.Snapshot().Pending, w => w.Kind == ScheduledKind.Effect);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void MarksAreOwnedPerMemberAndExpireInclusivelyWithoutSharingStacks()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        int hp = a.Enemy.Health;
        var one = Activation(a, a.Hero, "core:boon/sunderers_mark", 3, a.Enemy);
        var two = Activation(a, a.Friend, "core:boon/sunderers_mark", 2, a.Enemy);
        Apply(a, one, BoonEffectKind.MarkDamage); Apply(a, two, BoonEffectKind.MarkDamage);
        Assert.Equal(hp, a.Enemy.Health);
        Apply(a, one, BoonEffectKind.MarkDamage); Assert.Equal(hp - 3, a.Enemy.Health);
        Apply(a, one, BoonEffectKind.MarkDamage); Assert.Equal(hp - 9, a.Enemy.Health);
        Assert.Equal(1, a.Friend.Tactics!.Boons.Memory[two.BoonId].MarkCount);
        for (int i = 0; i < 4; i++) { a.Act(TacticalAction.Wait); a.Next(); }
        Assert.Equal(400, a.Combat.Clock);
        Apply(a, one, BoonEffectKind.MarkDamage); Assert.Equal(hp - 15, a.Enemy.Health);
        for (int i = 0; i < 5; i++) { a.Act(TacticalAction.Wait); a.Next(); }
        Apply(a, one, BoonEffectKind.MarkDamage);
        Assert.Equal(hp - 15, a.Enemy.Health); Assert.Equal(1, a.Hero.Tactics!.Boons.Memory[one.BoonId].MarkCount);
    }

    [Fact]
    public void StunCancelsRegularEnemyWindupWithoutBringingLaterRecoveryForward()
    {
        using var a = new Arena(ClassKind.Adventurer, enemyDue: 0);
        a.Enemy.Tactics!.RegularEnemy = true;
        a.Act(TacticalAction.Wait);
        Assert.Same(a.Enemy, a.Combat.AdvanceToDecision());
        a.Combat.CommitWindup("test:windup", [a.Hero.Position], AttackDelivery.Area, DamageElement.Fire, 3, 50, 500);
        var activation = Activation(a, a.Hero, "core:boon/hourglass_clasp", 100, a.Enemy);
        Apply(a, activation, BoonEffectKind.StunTarget);
        Assert.Empty(a.Combat.State.Attacks);
        Assert.Equal(550, a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Enemy.Id).Due);
        Assert.Equal(100, a.Enemy.Tactics.StunnedUntil);
        Assert.Equal(400, a.Hero.Tactics!.Boons.Memory[activation.BoonId].ExpiresAt);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
        a.Next(); Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        a.Enemy.Tactics.RegularEnemy = false;
        var old = a.Combat.State.Queue.Snapshot().Pending.Single(w => w.ActorId == a.Enemy.Id);
        Apply(a, activation, BoonEffectKind.StunTarget);
        Assert.Contains(old, a.Combat.State.Queue.Snapshot().Pending);
    }

    [Fact]
    public void DisplacementRequiresRegularCardinalTargetAndBlockedPushDealsCollisionDamage()
    {
        using var a = new Arena(ClassKind.Adventurer);
        a.Enemy.Tactics!.RegularEnemy = true;
        Apply(a, Activation(a, a.Hero, "core:boon/grasping_hand", 2, a.Enemy), BoonEffectKind.DisplaceTarget);
        Assert.Equal(new WorldPosition(102, 100), a.Enemy.Position);
        var arrival = a.Combat.State.Events.Last(e => e.Kind == "TileEntered");
        Assert.True(arrival.Forced); Assert.Equal(new WorldPosition(103, 100), arrival.From);
        a.World.Chunks.Edit(new(103, 100), Material.Wall);
        int hp = a.Enemy.Health;
        Apply(a, Activation(a, a.Hero, "core:boon/anchor_maul", 4, a.Enemy), BoonEffectKind.DisplaceTarget);
        Assert.Equal(hp - 4, a.Enemy.Health);
        a.Enemy.Position = new(102, 102);
        Apply(a, Activation(a, a.Hero, "core:boon/grasping_hand", 2, a.Enemy), BoonEffectKind.DisplaceTarget);
        Assert.Equal(new WorldPosition(102, 102), a.Enemy.Position);
        a.Enemy.Position = new(102, 100); a.Enemy.Tactics.RewardExempt = true;
        Apply(a, Activation(a, a.Hero, "core:boon/grasping_hand", 2, a.Enemy), BoonEffectKind.DisplaceTarget);
        Assert.Equal(new WorldPosition(102, 100), a.Enemy.Position);
    }

    [Fact]
    public void WardHealingSpendsOneHitWardsInCatalogOrderAndRefundUsesSharedGoldWithoutPickupEvent()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        Grant(a.Hero, "core:boon/betrayal_ward"); Grant(a.Hero, "core:boon/beggars_cap");
        a.Hero.Tactics!.Boons.ApplyTimed("core:boon/betrayal_ward", BoonEffectKind.Ward, 2, 100, 0);
        a.Hero.Tactics.Boons.ApplyTimed("core:boon/beggars_cap", BoonEffectKind.Ward, 7, 100, 0);
        a.Hero.Health -= 3;
        Apply(a, Activation(a, a.Hero, "core:boon/ward_ledger", 3), BoonEffectKind.WardToHeal);
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health); Assert.Equal(0, a.Hero.Tactics.Boons.WardTotal(0));
        int gold = a.World.State.Inventory["gold"];
        Apply(a, Activation(a, a.Friend, "core:boon/brokers_seal", 5, price: 3), BoonEffectKind.RefundPurchase);
        Assert.Equal(gold + 3, a.World.State.Inventory["gold"]);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "CurrencyCollected");
        Assert.Equal(a.Friend.Id, a.Combat.State.Events.Last(e => e.Kind == "CurrencyRefunded").Source);
    }

    [Fact]
    public void PendingBurnAndEchoResumeExactlyInWorldSaveAndRejectMissingWork()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        Apply(a, Activation(a, a.Hero, "core:boon/emberbrand", 2, a.Enemy), BoonEffectKind.ApplyStatus);
        Apply(a, Activation(a, a.Friend, "core:boon/temporal_echo", 3, a.Enemy, cells: [a.Enemy.Position]), BoonEffectKind.Echo);
        a.Enemy.Position = a.Enemy.Position with { Floor = 1 };
        string directory = Path.Combine(Path.GetTempPath(), "roag-boon-effect-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(directory, "world.json"); a.World.Save(slot);
            using var resumed = WorldSession.Load(slot);
            var combat = new TacticalCombat(resumed, _ => { });
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(combat.State));
            for (int i = 0; i < 8; i++)
            {
                var original = a.Combat.AdvanceToDecision(); var restored = combat.AdvanceToDecision();
                Assert.Equal(original!.Id, restored!.Id);
                Assert.True(a.Combat.Commit(new(TacticalAction.Wait)).Success); Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
            }
            Assert.Equal(JsonSerializer.Serialize(a.Combat.State), JsonSerializer.Serialize(combat.State));
            Assert.Equal(a.Enemy.Health, resumed.State.Actors.Single(actor => actor.Id == a.Enemy.Id).Health);
            TacticalValidation.Validate(resumed.Plan, resumed.State);
            Apply(a, Activation(a, a.Hero, "core:boon/emberbrand", 2, a.Enemy), BoonEffectKind.ApplyStatus);
            a.Combat.State.Queue.Cancel(a.Enemy.Tactics!.Burning!.Schedule);
            Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(a.World.Plan, a.World.State));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void ExistingBurnIsNotStoppedByNewOpaqueTerrainOnTheVictimsTile()
    {
        using var a = new Arena(ClassKind.Adventurer);
        Apply(a, Activation(a, a.Hero, "core:boon/emberbrand", 3, a.Enemy), BoonEffectKind.ApplyStatus);
        a.World.Chunks.Edit(a.Enemy.Position, Material.Wall);
        int hp = a.Enemy.Health;
        a.Act(TacticalAction.Wait); a.Next();
        Assert.Equal(hp - 3, a.Enemy.Health);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Fact]
    public void HealingStartsCooldownOnlyOnActualGainAndConsumesQueuedPrimeEvenAfterExpiry()
    {
        using var a = new Arena(ClassKind.Adventurer);
        var lullaby = Activation(a, a.Hero, "core:boon/mist_lullaby", 3);
        Apply(a, lullaby, BoonEffectKind.HealOwner);
        Assert.False(a.Hero.Tactics!.Boons.Memory.ContainsKey(lullaby.BoonId));
        a.Hero.Health -= 1;
        Apply(a, lullaby, BoonEffectKind.HealOwner);
        Assert.Equal(a.Hero.MaxHealth, a.Hero.Health);
        Assert.Equal(400, a.Hero.Tactics.Boons.Memory[lullaby.BoonId].ExpiresAt);
        var reserve = Activation(a, a.Hero, "core:boon/ration_reserve", 3);
        a.Hero.Tactics.Boons.ApplyTimed(reserve.BoonId, BoonEffectKind.Prime, 3, 1, 0);
        a.Act(TacticalAction.Wait); a.Next();
        Apply(a, reserve, BoonEffectKind.HealOwner);
        Assert.Equal(0, a.Hero.Tactics.Boons.Memory[reserve.BoonId].Charges);
    }

    [Fact]
    public void ExcessHealingAndExcessHungerBothGrantTheCappedWard()
    {
        using var a = new Arena(ClassKind.Adventurer);
        var definition = BoonCatalog.Default.Definitions.Single(d => d.Effects.Any(e => e.Scale == BoonScale.SupplyExcess));
        var effect = definition.Effects.Single(e => e.Scale == BoonScale.SupplyExcess);
        var original = Activation(a, a.Hero, definition.Id, 8);
        var healing = original with { Event = original.Event with { SupplyKind = "health", Amount = 10, ActualGain = 7 } };
        a.Combat.ExecuteBoonEffect(healing, effect);
        Assert.Equal(3, a.Hero.Tactics!.Boons.WardTotal(0));
        var food = original with { Event = original.Event with { SupplyKind = "food", Amount = 10, ActualGain = 1 } };
        a.Combat.ExecuteBoonEffect(food, effect);
        Assert.Equal(8, a.Hero.Tactics.Boons.WardTotal(0));
        var fractional = food with { Event = food.Event with { ActualGain = 3.25 } };
        a.Hero.Tactics.Boons.Memory.Clear();
        a.Combat.ExecuteBoonEffect(fractional, effect);
        Assert.Equal(6, a.Hero.Tactics.Boons.WardTotal(0));
    }

    [Fact]
    public void DelayedEffectsRetainTheirDeadTargetIdentityOnlyWhileReferenced()
    {
        using var a = new Arena(ClassKind.Adventurer);
        var activation = Activation(a, a.Hero, "core:boon/temporal_echo", 2, a.Enemy, cells: [a.Enemy.Position]);
        Apply(a, activation, BoonEffectKind.Echo);
        a.Enemy.Health = 0; a.Enemy.Life = LifeState.Dead;
        a.Combat.State.Queue.CancelActor(a.Enemy.Id);
        Assert.True(TacticalReferences.RetainsActor(a.World.State, a.Enemy.Id));
        a.Act(TacticalAction.Wait); a.Next(); a.Act(TacticalAction.Wait); a.Next();
        Assert.Empty(a.Combat.State.BoonEffects.Echoes);
        Assert.False(TacticalReferences.RetainsActor(a.World.State, a.Enemy.Id));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }
}
