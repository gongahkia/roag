using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeGatheringTests
{
    [Theory]
    [InlineData("resource.apple-tree")]
    [InlineData("resource.deep-iron")]
    public void PaidNativeHarvestUsesNaturalFiniteNodesAndContinuesExactly(string id)
    {
        using var world = WorldSession.Create(42);
        var target = ResourcePreview.Prepare(world, id); var node = world.ResourceAt(target)!; var actor = world.Party.Single();
        actor.Body.Hunger = actor.Body.Fatigue = 0;
        var combat = world.CreateTacticalSimulation();
        combat.Register(actor, new() { Build = CharacterBuild.Create(ClassKind.Adventurer, 42), AutoEat = false });
        Assert.Empty(actor.CombatTrait); Assert.Empty(actor.Talents);
        Assert.Empty(world.ProgressionChoices(actor.Id));
        combat.AdvanceToPartyDecision();
        var before = new Dictionary<string, int>(world.State.Inventory);
        Assert.True(combat.Commit(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Harvest, target))).Success);
        combat.AdvanceToPartyDecision(4);
        Assert.True(Assert.Single(world.State.Work).Progress > 0);
        string root = Path.Combine(Path.GetTempPath(), "roag-native-harvest-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            var continuation = loaded.CreateTacticalSimulation();
            for (int i = 0; i < 100 && actor.Tactics!.Activity != null; i++)
            { combat.AdvanceToPartyDecision(32); continuation.AdvanceToPartyDecision(32); }
            Assert.Null(actor.Tactics!.Activity); Assert.Empty(world.State.Work);
            Assert.Equal(node.Available - 1, world.ResourceAt(target)!.Available);
            foreach (var (item, quantity) in node.Definition.Yield)
            {
                string stack = world.Content.Get<ItemDefinition>(item).Stack;
                Assert.Equal(before.GetValueOrDefault(stack) + quantity, world.Count(stack));
            }
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.True(combat.Clock > 1200);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void ForagingUsesTheWorkingMembersCooldownAndConsumesFiniteHabitatFoodOnce()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var founder = world.Party.Single();
        var member = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 1);
        member.Position = founder.Position.Offset(2, 0); world.Chunks.Edit(member.Position, Material.Floor);
        long founderCooldown = founder.Body.ForageAfter = 1000;
        int food = world.Count("food"), wild = world.HabitatAt(member.Position).Food;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Watch, Choice: 10)).Success);
        Assert.Equal(member.Id, world.DungeonMember.Id);
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Forage))).Success);
        for (int i = 0; i < 100 && member.Tactics!.Activity != null; i++) world.ContinueDungeonActivities(32);
        Assert.Null(member.Tactics!.Activity); Assert.Equal(food + 2, world.Count("food"));
        Assert.Equal(wild - 2, world.HabitatAt(member.Position).Food);
        Assert.True(member.Body.ForageAfter > world.State.Tick); Assert.Equal(founderCooldown, founder.Body.ForageAfter);
        Assert.True(world.CancelDungeonActivity(founder.Id).Success);
    }

    [Fact]
    public void RepairConsumesOneRetainedMaterialAndRestoresAnActualOwnedStructure()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var actor = world.Party.Single();
        var target = actor.Position.Offset(1, 0); world.Chunks.Edit(target, Material.Floor); world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Build, target, Material: Material.Wall))).Success);
        for (int i = 0; i < 100 && actor.Tactics!.Activity != null; i++) world.ContinueDungeonActivities(32);
        var structure = Assert.Single(world.State.Structures); structure.Condition = 40;
        int stone = world.Count("stone");
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Repair, target))).Success);
        Assert.Equal(stone - 1, world.Count("stone"));
        for (int i = 0; i < 100 && actor.Tactics!.Activity != null; i++) world.ContinueDungeonActivities(32);
        Assert.Null(actor.Tactics!.Activity); Assert.Equal(100, structure.Condition); Assert.Empty(world.State.Work);
        Assert.Equal(stone - 1, world.Count("stone"));
    }
}
