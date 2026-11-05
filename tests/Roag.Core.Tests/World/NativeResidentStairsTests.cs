using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeResidentStairsTests
{
    private static (WorldSession World, Actor Worker, WorldPosition Top, WorldPosition Bottom) Prepare(bool onStair)
    {
        var world = NativeSocietyTests.AtTick(4); var worker = world.ActorById(1000);
        var top = world.Plan.Towns[1].Center.Offset(0, -30); var bottom = top.Offset(-4, 0) with { Floor = 1 };
        for (int y = -1; y <= 1; y++) for (int x = -2; x <= 1; x++) world.Chunks.Edit(bottom.Offset(x, y), Material.Wall);
        world.Chunks.Edit(bottom.Offset(-1, 0), Material.Floor); world.Chunks.Edit(bottom, Material.StairsUp);
        world.Chunks.Edit(top, Material.StairsDown); world.Chunks.Edit(top.Offset(1, 0), Material.Floor);
        var target = top.Offset(2, 0); world.Chunks.Edit(target, Material.Floor);
        worker.Position = onStair ? bottom : bottom.Offset(-1, 0); worker.Journey = null;
        worker.Routine = new(RoutineKind.AssignedWork, target, world.State.Tick, "Cross-floor work fixture");
        world.State.Inventory["stone"] -= 2;
        world.State.Work.Add(new() { Id = world.State.NextWorkId++, Kind = WorkKind.Build, Definition = "recipe.wall",
            Target = target, Before = Material.Floor, After = Material.Wall, Required = 15, Worker = worker.Id,
            RequestedBy = world.Party.Single().Id, PinnedWorker = true, SupplyVersion = 1,
            Supplies = [new() { Stack = "stone", Count = 2, Phase = SupplyPhase.Delivered }], Reserved = new() { ["stone"] = 2 } });
        return (world, worker, top, bottom);
    }

    private static void Wait(WorldSession world, int turns)
    { for (int i = 0; i < turns; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success); }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void OrdinaryResidentsUseRealStairsAtTheirExistingMovementCadenceWithoutTacticalBuilds(bool onStair)
    {
        var (world, worker, top, _) = Prepare(onStair); using var dispose = world;
        var hero = world.Party.Single().Position; int stone = world.Count("stone");
        Wait(world, 11); Assert.Equal(1, worker.Position.Floor);
        Wait(world, 1); Assert.Equal(top, worker.Position); Assert.Null(worker.Journey);
        Assert.Equal(hero, world.Party.Single().Position); Assert.Null(worker.Tactics); Assert.False(worker.Recruited);
        Assert.Equal(stone, world.Count("stone")); Assert.False(world.Chunks.Seen(top));
        // Surface weather may now take priority over the assigned project.
        // Either routine still takes exactly one ordinary physical step.
        Wait(world, 60); Assert.Equal(1, top.Distance(worker.Position));
        Assert.Equal(0, Assert.Single(world.State.Work).Progress);
    }

    [Theory]
    [InlineData("wall")] [InlineData("occupied")] [InlineData("unpaired")]
    public void BlockedCrossFloorWorkKeepsAnExactSaveAndResumesOnlyAfterPhysicalAccessReturns(string obstruction)
    {
        var (world, worker, top, bottom) = Prepare(true); using var dispose = world;
        Actor? blocker = null;
        if (obstruction == "wall") world.Chunks.Edit(top, Material.Wall);
        if (obstruction == "unpaired") world.Chunks.Edit(top, Material.Floor);
        if (obstruction == "occupied")
        { blocker = world.ActorById(1001); blocker.Position = top; blocker.Life = LifeState.Downed; blocker.Health = 0; }
        Wait(world, 12); Assert.Equal(bottom, worker.Position); Assert.Null(worker.Journey);
        string root = Path.Combine(Path.GetTempPath(), "roag-native-resident-stairs-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            foreach (var run in new[] { world, loaded })
            {
                run.Chunks.Edit(top, Material.StairsDown);
                if (blocker != null) run.ActorById(blocker.Id).Position = top.Offset(0, -3);
                Wait(run, 60);
            }
            Assert.Equal(top, worker.Position); Assert.Null(worker.Tactics);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            world.Save(root); using var arrived = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(arrived.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
