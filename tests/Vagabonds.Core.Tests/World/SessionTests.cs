using System.Text.Json;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class SessionTests
{
    private static WorldSession Open(int seed = 42) => WorldSession.Create(seed);
    [Fact]
    public void PlayersCanDismantleTheirWallsWithoutFoodOrDuplicatingMaterials()
    {
        var s = Open(); var p = s.State.Position.Offset(1, 0); int stone = s.Count("stone");
        s.Chunks.Edit(p, Material.Floor); Assert.True(s.Submit(new(WorldCommandKind.Build, p, Material: Material.Wall)).Success);
        s.State.Inventory["food"] = 0;
        Assert.True(s.Submit(new(WorldCommandKind.Dig, p)).Success);
        Assert.Equal(stone, s.Count("stone")); Assert.True(Materials.Walkable(s.Chunks.At(p)));
    }
    [Fact]
    public void GeneratedTravellerStartsAloneAndRecruitmentRemovesAWorker()
    {
        var s = Open(); var first = Assert.Single(s.Party);
        Assert.Equal(LifeState.Alive, first.Life);
        var recruit = s.State.Actors.First(a => a.HomeTown == 0 && a.Job == "farmer");
        recruit.Position = s.State.Position.Offset(1, 0);
        Assert.True(s.Submit(new(WorldCommandKind.Recruit, ActorId: recruit.Id)).Success);
        Assert.Contains(recruit.Id, s.State.Party); Assert.Equal(1, s.State.Towns[0].Vacancies);
        int food = s.State.Towns[0].Stocks["food"];
        s.Paused = false; s.AdvanceTicks(300);
        Assert.True(s.State.Towns[0].Stocks["food"] < food + 24);
        Assert.Equal(2, s.Party.Count());
    }
    [Fact]
    public void FixedTicksIgnoreFrameRateAndAllPauseReasonsStopTheClock()
    {
        var a = Open(); var b = Open(); a.Paused = b.Paused = false;
        for (int i = 0; i < 100; i++) a.AdvanceFrame(TimeSpan.FromMilliseconds(20));
        for (int i = 0; i < 20; i++) b.AdvanceFrame(TimeSpan.FromMilliseconds(100));
        Assert.Equal(10, a.State.Tick); Assert.Equal(a.State.Tick, b.State.Tick);
        Assert.Equal(JsonSerializer.Serialize(a.State), JsonSerializer.Serialize(b.State));
        a.Paused = true; Assert.Equal(0, a.AdvanceTicks(100)); a.Paused = false;
        a.ModalOpen = true; Assert.Equal(0, a.AdvanceTicks(100)); a.ModalOpen = false;
        a.Focused = false; Assert.Equal(0, a.AdvanceTicks(100)); a.Focused = true;
        a.StartBattle(a.State.Actors.First(x => x.Kind == ActorKind.Beast).Id); Assert.Equal(0, a.AdvanceTicks(100));
    }
    [Fact]
    public void CoarseTradeConservesStocksAndCargoAccountingForProductionAndConsumption()
    {
        var s = Open(); long initial = s.AccountedResources(); s.Paused = false; s.AdvanceTicks(2400);
        Assert.Equal(initial + s.State.ResourcesProduced - s.State.ResourcesConsumed - s.State.ResourcesLost, s.AccountedResources());
        Assert.Contains(s.State.Actors, a => a.Kind == ActorKind.Caravan && a.Traveling);
    }
    [Fact]
    public void BlockingRoadsChangesRegionalRoutingAndRepairRestoresIt()
    {
        var s = Open(); var road = s.Plan.Roads[0];
        var p = road.A.Offset(50, 0);
        for (int y = -1; y <= 1; y++) s.Chunks.Edit(p.Offset(0, y), Material.Wall);
        Assert.True(s.RoadBlocked(road));
        Assert.DoesNotContain(road.Id, s.RegionalRoute(road.FromTown, road.ToTown));
        Assert.NotEmpty(s.RegionalRoute(road.FromTown, road.ToTown));
        s.Chunks.Edit(p, Material.Earth);
        Assert.False(s.RoadBlocked(road)); Assert.Equal(new[] { road.Id }, s.RegionalRoute(road.FromTown, road.ToTown));
    }
    [Fact]
    public void ConstructionDiggingStorageAndKnownRoutesUsePersistentEdits()
    {
        var s = Open(); var p = s.State.Position.Offset(1, 0); s.Chunks.Edit(p, Material.Floor);
        Assert.True(s.Submit(new(WorldCommandKind.Build, p, Material: Material.Storage)).Success);
        Assert.True(s.Submit(new(WorldCommandKind.Store, p, Item: "wood", Amount: 3)).Success);
        Assert.False(s.Submit(new(WorldCommandKind.Dig, p)).Success);
        Assert.True(s.Submit(new(WorldCommandKind.Take, p, Item: "wood", Amount: 3)).Success);
        Assert.True(s.Submit(new(WorldCommandKind.Dig, p)).Success);
        Assert.True(Materials.Walkable(s.Chunks.At(p)));
        var destination = s.State.Position.Offset(2, 0); s.Chunks.Edit(destination, Material.Floor); s.Reveal();
        Assert.All(s.Route(destination), cell => Assert.True(s.Chunks.Seen(cell)));
        Assert.Empty(s.Route(s.State.Position.Offset(100, 0)));
        var stairs = s.Plan.Sites[0].Center.Offset(2, 0);
        s.State.Position = stairs.Offset(1, 0);
        Assert.False(s.Submit(new(WorldCommandKind.Build, stairs, Material: Material.Floor)).Success);
        Assert.True(s.Submit(new(WorldCommandKind.Interact, stairs)).Success);
        Assert.Equal(1, s.State.Position.Floor);
        Assert.True(s.Submit(new(WorldCommandKind.Interact, s.State.Position)).Success);
        Assert.Equal(stairs, s.State.Position);
    }
    [Fact]
    public void InvalidActionsConsumeNeitherResourcesNorCombatRandomness()
    {
        var s = Open(); string before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Submit(new(WorldCommandKind.Dig, s.State.Position.Offset(5, 0))).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
        s.StartBattle(s.State.Actors.First(a => a.Kind == ActorKind.Beast).Id);
        while (!s.State.Battle!.Active.Friendly) s.AdvanceEnemy();
        before = JsonSerializer.Serialize(s.State);
        Assert.False(s.Act(99, 0).Success); Assert.False(s.Act(0, 0).Success);
        Assert.Equal(before, JsonSerializer.Serialize(s.State));
    }
}
