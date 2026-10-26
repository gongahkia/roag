using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class PartyWorkTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-party-work-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    internal static WorldSession Crew()
    {
        var world = WorldCollisionTests.Arena();
        foreach (var actor in world.State.Actors.Where(a => a.Kind == ActorKind.Person && !a.Recruited).Take(3).ToArray())
        { actor.Position = world.State.Position.Offset(2, 0); Assert.True(world.Submit(new(WorldCommandKind.Recruit, ActorId: actor.Id)).Success); }
        world.State.Inventory["food"] = 100; world.Paused = false; return world;
    }
    [Fact]
    public void FourCompanionsWorkConcurrentlyWithoutOverlapsSwapsOrTeleporting()
    {
        var world = Crew(); var anchor = world.State.Position;
        var targets = new[] { anchor.Offset(-6, 0), anchor.Offset(6, 0), anchor.Offset(0, -6), anchor.Offset(0, 6) };
        foreach (var p in targets) world.Chunks.Edit(p, Material.Rock);
        Assert.Equal(4, world.DesignateWork(WorldCommandKind.Dig, targets).Queued);
        int concurrent = 0;
        for (int i = 0; i < 100; i++)
        {
            var old = world.Party.ToDictionary(a => a.Id, a => a.Position); world.AdvanceTicks(1);
            concurrent = Math.Max(concurrent, world.State.Work.Count(o => o.Worker != 0));
            Assert.Equal(4, world.Party.Select(a => a.Position).Distinct().Count());
            foreach (var actor in world.Party)
            {
                Assert.InRange(actor.Position.Distance(old[actor.Id]), 0, 1);
                Assert.InRange(actor.Position.Distance(anchor), 0, WorldSession.WorkRallyRadius);
                Assert.DoesNotContain(world.Party.Where(a => a.Id != actor.Id), a => actor.Position == old[a.Id] && a.Position == old[actor.Id]);
            }
        }
        Assert.Equal(4, concurrent); Assert.Empty(world.State.Work);
        Assert.All(world.Party, a => Assert.True(a.Body.WorkDone > 0));
        Assert.All(targets, p => Assert.Equal(Material.Earth, world.Chunks.At(p)));
        Assert.All(world.Party, a => Assert.InRange(a.Position.Distance(anchor), 0, 2));
    }
    [Fact]
    public void PrioritiesSuspensionNamedAssignmentsAndRallySurviveExactContinuation()
    {
        var world = Crew(); var p = world.State.Position;
        foreach (var target in new[] { p.Offset(-6, 0), p.Offset(6, 0) }) world.Chunks.Edit(target, Material.Rock);
        world.DesignateWork(WorldCommandKind.Dig, [p.Offset(-6, 0), p.Offset(6, 0)]);
        var first = world.State.Work[0]; var second = world.State.Work[1]; var worker = world.Party.Last();
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: worker.Id, WorkId: first.Id)).Success);
        world.Submit(new(WorldCommandKind.ConfigureWork, WorkId: second.Id, Item: "priority", Amount: 2));
        world.Submit(new(WorldCommandKind.ConfigureWork, WorkId: second.Id, Item: "suspend"));
        world.AdvanceTicks(5); Assert.Equal(0, second.Progress); Assert.Equal(worker.Id, first.Worker);
        var path = Path.Combine(_root, "work.json"); world.Save(path); var loaded = WorldSession.Load(path); loaded.Paused = false;
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        foreach (var session in new[] { world, loaded }) session.Submit(new(WorldCommandKind.ConfigureWork, WorkId: second.Id, Item: "resume"));
        for (int i = 0; i < 80; i++)
        { world.AdvanceTicks(1); loaded.AdvanceTicks(1); Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State)); }
    }
    [Fact]
    public void HighPriorityWinsAndUnavailableWorkersDoNotHoldAutomaticClaims()
    {
        var world = WorldCollisionTests.Arena(); world.Paused = false; world.State.Inventory["food"] = 100;
        var p = world.State.Position;
        foreach (var target in new[] { p.Offset(4, 0), p.Offset(-4, 0) }) world.Chunks.Edit(target, Material.Rock);
        world.DesignateWork(WorldCommandKind.Dig, [p.Offset(4, 0), p.Offset(-4, 0)]);
        var urgent = world.State.Work[1]; world.Submit(new(WorldCommandKind.ConfigureWork, WorkId: urgent.Id, Item: "priority", Amount: 2));
        world.AdvanceTicks(1); Assert.Equal(world.State.Party[0], urgent.Worker); Assert.Equal(0, world.State.Work[0].Worker);
        world.Party.First().Body.Fatigue = 90; var position = world.State.Position;
        world.AdvanceTicks(1); Assert.Equal(0, urgent.Worker); Assert.Equal(position, world.State.Position);
    }
    [Fact]
    public void WorkStaysObservedAndLocalAndTravelTemporarilyTakesPrecedence()
    {
        var world = Crew(); var p = world.State.Position;
        world.Chunks.Edit(p.Offset(4, 0), Material.Rock); world.Chunks.Edit(p.Offset(20, 0), Material.Rock);
        world.RevealSurface(p.X - 30, p.Y - 10, p.X + 30, p.Y + 10);
        world.DesignateWork(WorldCommandKind.Dig, [p.Offset(4, 0), p.Offset(20, 0)]);
        world.PartyTravelling = true; var before = world.Party.Select(a => a.Position).ToArray(); world.AdvanceTicks(4);
        Assert.Equal(before, world.Party.Select(a => a.Position)); Assert.All(world.State.Work, o => Assert.Equal(0, o.Progress));
        world.PartyTravelling = false; world.AdvanceTicks(100);
        var distant = Assert.Single(world.State.Work); Assert.Contains("rally", distant.Status); Assert.Equal(0, distant.Progress);
        world.StopPartyWorkApproach(); world.AdvanceTicks(1); Assert.True(distant.Suspended);
    }
    [Fact]
    public void WorkersCannotRouteThroughUnexploredGroundToASeenTarget()
    {
        var world = WorldCollisionTests.Arena(); var origin = world.State.Position; var target = origin.Offset(6, 0);
        world.Chunks.Edit(target, Material.Rock);
        foreach (var chunk in world.State.Chunks) chunk.Discovered.Clear();
        world.Chunks.Delta(origin.Chunk).Discovered.Add(origin.LocalIndex);
        world.Chunks.Delta(target.Chunk).Discovered.Add(target.LocalIndex);
        world.DesignateWork(WorldCommandKind.Dig, [target]); world.Paused = false;
        string chunks = JsonSerializer.Serialize(world.State.Chunks); world.AdvanceTicks(1);
        Assert.Equal(origin, world.State.Position); Assert.Equal(0, world.State.Work.Single().Worker);
        Assert.Contains("observed", world.State.Work.Single().Status); Assert.Equal(chunks, JsonSerializer.Serialize(world.State.Chunks));
    }
    [Fact]
    public void DangerStopsAllPartyWorkAndUsesTheSharedTurnBasedEncounter()
    {
        var world = Crew(); var p = world.State.Position; world.Chunks.Edit(p.Offset(6, 0), Material.Rock);
        world.DesignateWork(WorldCommandKind.Dig, [p.Offset(6, 0)]); world.AdvanceTicks(5);
        var worker = world.Party.Single(a => a.Id == world.State.Work.Single().Worker);
        var enemy = world.State.Actors.First(a => a.Kind == ActorKind.Beast);
        enemy.Position = WorldSession.Directions.Select(d => worker.Position.Offset(d.X, d.Y)).First(c => world.CanEnter(c, enemy.Id));
        double progress = world.State.Work.Single().Progress; world.AdvanceTicks(1);
        Assert.True(world.IsInCombat); Assert.Equal(4, world.State.Battle!.Units.Count(u => u.Friendly));
        Assert.Equal(progress, world.State.Work.Single().Progress); Assert.Equal(0, world.AdvanceTicks(10));
        var positions = world.Party.Select(a => a.Position).ToArray();
        while (!world.State.Battle!.Active.Friendly) world.AdvanceEnemy();
        Assert.Equal(positions, world.Party.Select(a => a.Position)); Assert.Equal(progress, world.State.Work.Single().Progress);
    }
    [Fact]
    public void OldCompatibleWorkDefaultsLoadAndInvalidNewStateFailsSafely()
    {
        var world = WorldCollisionTests.Arena(); world.Submit(new(WorldCommandKind.Build, world.State.Position.Offset(1, 0), Material: Material.Wall));
        string path = Path.Combine(_root, "older.json"); world.Save(path); var node = JsonNode.Parse(File.ReadAllText(path))!;
        var state = node["state"]!.AsObject(); state.Remove("workRally"); var order = state["work"]![0]!.AsObject();
        foreach (string key in new[] { "priority", "pinnedWorker", "suspended" }) order.Remove(key);
        File.WriteAllText(path, node.ToJsonString()); var restored = WorldSession.Load(path);
        Assert.Equal(1, restored.State.Work.Single().Priority); Assert.False(restored.State.Work.Single().Suspended);
        // Old adjacent jobs had no automatic approach intent but still worked in place.
        restored.State.Work.Single().PartyApproach = false; restored.Paused = false; restored.AdvanceTicks(1);
        Assert.True(restored.State.Work.Single().Progress > 0);
        order["priority"] = 999; File.WriteAllText(path, node.ToJsonString()); Assert.Throws<InvalidDataException>(() => WorldSession.Load(path));
    }
    [Fact]
    public void SuspendedNamedAssignmentsCannotCreateDuplicateWorkersWhenNewOrdersAreQueued()
    {
        var world = WorldCollisionTests.Arena(); var p = world.State.Position;
        world.Submit(new(WorldCommandKind.Build, p.Offset(1, 0), Material: Material.Wall));
        var named = world.State.Work.Single();
        world.Submit(new(WorldCommandKind.AssignWork, ActorId: world.State.Party[0], WorkId: named.Id));
        world.Submit(new(WorldCommandKind.ConfigureWork, WorkId: named.Id, Item: "suspend"));
        Assert.True(world.Submit(new(WorldCommandKind.Build, p.Offset(-1, 0), Material: Material.Wall)).Success);
        Assert.Single(world.State.Work, o => o.Worker != 0);
        string path = Path.Combine(_root, "suspended.json"); world.Save(path);
        Assert.Equal(2, WorldSession.Load(path).State.Work.Count);
    }
}
