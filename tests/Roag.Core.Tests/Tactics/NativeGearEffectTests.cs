using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class ClassActionTests
{
    private static void NativeGear(Arena a, string item)
    {
        a.World.State.Inventory[item] = 1;
        Assert.True(a.Combat.Commit(new(TacticalAction.Equip, Item: item)).Success);
        a.Next();
    }

    [Fact]
    public void NativeGearKindledAddsFirePowerInTheRealPreviewAndHitWithoutChangingOtherElements()
    {
        using var a = new Arena(ClassKind.Arcanist);
        NativeGear(a, "item.ember-staff");
        a.Enemy.Position = new(102, 100);
        var basic = a.Combat.Preview(new(TacticalAction.BasicAttack, Facing.East));
        Assert.Equal(a.Hero.Power, basic.Damage);
        int health = a.Enemy.Health;
        var fire = a.Combat.Preview(new(TacticalAction.Special, Facing.East));
        int expected = a.Hero.Tactics!.Stats.SpecialDamage + 4 + 3;
        Assert.Equal(expected, fire.Damage);
        Assert.True(a.Combat.Commit(new(TacticalAction.Special, Facing.East)).Success);
        Assert.Equal(health - expected, a.Enemy.Health);
        Assert.Contains("ongoing burn damage is unchanged", a.World.EquipmentDescription(a.World.Item("item.ember-staff")));
        Assert.Null(a.Enemy.Tactics!.Burning);
    }

    [Fact]
    public void NativeGearCoverCreditsTheWearerOnceAndOnlyTheirOwnReadinessRefreshesIt()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        a.World.State.Inventory["item.wardens-shield"] = 1;
        Assert.True(a.Combat.Commit(new(TacticalAction.Equip, Item: "item.wardens-shield")).Success);
        var ready = a.Combat.State.Timeline.Pending.Single(w => w.ActorId == a.Hero.Id);
        a.Combat.State.Queue.Delay(ready.Sequence, 200);
        a.Next(a.Friend.Id); a.Friend.Health = a.Friend.MaxHealth / 2;
        int health = a.Friend.Health;
        a.Hit(a.Friend, 6);
        Assert.Equal(health - 3, a.Friend.Health); Assert.True(a.Hero.Tactics!.EquipmentCoverUsed);
        var ledger = a.Combat.State.Rewards.Enemies[a.Enemy.Id];
        Assert.Equal(3, ledger.Members[a.Hero.Id].Protection);
        Assert.False(ledger.Members.ContainsKey(a.Friend.Id));
        a.Act(TacticalAction.Wait); a.Next(a.Friend.Id);
        Assert.True(a.Hero.Tactics.EquipmentCoverUsed);
        a.Hit(a.Friend, 2); Assert.Equal(health - 5, a.Friend.Health);
        Assert.Single(a.Combat.State.Events, e => e.Kind == "AllyCovered");
        a.Act(TacticalAction.Wait); a.Next();
        Assert.False(a.Hero.Tactics.EquipmentCoverUsed);
        a.Hit(a.Friend, 4); Assert.Equal(health - 6, a.Friend.Health);
        Assert.Equal(6, ledger.Members[a.Hero.Id].Protection);
        Assert.Contains("own turn", a.World.EquipmentDescription(a.World.Item("item.wardens-shield")));
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }

    [Theory]
    [InlineData("wall")] [InlineData("floor")] [InlineData("range")] [InlineData("stun")]
    [InlineData("healthy")] [InlineData("ongoing")] [InlineData("one")]
    public void NativeGearCoverRequiresAnAvailableWearerAndAPhysicalWoundedAlly(string unavailable)
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        NativeGear(a, "item.wardens-shield"); a.Friend.Health = a.Friend.MaxHealth / 2;
        if (unavailable == "wall") a.World.Chunks.Edit(new(100, 101), Material.Wall);
        if (unavailable == "floor") a.Friend.Position = new(100, 102, 1);
        if (unavailable == "range") a.Friend.Position = new(100, 103);
        if (unavailable == "stun") a.Hero.Tactics!.StunnedUntil = a.Combat.Clock + 50;
        if (unavailable == "healthy") a.Friend.Health = a.Friend.MaxHealth;
        int damage = unavailable == "one" ? 1 : 4, health = a.Friend.Health;
        a.Combat.ResolveAttack(new(a.Combat.State.NextAction++, a.Enemy.Id, a.Friend.Position,
            [a.Friend.Position], "test:cover", AttackDelivery.Area, DamageElement.Fire, damage, a.Combat.Clock,
            unavailable == "ongoing" ? DamageCause.OngoingEffect : DamageCause.DirectAttack));
        Assert.Equal(health - damage, a.Friend.Health); Assert.False(a.Hero.Tactics!.EquipmentCoverUsed);
        Assert.DoesNotContain(a.Combat.State.Events, e => e.Kind == "AllyCovered");
    }

    [Fact]
    public void NativeGearMendingAffectsCooperativeHealingAndUsedCoverRoundTripsWithoutRerolling()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        const string gear = "gear.999.0.0";
        a.World.State.GeneratedEquipment.Add(gear, new("item.heartstone", ["affix.mending"]));
        NativeGear(a, gear); NativeGear(a, "item.wardens-shield");
        a.Friend.Health = a.Friend.MaxHealth / 2; a.Hit(a.Friend, 6);
        int health = a.Friend.Health;
        Assert.True(a.Combat.Commit(new(TacticalAction.Cooperate, Target: a.Friend.Id, Choice: 2)).Success);
        Assert.Equal(health + 9, a.Friend.Health); Assert.True(a.Hero.Tactics!.EquipmentCoverUsed);
        string root = Path.Combine(Path.GetTempPath(), "roag-native-gear-effects-" + Guid.NewGuid().ToString("N"));
        try
        {
            a.World.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(a.World.State), JsonSerializer.Serialize(loaded.State));
            Assert.True(loaded.Party.First().Tactics!.EquipmentCoverUsed);
            Assert.Equal(a.World.Item(gear).Effects, loaded.Item(gear).Effects);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void NativeGearCoverDoesNotSpendAgainstAFullWardOrClaimIneffectiveOverkillProtection()
    {
        using var a = new Arena(ClassKind.Adventurer, ClassKind.Adventurer);
        NativeGear(a, "item.wardens-shield");
        a.Friend.Health = 1; a.Friend.Tactics!.Supplies.WardActions = 1;
        a.Hit(a.Friend, 20);
        Assert.Equal(1, a.Friend.Health); Assert.False(a.Hero.Tactics!.EquipmentCoverUsed);
        var ledger = a.Combat.State.Rewards.Enemies[a.Enemy.Id];
        Assert.Equal(1, ledger.Members[a.Friend.Id].Protection);
        Assert.False(ledger.Members.ContainsKey(a.Hero.Id));
        a.Hit(a.Friend, 20);
        Assert.Equal(LifeState.Downed, a.Friend.Life); Assert.True(a.Hero.Tactics.EquipmentCoverUsed);
        Assert.False(ledger.Members.ContainsKey(a.Hero.Id));
        Assert.Single(a.Combat.State.Events, e => e.Kind == "AllyCovered" && e.Amount == 3);
        TacticalValidation.Validate(a.World.Plan, a.World.State);
    }
}
