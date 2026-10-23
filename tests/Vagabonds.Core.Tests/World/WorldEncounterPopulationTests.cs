using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class WorldEncounterPopulationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-population-" + Guid.NewGuid().ToString("N"));
    private string Slot { get { Directory.CreateDirectory(_directory); return Path.Combine(_directory, "world.json"); } }
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static string State(WorldSession world) => JsonSerializer.Serialize(world.State);

    [Fact]
    public void EveryContextAndBothVisitationOrdersPassTheActualPopulationAudit()
    {
        var report = EncounterPopulationAudit.Run(ContentCatalog.Default, [42]);
        Assert.True(report.Passed, JsonSerializer.Serialize(report));
        Assert.Equal(72, report.Contexts.Length); Assert.Equal(144, report.Groups.Length);
        Assert.Equal(0, report.AvoidableRepetitions); Assert.True(report.Distribution.Count >= 6);
    }

    [Fact]
    public void PlanningAndInspectionDoNotGenerateTerrainActorsOrSpendRandomness()
    {
        var world = WorldSession.Create(42); string before = State(world); int footprints = world.Chunks.Generator.CachedFootprints;
        var plans = world.Plan.Sites.SelectMany(s => world.EncounterPopulationPlan(s.Id)).ToArray();
        Assert.Equal(144, plans.Length); Assert.Equal(before, State(world)); Assert.Equal(footprints, world.Chunks.Generator.CachedFootprints);
        Assert.Empty(world.State.EncounterPopulations); Assert.DoesNotContain(world.State.Actors, a => a.Id is >= 70000 and < 80000);
        Assert.All(plans, p => Assert.True(world.Content.Get<EncounterDefinition>(p.EncounterId).Fits(p.Purpose, p.Biome, p.Floor)));
        world.Party.First().Level = 40;
        Assert.Equal(JsonSerializer.Serialize(plans), JsonSerializer.Serialize(world.Plan.Sites.SelectMany(s => world.EncounterPopulationPlan(s.Id))));
    }

    [Theory]
    [InlineData(1337, 0, 1)] [InlineData(-42, 2, 2)] [InlineData(2026, 3, 3)]
    public void MaterializedGroupsAreFixedConnectedClearAndPersistent(int seed, int site, int floor)
    {
        var world = WorldSession.Create(seed); uint combat = world.State.CombatRandom, incident = world.State.IncidentRandom;
        string terrain = JsonSerializer.Serialize(world.State.Chunks);
        var result = world.EnsureEncounterPopulation(site, floor);
        Assert.True(result.Record.Spawned, result.Record.Failure); Assert.True(result.Created);
        Assert.All(result.Positions, p => Assert.True(world.GroundOpen(p)));
        Assert.Equal(result.Positions.Length, result.Positions.Distinct().Count());
        Assert.All(result.Positions, p => Assert.All(result.Positions, other => Assert.True(p.Distance(other) <= 8)));
        Assert.Equal(combat, world.State.CombatRandom); Assert.Equal(incident, world.State.IncidentRandom); Assert.Equal(0, world.State.Tick);
        Assert.Equal(terrain, JsonSerializer.Serialize(world.State.Chunks));
        string once = State(world); Assert.False(world.EnsureEncounterPopulation(site, floor).Created); Assert.Equal(once, State(world));
        world.Save(Slot); var loaded = WorldSession.Load(Slot); Assert.Equal(State(world), State(loaded));
        Assert.False(loaded.EnsureEncounterPopulation(site, floor).Created); Assert.Equal(State(world), State(loaded));
        foreach (long id in result.Actors) { var actor = loaded.State.Actors.Single(a => a.Id == id); actor.Life = LifeState.Dead; actor.Health = 0; }
        string defeated = State(loaded); Assert.False(loaded.EnsureEncounterPopulation(site, floor).Created); Assert.Equal(defeated, State(loaded));
    }

    [Fact]
    public void FailedPlacementRecordsTheWholeFailureWithoutPartialSpawnsOrRepeatedAttempts()
    {
        var world = WorldSession.Create(42); var site = world.Plan.Sites[0]; const int floor = 1;
        for (int y = -32; y < 32; y++) for (int x = -32; x < 32; x++)
            if (y != 0 || x is not (-2 or 2)) world.Chunks.Edit(site.Center.Offset(x, y) with { Floor = floor }, Material.Wall);
        var ids = world.State.Actors.Select(a => a.Id).ToArray(); uint random = world.State.CombatRandom;
        var result = world.EnsureEncounterPopulation(site.Id, floor);
        Assert.False(result.Record.Spawned); Assert.NotEmpty(result.Record.Failure); Assert.Empty(result.Actors);
        Assert.Equal(ids, world.State.Actors.Select(a => a.Id)); Assert.Equal(random, world.State.CombatRandom);
        string before = State(world); Assert.False(world.EnsureEncounterPopulation(site.Id, floor).Created); Assert.Equal(before, State(world));
        world.Save(Slot); var restored = WorldSession.Load(Slot); Assert.Equal(State(world), State(restored));
    }

    [Fact]
    public void ActualStairTravelActivatesTheFloorWhileRevealAndLoadDoNot()
    {
        var world = WorldSession.Create(42); var site = world.Plan.Sites.First(s => !world.State.Actors.Any(a => a.EncounterLeader == a.Id && a.Position.Floor == 1
            && a.Position.Distance(s.Center with { Floor = 1 }) < 10));
        var hero = world.Party.First(); hero.Position = world.State.Position = site.Center.Offset(1, 0);
        world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal(); Assert.Empty(world.State.EncounterPopulations);
        var travel = world.Submit(new(WorldCommandKind.Interact, site.Center.Offset(2, 0)));
        Assert.True(travel.Success, travel.Message); Assert.Single(world.State.EncounterPopulations); Assert.Equal(1, world.State.Position.Floor);
        hero.Position = world.State.Position = site.Center.Offset(-2, 0) with { Floor = 2 };
        world.Reveal(); Assert.Single(world.State.EncounterPopulations);
        world.Save(Slot); var loaded = WorldSession.Load(Slot); Assert.Single(loaded.State.EncounterPopulations);
        Assert.Equal(State(world), State(loaded));
    }

    [Theory]
    [InlineData("definition")] [InlineData("floor")] [InlineData("duplicate")]
    [InlineData("remove-record")] [InlineData("failed-with-members")] [InlineData("missing-member")]
    public void ForgedPopulationRecordsCannotChangeSeededCompositionOrRespawnMembers(string invalid)
    {
        var world = WorldSession.Create(42); var result = world.EnsureEncounterPopulation(0, 1); Assert.True(result.Record.Spawned);
        world.Save(Slot); var document = JsonNode.Parse(File.ReadAllText(Slot))!; var state = document["state"]!;
        var records = state["encounterPopulations"]!.AsArray(); var first = records[0]!;
        switch (invalid)
        {
            case "definition": first["encounterId"] = "encounter.missing"; break;
            case "floor": first["floor"] = 0; break;
            case "duplicate": records.Add(first.DeepClone()); break;
            case "remove-record": records.Clear(); break;
            case "failed-with-members": first["spawned"] = false; first["failure"] = "Blocked"; break;
            case "missing-member":
                var actors = state["actors"]!.AsArray(); actors.Remove(actors.Single(a => a!["id"]!.GetValue<long>() == result.Actors[^1])); break;
        }
        File.WriteAllText(Slot, document.ToJsonString()); Assert.Throws<InvalidDataException>(() => WorldSession.Load(Slot));
    }
}
