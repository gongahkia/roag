using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonEquipmentTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ChangingGearPaysPreChangeSpeedInAndOutOfLocalCombatAndLeavesOtherWorkAlone(bool engaged)
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var hero = world.Party.Single(); var friend = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 2);
        if (engaged)
        {
            var enemy = world.State.Actors.First(a => a.Tactics?.Enemy is { Dormant: false });
            enemy.Position = hero.Position.Offset(1, 0);
            world.Chunks.Edit(enemy.Position, Material.Floor);
            world.Reveal();
        }
        Assert.Equal(engaged, world.IsActorInCombat(hero.Id));
        world.State.Inventory["item.quick-silver"] = 1;
        var intent = new TacticalIntent(TacticalAction.Equip, Item: "item.quick-silver");
        long cost = ActionTiming.Recovery(world.DungeonCombat.Tuning.EquipmentCost, hero.Speed);
        string others = JsonSerializer.Serialize(world.State.Tactics!.Timeline.Pending.Where(w => w.ActorId != hero.Id));
        string before = JsonSerializer.Serialize(world.State);
        var preview = world.DungeonCombat.Preview(intent);
        Assert.True(preview.Valid); Assert.Equal(cost, preview.Recovery);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(world.DungeonCombat.Commit(intent).Success);
        Assert.Null(world.DungeonCombat.Active); Assert.Equal(0, world.DungeonCombat.Clock);
        Assert.Equal(cost, Assert.Single(world.State.Tactics.Timeline.Pending, w => w.ActorId == hero.Id).Due);
        Assert.Equal(others, JsonSerializer.Serialize(world.State.Tactics.Timeline.Pending.Where(w => w.ActorId != hero.Id)));
        Assert.Equal(0, world.Count("item.quick-silver")); Assert.Empty(friend.Equipment);
        Assert.Equal("item.quick-silver", hero.Equipment["charm"].Item);
        Assert.True(ActionTiming.Recovery(100, hero.Speed) < cost);
        Assert.Single(world.State.Tactics.Events, e => e.Kind == "EquipmentChanged" && e.Source == hero.Id);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void ReplacingAndRemovingGearConservesSharedStockWithoutHealingOrChangingClassPatterns()
    {
        using var world = WorldSession.CreateDungeonRun(37, ClassKind.Ranger);
        var hero = world.Party.Single(); int maximum = hero.MaxHealth, power = hero.Power;
        hero.Health -= 3; int health = hero.Health;
        var pattern = world.DungeonCombat.Preview(new(TacticalAction.BasicAttack, Facing.East)).Cells;
        var stats = hero.Tactics!.Build!.Stats;
        world.State.Inventory["item.chainmail"] = world.State.Inventory["item.wanderers-cloak"] = world.State.Inventory["item.iron-sword"] = 1;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Equip, Item: "item.chainmail")).Success);
        Assert.Equal(health, hero.Health); Assert.Equal(maximum + 6, hero.MaxHealth);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Equip, Item: "item.wanderers-cloak")).Success);
        Assert.Equal(1, world.Count("item.chainmail")); Assert.Equal(0, world.Count("item.wanderers-cloak"));
        Assert.Equal(health, hero.Health); Assert.Equal(maximum + 4, hero.MaxHealth);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Equip, Item: "item.iron-sword")).Success);
        Assert.Equal(power + 3, hero.Power); Assert.Equal(stats, hero.Tactics.Build.Stats);
        Assert.Equal(pattern, world.DungeonCombat.Preview(new(TacticalAction.BasicAttack, Facing.East)).Cells);
        hero.Health = hero.MaxHealth;
        long time = world.DungeonCombat.Clock;
        long recovery = world.DungeonCombat.Preview(new(TacticalAction.Unequip, Item: "armor")).Recovery;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Unequip, Item: "armor")).Success);
        Assert.Equal(time + recovery, world.DungeonCombat.Clock);
        Assert.Equal(maximum, hero.Health); Assert.Equal(maximum, hero.MaxHealth);
        Assert.Equal(1, world.Count("item.wanderers-cloak")); Assert.False(hero.Equipment.ContainsKey("armor"));
    }

    [Fact]
    public void InvalidStockSlotsTargetsAndLegacyCommandsAreFree()
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var hero = world.Party.Single(); var friend = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 2);
        world.State.Inventory["item.iron-sword"] = 1;
        foreach (var intent in new[] {
            new TacticalIntent(TacticalAction.Equip), new(TacticalAction.Equip, Item: "unknown"),
            new(TacticalAction.Equip, Item: "item.food"), new(TacticalAction.Equip, Item: "item.chainmail"),
            new(TacticalAction.Equip, Target: friend.Id, Item: "item.iron-sword"),
            new(TacticalAction.Unequip, Item: "weapon"), new(TacticalAction.Unequip, Item: "unknown") })
        {
            string before = JsonSerializer.Serialize(world.State);
            Assert.False(world.SubmitDungeon(intent).Success);
            Assert.Equal(before, JsonSerializer.Serialize(world.State));
        }
        string state = JsonSerializer.Serialize(world.State);
        Assert.False(world.Submit(new(WorldCommandKind.Equip, ActorId: hero.Id, Item: "item.iron-sword")).Success);
        Assert.False(world.Submit(new(WorldCommandKind.Unequip, ActorId: hero.Id, Item: "weapon")).Success);
        Assert.Equal(state, JsonSerializer.Serialize(world.State));
        Assert.True(world.SelectDungeonMember(friend.Id).Success); state = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Equip, Item: "item.iron-sword")).Success);
        Assert.Equal(state, JsonSerializer.Serialize(world.State));
    }

    [Fact]
    public void GeneratedEquipmentKeepsIdentityAndIndividualOwnershipAcrossFloorsDeathAndExactSave()
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var hero = world.Party.Single(); var friend = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 2);
        const string id = "gear.999.0.0";
        world.State.GeneratedEquipment.Add(id, new("item.iron-sword", ["affix.kindled", "affix.mending"]));
        world.State.Inventory[id] = 1;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Equip, Item: id)).Success);
        Assert.Equal(friend.Id, world.State.Tactics!.ActiveActor);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Equip, Item: id)).Success);
        Assert.Empty(friend.Equipment); Assert.Equal(id, hero.Equipment["weapon"].Item);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        DungeonAbandonmentTests.Down(world, hero);
        Assert.True(world.SelectDungeonMember(friend.Id).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Abandon, Target: hero.Id)).Success);
        Assert.Equal(LifeState.Dead, hero.Life); Assert.Equal(id, hero.Equipment["weapon"].Item);
        string root = Path.Combine(Path.GetTempPath(), "roag-native-equipment-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root);
            using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(world.Item(id).Effects, loaded.Item(id).Effects);
            Assert.Equal(world.Item(id).Name, loaded.Item(id).Name);
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.False(loaded.SubmitDungeon(new(TacticalAction.Equip, Item: id)).Success);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
