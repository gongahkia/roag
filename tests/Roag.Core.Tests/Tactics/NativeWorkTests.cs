using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeWorkTests
{
    private static void Finish(WorldSession world, Actor worker)
    {
        for (int i = 0; i < 100 && worker.Tactics!.Activity != null; i++) world.ContinueDungeonActivities(32);
        Assert.Null(worker.Tactics!.Activity);
    }

    [Fact]
    public void DiggingPaysTravelAndWorkChangesRealTerrainAndReturnsOutputToSharedStockOnce()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var worker = world.Party.Single();
        var target = worker.Position.Offset(3, 0); world.Chunks.Edit(target, Material.Wall); world.Reveal();
        int stock = world.Count("stone"); var start = worker.Position;
        string before = JsonSerializer.Serialize(world.State);
        Assert.True(world.DungeonCombat.Preview(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Dig, target))).Valid);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Dig, target))).Success);
        Assert.Equal(0, Assert.Single(world.State.Work).Progress); Assert.Equal(100, world.DungeonCombat.Clock);
        Assert.Equal(start, worker.Position);
        world.ContinueDungeonActivities(1);
        Assert.NotEqual(start, worker.Position);
        Assert.Contains(world.State.Tactics!.Events, e => e.Kind == "Moved" && e.Source == worker.Id);
        Finish(world, worker);
        Assert.Equal(Material.Floor, world.Chunks.At(target)); Assert.Empty(world.State.Work);
        Assert.True(world.Count("stone") > stock); Assert.True(worker.Position.Distance(target) <= 1);
        Assert.True(world.DungeonCombat.Clock >= 12000); Assert.True(worker.Body.WorkDone > 0);
        int result = world.Count("stone"); Assert.False(world.ContinueDungeonActivities()); Assert.Equal(result, world.Count("stone"));
    }

    [Fact]
    public void BuildingReservesSharedInputsOnceAndCancellationAcrossFloorsRefundsThemWithoutTime()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var worker = world.Party.Single();
        var target = worker.Position.Offset(1, 0); world.Chunks.Edit(target, Material.Floor); world.Reveal();
        var other = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 2);
        int wood = world.Count("wood"), stone = world.Count("stone");
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Build, target, Material: Material.Wall))).Success);
        var order = Assert.Single(world.State.Work); Assert.True(world.Count("wood") < wood || world.Count("stone") < stone);
        Assert.Equal(other.Id, world.DungeonMember.Id); long clock = world.DungeonCombat.Clock;
        Assert.True(world.CancelDungeonWork(order.Id).Success);
        Assert.Equal(wood, world.Count("wood")); Assert.Equal(stone, world.Count("stone")); Assert.Equal(clock, world.DungeonCombat.Clock);
        Assert.Empty(world.State.Work); Assert.Null(worker.Tactics!.Activity);
        Assert.False(world.CancelDungeonWork(order.Id).Success);
    }

    [Fact]
    public void ConstructionCompletesAndTheOwnedStructureCanBeDismantled()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var worker = world.Party.Single();
        var target = worker.Position.Offset(1, 0); world.Chunks.Edit(target, Material.Floor); world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Build, target, Material: Material.Wall))).Success);
        Finish(world, worker);
        Assert.Equal(Material.Wall, world.Chunks.At(target)); Assert.Contains(world.State.Structures, s => s.Position == target);
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Dig, target))).Success);
        Finish(world, worker);
        Assert.Equal(Material.Floor, world.Chunks.At(target)); Assert.DoesNotContain(world.State.Structures, s => s.Position == target);
    }

    [Fact]
    public void PartialWorkSuspendsAndResumesExactlyWithoutFreeProgressOrDuplicateReservation()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var worker = world.Party.Single();
        var target = worker.Position.Offset(1, 0); world.Chunks.Edit(target, Material.Wall); world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Dig, target))).Success);
        world.ContinueDungeonActivities(8); var order = Assert.Single(world.State.Work); Assert.True(order.Progress > 0);
        double progress = order.Progress;
        Assert.True(world.CancelDungeonActivity(worker.Id).Success); Assert.True(order.Suspended);
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.ContinueDungeonActivities()); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.AssignWork, WorkId: order.Id))).Success);
        Assert.Equal(progress, order.Progress); Assert.False(order.Suspended);
        string root = Path.Combine(Path.GetTempPath(), "roag-native-work-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            for (int i = 0; i < 3; i++) { world.ContinueDungeonActivities(8); loaded.ContinueDungeonActivities(8); }
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void AnotherReadyMemberCanTakeOverASeparatedCompanionsUnfinishedProject()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var worker = world.Party.Single();
        var target = worker.Position.Offset(1, 0); world.Chunks.Edit(target, Material.Wall); world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Dig, target))).Success);
        world.ContinueDungeonActivities(8); var order = Assert.Single(world.State.Work);
        var other = DungeonPartyTests.AddMember(world, ClassKind.Ranger, 2);
        var room = world.State.Dungeon!.Floors.Single(f => f.Origin.Floor == 2).Rooms.Single(r => r.Role == "start");
        other.Position = room.Origin.Offset(room.Width / 2, room.Height / 2); world.Chunks.Edit(other.Position, Material.Floor);
        Assert.True(world.DungeonCombat.Commit(new(TacticalAction.ContinueActivity)).Success);
        world.DungeonCombat.AdvanceToPartyDecision(); Assert.Equal(other.Id, world.DungeonMember.Id);
        double progress = order.Progress; var inventory = JsonSerializer.Serialize(world.State.Inventory);
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.AssignWork, WorkId: order.Id))).Success);
        Assert.Null(worker.Tactics!.Activity); Assert.Equal(other.Id, order.Worker); Assert.False(order.Suspended);
        Assert.Equal(progress, order.Progress); Assert.Equal(inventory, JsonSerializer.Serialize(world.State.Inventory));
        Assert.Equal(order.Id, other.Tactics!.Activity!.Work);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void CraftingAtAPhysicalCampUsesRetainedRecipeCostsAndProducesSharedEquipment()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var worker = world.Party.Single();
        var station = worker.Position.Offset(1, 0); world.Chunks.Edit(station, Material.Camp); world.Reveal();
        var recipe = world.Content.Game.Recipes.Select(world.Content.Get<RecipeDefinition>).First(r => r.Output != null && r.Work <= 100
            && r.Cost.All(p => world.Count(world.Content.Get<ItemDefinition>(p.Key).Stack) >= p.Value));
        var stock = new Dictionary<string, int>(world.State.Inventory); string output = world.Content.Get<ItemDefinition>(recipe.Output!).Stack;
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Craft, Item: recipe.Id))).Success);
        foreach (var cost in recipe.Cost)
        {
            string stack = world.Content.Get<ItemDefinition>(cost.Key).Stack;
            Assert.Equal(stock.GetValueOrDefault(stack) - cost.Value, world.Count(stack));
        }
        int before = world.Count(output); Finish(world, worker);
        Assert.Equal(before + recipe.Amount, world.Count(output)); Assert.Empty(world.State.Work);
        Assert.DoesNotContain(worker.Cargo, p => p.Value > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrdinaryDoorsCanBeDismantledWithoutLeavingASaveBreakingFixture(bool open)
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var worker = world.Party.Single();
        var target = worker.Position.Offset(1, 0); world.Chunks.Edit(target, Material.Floor);
        var door = world.DungeonCombat.SpawnDoor("test:native-work", target); world.Reveal();
        if (open) Assert.True(world.SubmitDungeon(new(TacticalAction.OpenDoor, Target: door.Id)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Dig, target))).Success);
        Finish(world, worker);
        Assert.Equal(Material.Floor, world.Chunks.At(target));
        Assert.DoesNotContain(world.State.Tactics!.Doors.Nodes, d => d.Id == door.Id);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void AChangedObservedWorkSiteReleasesInputsAndStopsTheOrderWithoutAFreeWorkStep()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var worker = world.Party.Single();
        var target = worker.Position.Offset(1, 0); world.Chunks.Edit(target, Material.Floor); world.Reveal();
        int wood = world.Count("wood"), stone = world.Count("stone");
        Assert.True(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Build, target, Material: Material.Wall))).Success);
        world.Chunks.Edit(target, Material.Rock); long clock = world.DungeonCombat.Clock;
        Assert.True(world.ContinueDungeonActivities(1));
        Assert.Empty(world.State.Work); Assert.Null(worker.Tactics!.Activity);
        Assert.Equal(clock, world.DungeonCombat.Clock); Assert.Equal(wood, world.Count("wood")); Assert.Equal(stone, world.Count("stone"));
    }

    [Fact]
    public void InvalidWorkCannotReserveResourcesOrSpendARecovery()
    {
        using var world = WorldSession.CreateDungeonRun(37, survival: true); var worker = world.Party.Single();
        string before = JsonSerializer.Serialize(world.State);
        Assert.False(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Dig, worker.Position with { Floor = 2 }))).Success);
        Assert.False(world.SubmitDungeon(new(TacticalAction.StartWork, Work: new(WorldCommandKind.Buy))).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
    }
}
