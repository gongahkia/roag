using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonMedicineTests
{
    [Fact]
    public void MedicineUsesTheHealersGearAndRecoveryButTheRecipientOwnsOverflowAndTheirReadiness()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var healer = world.Party.Single();
        const string gear = "gear.999.0.0";
        world.State.GeneratedEquipment.Add(gear, new("item.heartstone", ["affix.mending"])); world.State.Inventory[gear] = 1;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Equip, Item: gear)).Success);
        var recipient = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 1);
        recipient.Position = healer.Position.Offset(1, 0); world.Chunks.Edit(recipient.Position, Material.Floor);
        foreach (var member in world.Party)
            member.Tactics!.Boons.Inventory = member.Tactics.Boons.Inventory.Grant("core:boon/overflow_chalice", BoonRarity.Common, member.Tactics.Build!.Class);
        var enemy = world.State.Actors.First(a => a.Tactics?.Enemy != null && a.Position.Floor == 1);
        world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, enemy.Id, enemy.Position, [recipient.Position],
            "test:medical-debt", AttackDelivery.Area, DamageElement.Physical, 12, world.DungeonCombat.Clock));
        var ready = world.State.Tactics.Timeline.Pending.Single(w => w.ActorId == recipient.Id && w.Kind == ScheduledKind.ActorReady);
        int medicine = world.Count("medicine"), hunger = (int)recipient.Body.Hunger;
        var intent = new TacticalIntent(TacticalAction.UseMedicine, Target: recipient.Id);
        string before = JsonSerializer.Serialize(world.State); var preview = world.DungeonCombat.Preview(intent);
        Assert.True(preview.Valid); Assert.Equal(100, preview.Recovery); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(world.DungeonCombat.Commit(intent).Success);
        Assert.Equal(medicine - 1, world.Count("medicine")); Assert.Equal(recipient.MaxHealth, recipient.Health);
        Assert.Contains(ready, world.State.Tactics.Timeline.Pending); Assert.Equal(hunger, recipient.Body.Hunger);
        Assert.Equal(2, recipient.Tactics!.Boons.WardTotal(world.DungeonCombat.Clock)); Assert.Equal(0, healer.Tactics!.Boons.WardTotal(world.DungeonCombat.Clock));
        var collected = Assert.Single(world.State.Tactics.Events, e => e.Kind == "SupplyCollected" && e.Ability == "ability.medicine");
        Assert.Equal(27, collected.Amount); Assert.Equal(recipient.Id, collected.Source);
        Assert.Equal(12, world.State.Tactics.Rewards.Enemies[enemy.Id].Members[healer.Id].Healing);
        Assert.Empty(world.State.Tactics.Rewards.Enemies[enemy.Id].UnhealedDamage);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData("healthy")] [InlineData("empty")] [InlineData("remote")] [InlineData("downed")]
    public void MedicineRejectsInvalidRecipientsFreelyAndNeverReplacesQuarterHealthRevival(string invalid)
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true);
        var other = DungeonPartyTests.AddMember(world, ClassKind.Ranger, invalid == "remote" ? 2 : 1);
        var hero = world.Party.First();
        other.Position = invalid == "remote" ? other.Position : hero.Position.Offset(1, 0);
        world.Chunks.Edit(other.Position, Material.Floor);
        if (invalid != "healthy") other.Health -= 4;
        if (invalid == "empty") world.State.Inventory["medicine"] = 0;
        if (invalid == "downed") DungeonAbandonmentTests.Down(world, other);
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.UseMedicine, Target: other.Id)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
}
