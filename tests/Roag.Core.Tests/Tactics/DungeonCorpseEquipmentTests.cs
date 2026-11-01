using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonCorpseEquipmentTests
{
    [Fact]
    public void PhysicalRecoveryReturnsExactGeneratedGearOnceWithoutMovingBoonsOrRevivingTheDead()
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var actor = world.Party.Single(); var member = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 2);
        const string id = "gear.999.0.0";
        world.State.GeneratedEquipment.Add(id, new("item.iron-sword", ["affix.kindled"]));
        world.State.Inventory[id] = 1;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Equip, Item: id)).Success);
        actor.Tactics!.Boons.Inventory = actor.Tactics.Boons.Inventory.Grant("core:boon/temporal_echo", BoonRarity.Common, ClassKind.Adventurer);
        DungeonAbandonmentTests.Down(world, actor);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Abandon, Target: actor.Id)).Success);
        Assert.Equal(LifeState.Dead, actor.Life); Assert.Equal(0, world.Count(id));
        var body = actor.Position; string build = JsonSerializer.Serialize(actor.Tactics.Build), boons = JsonSerializer.Serialize(actor.Tactics.Boons);
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.RecoverEquipment, Target: actor.Id)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        // Physical test placement: a living member stands beside the existing corpse.
        member.Position = body.Offset(1, 0); world.State.Position = member.Position;
        world.Chunks.Edit(member.Position, Material.Floor); world.Reveal();
        long time = world.DungeonCombat.Clock;
        long recovery = ActionTiming.Recovery(100, member.Speed);
        var intent = new TacticalIntent(TacticalAction.RecoverEquipment, Target: actor.Id);
        Assert.True(world.DungeonCombat.Preview(intent).Valid);
        Assert.True(world.SubmitDungeon(intent).Success);
        Assert.Equal(time + recovery, world.DungeonCombat.Clock); Assert.Equal(1, world.Count(id));
        Assert.Empty(actor.Equipment); Assert.Equal(LifeState.Dead, actor.Life); Assert.Equal(0, actor.Health); Assert.Equal(body, actor.Position);
        Assert.Equal(build, JsonSerializer.Serialize(actor.Tactics.Build)); Assert.Equal(boons, JsonSerializer.Serialize(actor.Tactics.Boons));
        Assert.Contains(actor.Id, world.State.Party); Assert.Empty(member.Equipment);
        string after = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(intent).Success); Assert.Equal(after, JsonSerializer.Serialize(world.State));
        string root = Path.Combine(Path.GetTempPath(), "roag-corpse-equipment-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Equip, Item: id)).Success);
            Assert.Equal(0, loaded.Count(id)); Assert.Equal(id, loaded.DungeonMember.Equipment["weapon"].Item);
            Assert.False(loaded.SubmitDungeon(intent).Success);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void LivingDownedEmptyUnknownAndUnselectedTargetsRejectWithoutSpendingSuppliesOrTime()
    {
        using var world = WorldSession.CreateDungeonRun(37);
        var actor = world.Party.Single(); var other = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 2);
        foreach (long target in new[] { 0, long.MaxValue, actor.Id, other.Id, world.State.Dungeon!.Recruits[0].Actor })
        {
            string before = JsonSerializer.Serialize(world.State);
            Assert.False(world.SubmitDungeon(new(TacticalAction.RecoverEquipment, Target: target)).Success);
            Assert.Equal(before, JsonSerializer.Serialize(world.State));
        }
        DungeonAbandonmentTests.Down(world, other);
        string downed = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.RecoverEquipment, Target: other.Id)).Success);
        Assert.Equal(downed, JsonSerializer.Serialize(world.State));
        Assert.True(world.SelectDungeonMember(other.Id).Success); downed = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.RecoverEquipment, Target: actor.Id)).Success);
        Assert.Equal(downed, JsonSerializer.Serialize(world.State));
    }
}
