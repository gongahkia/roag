using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class ResidentJourneyTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-journeys-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    private static WorldSession Prepare(out Actor person, out WorldPosition far)
    {
        var world = WorldSession.Create(42); person = RoutinePreview.Prepare(world); far = JourneyPreview.PrepareFarField(world);
        person.Body.Hunger = 0; person.Body.Fatigue = 80; world.Paused = false; world.AdvanceTicks(9);
        Assert.NotNull(person.Journey); Assert.True(person.Journey!.Steps.Count > 3); return world;
    }
    private static void Leave(WorldSession world, WorldPosition far) => world.State.Position = world.Party.Single().Position = far;
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void RealPreviewPreservesNearFarFoodRestAndSavedContinuation(int seed)
    { Assert.True(JsonSerializer.SerializeToNode(JourneyPreview.Run(ContentCatalog.Default, seed))!["passed"]!.GetValue<bool>()); }

    [Fact]
    public void RetainedOffscreenFootstepsDoNotLoadTerrainAgain()
    {
        using var world = Prepare(out var person, out var far); Leave(world, far); world.AdvanceTicks(5);
        var before = world.ResidentJourneys; var position = person.Position; int generated = 0;
        world.DiagnosticSink = e => { if (e.Kind == "chunk-generation") generated++; };
        world.AdvanceTicks(50); var after = world.ResidentJourneys;
        Assert.Equal(0, generated); Assert.Equal(before.TerrainReads, after.TerrainReads); Assert.Equal(before.Plans, after.Plans);
        Assert.Equal(before.CommittedSteps + 10, after.CommittedSteps); Assert.Equal(10, person.Position.Distance(position));
        Assert.InRange(after.RetainedSteps, 1, world.State.Actors.Count * WorldSession.ResidentJourneyLimit);
        Assert.InRange(world.Chunks.ResidentCount, 1, ChunkCache.Capacity);
    }
    [Fact]
    public void ChangedFutureTerrainAndAQueuedRouteSurviveSaveWithoutWalkingThroughTheWall()
    {
        using var world = Prepare(out var person, out var far); Leave(world, far);
        var blocked = person.Journey!.Steps[person.Journey.Next + 2]; world.Chunks.Edit(blocked, Material.Wall);
        string path = Path.Combine(_root, "wall.json"); world.Save(path); using var loaded = WorldSession.Load(path); loaded.Paused = false;
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        for (int i = 0; i < 180; i++)
        {
            var position = person.Position; world.AdvanceTicks(1); loaded.AdvanceTicks(1);
            Assert.NotEqual(blocked, person.Position); Assert.InRange(position.Distance(person.Position), 0, 1);
            if (i % 11 == 0 || i == 179) Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        Assert.True(person.Body.RestUntil > world.State.Tick); Assert.InRange(person.Position.Distance(person.Routine!.Target), 0, 1);
    }
    [Fact]
    public void DownedOccupantsBlockAQueuedStepAndOpeningTheRouteAllowsArrival()
    {
        using var world = Prepare(out var person, out var far); Leave(world, far); var position = person.Position;
        var blocker = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited && a.Id != person.Id);
        var exit = person.Journey!.Steps[person.Journey.Next];
        foreach (var (x, y) in WorldSession.Directions) world.Chunks.Edit(position.Offset(x, y), Material.Wall);
        world.Chunks.Edit(exit, Material.Floor); blocker.Position = exit; blocker.Health = 0; blocker.Life = LifeState.Downed;
        world.AdvanceTicks(15); Assert.Equal(position, person.Position); Assert.True(person.Routine!.Blocked); Assert.Empty(person.Journey!.Steps);
        blocker.Life = LifeState.Dead; world.AdvanceTicks(40);
        Assert.NotEqual(position, person.Position); Assert.False(person.Routine!.Blocked);
    }
    [Fact]
    public void EditingAnExitWakesAnEmptyRouteBeforeItsRetryDeadline()
    {
        using var world = Prepare(out var person, out var far); Leave(world, far); var position = person.Position;
        foreach (var (x, y) in WorldSession.Directions) world.Chunks.Edit(position.Offset(x, y), Material.Wall);
        world.AdvanceTicks(5); Assert.Empty(person.Journey!.Steps); Assert.True(person.Journey.RetryAt > world.State.Tick + 5);
        world.Chunks.Edit(position.Offset(-1, 0), Material.Floor); world.AdvanceTicks(5); Assert.NotEqual(position, person.Position);
    }
    [Fact]
    public void GroundSnapshotsMatchLiveMaterialsObjectsAndLandmarkOverlays()
    {
        using var world = WorldSession.Create(42); var person = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        var origin = world.State.Position; string before = JsonSerializer.Serialize(world.State);
        for (int y = -24; y <= 24; y += 4) for (int x = -24; x <= 24; x += 4)
        { var p = origin.Offset(x, y); Assert.Equal(world.GroundOpen(p), world.ResidentAnchorOpen(person, p)); }
        foreach (var d in world.State.Discoveries.Take(8))
            for (int y = -3; y <= 3; y++) for (int x = -3; x <= 3; x++)
            { var p = d.Position.Offset(x, y); Assert.Equal(world.GroundOpen(p), world.ResidentAnchorOpen(person, p)); }
        Assert.Equal(before, JsonSerializer.Serialize(world.State)); var cell = origin.Offset(3, 0);
        foreach (var material in Enum.GetValues<Material>())
        { world.Chunks.Edit(cell, material); Assert.Equal(world.GroundOpen(cell), world.ResidentAnchorOpen(person, cell)); }
        world.Chunks.Edit(cell, Material.Floor); Assert.True(world.ResidentAnchorOpen(person, cell));
        var cache = new LootCache { Id = "journey-blocker", Position = cell, Contents = new() { ["wood"] = 1 } }; world.State.Loot.Add(cache);
        Assert.False(world.ResidentAnchorOpen(person, cell)); cache.Taken = true; Assert.True(world.ResidentAnchorOpen(person, cell));
        cache.Taken = false; cache.Id = "work-return-999-0"; Assert.True(world.ResidentAnchorOpen(person, cell));
        var discovery = world.State.Discoveries[0]; cell = discovery.Position.Offset(1, 0); world.ResidentAnchorOpen(person, cell);
        world.State.Discoveries.Remove(discovery); Assert.Equal(world.GroundOpen(cell), world.ResidentAnchorOpen(person, cell));
        world.State.Discoveries.Add(discovery); Assert.Equal(world.GroundOpen(cell), world.ResidentAnchorOpen(person, cell));
    }
    [Fact]
    public void ExternalMovementAndRecruitmentDiscardTheOldRouteWithoutChangingIdentity()
    {
        using var world = Prepare(out var person, out _); var original = person.Position;
        person.Position = original.Offset(1, 0); Assert.Null(person.Journey); world.AdvanceTicks(5); Assert.NotNull(person.Journey);
        world.State.Position = world.Party.Single().Position = person.Position.Offset(1, 0);
        Assert.True(world.Submit(new(WorldCommandKind.Recruit, ActorId: person.Id)).Success);
        Assert.Null(person.Journey); Assert.Same(person, world.ActorById(person.Id));
        Assert.Single(world.State.Actors, a => a.Id == person.Id);
    }
    [Fact]
    public void OlderRoutineWithoutAJourneyCanPlanAndArriveOffscreen()
    {
        using var world = Prepare(out var person, out var far); Leave(world, far); string path = Path.Combine(_root, "older.json"); world.Save(path);
        var json = JsonNode.Parse(File.ReadAllText(path))!; var actor = json["state"]!["actors"]!.AsArray().First(a => a!["id"]!.GetValue<long>() == person.Id)!.AsObject();
        actor.Remove("journey"); File.WriteAllText(path, json.ToJsonString()); using var loaded = WorldSession.Load(path); loaded.Paused = false;
        var remote = loaded.ActorById(person.Id); Assert.Null(remote.Journey); loaded.AdvanceTicks(180);
        Assert.True(remote.Body.RestUntil > loaded.State.Tick); Assert.InRange(remote.Position.Distance(remote.Routine!.Target), 0, 1);
    }
    [Theory]
    [InlineData("next")] [InlineData("long")] [InlineData("disconnected")] [InlineData("origin")] [InlineData("floor")]
    [InlineData("retry")] [InlineData("revision")] [InlineData("position")] [InlineData("null")]
    [InlineData("world-revision")] [InlineData("repeated")] [InlineData("radius")]
    public void MalformedJourneyStateIsRejectedWithoutRewritingTheSave(string field)
    {
        using var world = Prepare(out var person, out _); string path = Path.Combine(_root, "bad.json"); world.Save(path);
        var json = JsonNode.Parse(File.ReadAllText(path))!; var actor = json["state"]!["actors"]!.AsArray().First(a => a!["id"]!.GetValue<long>() == person.Id)!;
        var journey = actor["journey"]!;
        switch (field)
        {
            case "next": journey["next"] = -1; break;
            case "long": for (int i = 0; i < 65; i++) journey["steps"]!.AsArray().Add(journey["origin"]!.DeepClone()); break;
            case "disconnected": journey["steps"]![0]!["x"] = 0; break;
            case "origin": journey["origin"]!["x"] = -1; break;
            case "floor": journey["target"]!["floor"] = 1; break;
            case "retry": journey["retryAt"] = world.State.Tick + 26; break;
            case "revision": journey["revision"] = world.State.NavigationRevision + 1; break;
            case "position": actor["position"]!["x"] = 0; break;
            case "null": journey["steps"] = null; break;
            case "world-revision": json["state"]!["navigationRevision"] = -1; break;
            case "repeated": journey["steps"]![1] = journey["origin"]!.DeepClone(); break;
            case "radius":
                journey["next"] = 0; journey["steps"] = new JsonArray(); journey["origin"] = actor["position"]!.DeepClone();
                for (int i = 1; i <= 25; i++) { var p = actor["position"]!.DeepClone(); p["x"] = person.Position.X + i; journey["steps"]!.AsArray().Add(p); } break;
        }
        File.WriteAllText(path, json.ToJsonString()); string before = File.ReadAllText(path);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(path)); Assert.Equal(before, File.ReadAllText(path));
    }
}
