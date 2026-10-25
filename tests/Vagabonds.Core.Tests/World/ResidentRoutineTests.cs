using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class ResidentRoutineTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-routines-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void AuthoringPreviewExecutesActualFoodAndRestDestinations(int seed)
    {
        var report = JsonSerializer.SerializeToNode(RoutinePreview.Run(ContentCatalog.Default, seed))!;
        Assert.True(report["passed"]!.GetValue<bool>());
    }
    private static WorldSession Prepare(out Actor person)
    {
        var world = WorldCollisionTests.Arena(); var p = world.State.Position;
        for (int y = -25; y <= 25; y++) for (int x = -25; x <= 25; x++) world.Chunks.Edit(p.Offset(x, y), Material.Floor);
        person = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited && a.HomeTown == 0);
        person.Position = p.Offset(4, 0); person.Body.Hunger = person.Body.Fatigue = 0;
        world.Paused = false; return world;
    }

    [Fact]
    public void FoodIsTakenAtTheSupplyDestinationAndNotWhileWalkingAcrossTown()
    {
        using var world = Prepare(out var person); person.Body.Hunger = 40;
        int food = world.State.Towns[0].Stocks["food"];
        world.AdvanceTicks(1);
        Assert.Equal(RoutineKind.Food, person.Routine!.Kind); Assert.True(person.Body.Hunger > 40);
        Assert.Equal(food, world.State.Towns[0].Stocks["food"]); Assert.Equal(0, person.Body.ForageProgress);
        world.AdvanceTicks(20);
        Assert.Equal(food - 1, world.State.Towns[0].Stocks["food"]); Assert.True(person.Body.Hunger < 10);
        Assert.Equal(0, person.Body.ForageProgress);
    }

    [Fact]
    public void RestInterruptsWorkOnlyBeginsAtHomeAndThenResumesTheSameOrder()
    {
        using var world = Prepare(out var person); var p = world.State.Position;
        Assert.True(world.Submit(new(WorldCommandKind.Build, p.Offset(1, 0), Material: Material.Wall)).Success);
        var work = Assert.Single(world.State.Work); work.Required = 1000;
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: work.Id)).Success);
        person.Body.Fatigue = 80; world.AdvanceTicks(1);
        Assert.Equal(RoutineKind.Rest, person.Routine!.Kind); Assert.True(person.Body.Fatigue > 80);
        Assert.Equal(0, person.Body.RestUntil); Assert.Equal(0, work.Progress); Assert.Equal(person.Id, work.Worker);
        int ticks = 0;
        while (person.Body.RestUntil <= world.State.Tick && ticks++ < 200) Assert.Equal(1, world.AdvanceTicks(1));
        Assert.True(person.Position.Distance(person.Routine!.Target) <= 1); Assert.True(person.Body.RestUntil > world.State.Tick);
        double fatigue = person.Body.Fatigue; world.AdvanceTicks(20); Assert.True(person.Body.Fatigue < fatigue);
        for (int i = 0; i < 500 && work.Progress == 0; i++) Assert.Equal(1, world.AdvanceTicks(1));
        Assert.True(work.Progress > 0); Assert.Equal(person.Id, work.Worker); Assert.Equal(RoutineKind.AssignedWork, person.Routine!.Kind);
    }

    [Fact]
    public void DangerOverridesWorkAndFoodWithoutDiscardingProgressOrReservation()
    {
        using var world = Prepare(out var person); var p = world.State.Position;
        person.Position = p.Offset(10, 0); person.Body.Hunger = 40;
        var threat = world.State.Actors.First(a => a.Kind == ActorKind.Raider && a.EncounterLeader == 0);
        threat.Position = p.Offset(12, 0); threat.Body.RestUntil = 1000;
        Assert.True(world.Submit(new(WorldCommandKind.Build, p.Offset(1, 0), Material: Material.Wall)).Success);
        var work = Assert.Single(world.State.Work); work.Required = 1000; work.Progress = 3;
        foreach (var supply in work.Supplies!) supply.Phase = SupplyPhase.Delivered; // Already supplied partial construction.
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: work.Id)).Success);
        var reserved = new Dictionary<string, int>(work.Reserved); int distance = person.Position.Distance(threat.Position);
        world.AdvanceTicks(5);
        Assert.Equal(RoutineKind.Danger, person.Routine!.Kind); Assert.True(person.Position.Distance(threat.Position) > distance);
        Assert.Equal(3, work.Progress); Assert.Equal(person.Id, work.Worker); Assert.Equal(reserved, work.Reserved); Assert.Equal(0, person.Body.ForageProgress);
        threat.Life = LifeState.Dead; threat.Health = 0; person.Body.Hunger = 0;
        for (int i = 0; i < 160 && work.Progress == 3; i++) Assert.Equal(1, world.AdvanceTicks(1));
        Assert.True(work.Progress > 3); Assert.Equal(person.Id, work.Worker);
    }

    [Fact]
    public void BlockedFoodRoutesWaitAndRecoverWhenTheExitOpens()
    {
        using var world = Prepare(out var person); var p = person.Position; person.Body.Hunger = 40;
        foreach (var d in WorldSession.Directions) world.Chunks.Edit(p.Offset(d.X, d.Y), Material.Wall);
        world.AdvanceTicks(10); Assert.True(person.Routine!.Blocked); Assert.Equal(p, person.Position);
        Assert.Contains("route blocked", world.RoutineDescription(person));
        world.Chunks.Edit(p.Offset(-1, 0), Material.Floor); world.AdvanceTicks(20);
        Assert.True(person.Body.Hunger < 10); Assert.False(person.Routine!.Blocked);
    }

    [Fact]
    public void LeavingTheRegionDoesNotStrandAResidentInAnInterruptedJob()
    {
        using var world = Prepare(out var person); var p = world.State.Position;
        Assert.True(world.Submit(new(WorldCommandKind.Build, p.Offset(1, 0), Material: Material.Wall)).Success);
        var work = Assert.Single(world.State.Work); work.Required = 1000;
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: work.Id)).Success);
        for (int i = 0; i < 100 && work.Progress < 3; i++) world.AdvanceTicks(1);
        Assert.True(work.Progress >= 3); double progress = work.Progress;
        person.Body.Fatigue = 80; world.AdvanceTicks(5); Assert.Equal(RoutineKind.Rest, person.Routine!.Kind);
        var far = world.Plan.Towns[1].Center; world.State.Position = world.Party.Single().Position = far; world.Reveal();
        bool restedAtHome = false;
        for (int i = 0; i < 750 && work.Progress == progress; i++)
        {
            var before = person.Position; Assert.Equal(1, world.AdvanceTicks(1)); Assert.InRange(before.Distance(person.Position), 0, 1);
            if (person.Body.RestUntil > world.State.Tick)
            { restedAtHome = true; Assert.InRange(person.Position.Distance(person.Routine!.Target), 0, 1); }
        }
        Assert.True(restedAtHome);
        Assert.True(work.Progress > progress); Assert.Equal(person.Id, work.Worker);
        Assert.Same(person, world.ActorById(person.Id)); Assert.Single(world.State.Actors, a => a.Id == person.Id);
    }

    [Fact]
    public void SocialRoutinesFollowTheActualFriendAndKeepHiddenNamesPrivate()
    {
        using var world = Prepare(out var person); var friend = world.ActorById(person.FriendId);
        friend.Position = person.Position.Offset(5, 0); world.State.Tick = WorldSession.TicksPerHour * 17;
        world.AdvanceTicks(1); Assert.Equal(RoutineKind.Social, person.Routine!.Kind); Assert.Equal(friend.Id, person.Routine.Other);
        world.Visible.Remove(friend.Position); string before = JsonSerializer.Serialize(world.State);
        Assert.DoesNotContain(friend.Name, world.RoutineDescription(person)); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        world.AdvanceTicks(20); Assert.InRange(person.Position.Distance(friend.Position), 0, 2);
        person.Trust[friend.Id] = -40; world.AdvanceTicks(5); Assert.NotEqual(RoutineKind.Social, person.Routine!.Kind);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PhysicalRoutineAndBodyContinuationRemainExactAcrossASave(bool rest)
    {
        using var world = Prepare(out var person);
        if (rest) person.Body.Fatigue = 80; else person.Body.Hunger = 40;
        world.AdvanceTicks(7); string file = Path.Combine(_directory, "world.json"); world.Save(file);
        using var loaded = WorldSession.Load(file); loaded.Paused = false;
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        for (int i = 0; i < 450; i++)
        {
            Assert.Equal(1, world.AdvanceTicks(1)); Assert.Equal(1, loaded.AdvanceTicks(1));
            if (i % 37 == 0 || i == 449) Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
    }

    [Theory]
    [InlineData("kind")] [InlineData("target")] [InlineData("since")] [InlineData("other")] [InlineData("reason")]
    public void ForgedRoutineStateIsRejectedWithoutChangingTheSave(string field)
    {
        using var world = Prepare(out var person); world.AdvanceTicks(1);
        string file = Path.Combine(_directory, "world.json"); world.Save(file); var json = JsonNode.Parse(File.ReadAllText(file))!;
        var routine = json["state"]!["actors"]!.AsArray().First(a => a!["id"]!.GetValue<long>() == person.Id)!["routine"]!;
        switch (field)
        {
            case "kind": routine[field] = 999; break;
            case "target": routine[field]!["x"] = -1; break;
            case "since": routine[field] = -1; break;
            case "other": routine[field] = long.MaxValue; break;
            case "reason": routine[field] = new string('x', 257); break;
        }
        File.WriteAllText(file, json.ToJsonString()); string before = File.ReadAllText(file);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(file)); Assert.Equal(before, File.ReadAllText(file));
    }
}
