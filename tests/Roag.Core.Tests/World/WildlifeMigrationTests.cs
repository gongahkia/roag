using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class WildlifeMigrationTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-migration-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    private static WorldSession Prepare(out Actor actor)
    {
        var world = WorldSession.Create(42); var fixture = MigrationPreview.Prepare(world); actor = world.ActorById(fixture.Actor);
        world.Paused = false; return world;
    }
    private static void Target(WorldSession world, Actor actor)
    {
        for (int i = 0; i < 1000 && actor.Migration?.Target == null; i++) Assert.Equal(1, world.AdvanceTicks(1));
        Assert.NotNull(actor.Migration?.Target);
    }
    private static void Arrive(WorldSession world, Actor actor)
    {
        var origin = actor.Habitat; int population = world.State.Actors.Count;
        for (int i = 0; i < 3000 && actor.Habitat == origin; i++)
        {
            var before = actor.Position; long visits = world.WildlifeMigrationCandidates;
            Assert.Equal(1, world.AdvanceTicks(1)); Assert.InRange(before.Distance(actor.Position), 0, 1);
            Assert.InRange(world.WildlifeMigrationCandidates - visits, 0, WorldSession.WildlifeSearchBudget);
        }
        Assert.NotEqual(origin, actor.Habitat); Assert.Null(actor.Migration); Assert.Equal(population, world.State.Actors.Count);
    }
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void NaturalSeededPreviewMigratesTheOriginalAnimal(int seed)
    { Assert.True(JsonSerializer.SerializeToNode(MigrationPreview.Run(ContentCatalog.Default, seed))!["passed"]!.GetValue<bool>()); }
    [Fact]
    public void LocalSearchEnumerationIsUniqueCardinalByRadiusAndFinite()
    {
        var origin = new WorldPosition(2048, 2048); var cells = Enumerable.Range(0, WorldSession.WildlifeSearchCells).Select(i => WorldSession.WildlifeSearchCell(origin, i)).ToArray();
        Assert.Equal(cells.Length, cells.Distinct().Count()); Assert.Equal(origin, cells[0]);
        Assert.All(cells, p => Assert.InRange(p.Distance(origin), 0, 96));
        Assert.Equal(cells.Select(p => p.Distance(origin)).Order(), cells.Select(p => p.Distance(origin)));
    }
    [Fact]
    public void HungryAnimalsSeekFoodButAHealthyHabitatDoesNotTriggerAMove()
    {
        using var world = Prepare(out var actor); actor.Migration = null; actor.WildlifeAfter = 0; actor.Journey = null;
        var habitat = world.State.Habitats[WorldSession.HabitatIndex(actor.Habitat!.Value.Chunk)]; habitat.Pressure = 0;
        actor.Body.Hunger = 40; world.AdvanceTicks(5);
        Assert.Equal(MigrationReason.Food, actor.Migration!.Reason);
        using var healthy = Prepare(out var other); other.Migration = null; other.WildlifeAfter = 0; other.Journey = null;
        var food = healthy.State.Habitats[WorldSession.HabitatIndex(other.Habitat!.Value.Chunk)]; food.Food = 8; food.Pressure = 0;
        healthy.AdvanceTicks(20); Assert.Null(other.Migration);
    }
    [Fact]
    public void DestinationReservationsRejectDuplicatesAndDoNotSpawnAnotherAnimalOnObservation()
    {
        using var world = Prepare(out var actor); Target(world, actor); var target = actor.Migration!.Target!.Value;
        world.State.Position = world.Party.Single().Position = target.Offset(7, 5); world.Reveal();
        Assert.Single(world.State.Actors, a => a.Life != LifeState.Dead && (a.Habitat?.Chunk == target.Chunk || a.Migration?.Target?.Chunk == target.Chunk));
        string path = Path.Combine(_root, "reserved.json"); world.Save(path); var json = JsonNode.Parse(File.ReadAllText(path))!;
        var animals = json["state"]!["actors"]!.AsArray(); var original = animals.First(a => a!["id"]!.GetValue<long>() == actor.Id)!;
        var copy = original.DeepClone(); copy["id"] = world.State.NextActorId; copy["journey"] = null;
        var otherHome = actor.Habitat!.Value.Offset(64, 0);
        copy["habitat"]!["x"] = otherHome.X; copy["natalHabitat"] = copy["habitat"]!.DeepClone(); copy["migration"]!["origin"] = copy["habitat"]!.DeepClone();
        json["state"]!["ecologyChunks"]!.AsArray().Add(new JsonObject { ["x"] = otherHome.Chunk.X, ["y"] = otherHome.Chunk.Y, ["floor"] = otherHome.Floor });
        animals.Add(copy); json["state"]!["nextActorId"] = world.State.NextActorId + 1;
        File.WriteAllText(path, json.ToJsonString()); Assert.Throws<InvalidDataException>(() => WorldSession.Load(path));
    }
    [Fact]
    public void ASharedBudgetBoundsAllSimultaneousSearchesAndInspectionDoesNotPlan()
    {
        using var world = Prepare(out _);
        foreach (var actor in world.State.Actors.Where(a => a.Habitat != null))
        {
            actor.Body.RestUntil = 0; actor.WildlifeAfter = 0;
            world.State.Habitats[WorldSession.HabitatIndex(actor.Habitat!.Value.Chunk)] = new() { Food = 0, Pressure = 800 };
        }
        for (int tick = 0; tick < 150; tick++)
        {
            long before = world.WildlifeMigrationCandidates; world.AdvanceTicks(1);
            Assert.InRange(world.WildlifeMigrationCandidates - before, 0, 128);
        }
        string saved = JsonSerializer.Serialize(world.State); long candidates = world.WildlifeMigrationCandidates;
        foreach (var actor in world.State.Actors.Where(a => a.Habitat != null)) _ = world.WildlifeDescription(actor);
        Assert.Equal(saved, JsonSerializer.Serialize(world.State)); Assert.Equal(candidates, world.WildlifeMigrationCandidates);
    }
    [Fact]
    public void MigrationChangesHomeWithoutCloningIdentityEquipmentCargoOrRevealingTheDestination()
    {
        using var world = Prepare(out var actor); long id = actor.Id; string name = actor.Name, archetype = actor.Archetype;
        actor.Cargo["food"] = 3; var original = actor.Habitat; string equipment = JsonSerializer.Serialize(actor.Equipment);
        Target(world, actor); var target = actor.Migration!.Target!.Value; bool known = world.Chunks.Seen(target);
        Assert.False(known); Assert.NotEqual(original!.Value.Chunk, target.Chunk); Arrive(world, actor);
        Assert.Equal(id, actor.Id); Assert.Equal(name, actor.Name); Assert.Equal(archetype, actor.Archetype); Assert.Equal(equipment, JsonSerializer.Serialize(actor.Equipment));
        Assert.Equal(3, actor.Cargo["food"]); Assert.Equal(original, actor.NatalHabitat); Assert.False(world.Chunks.Seen(target));
        Assert.Equal(target, actor.Habitat); Assert.Single(world.State.Actors, a => a.Id == id);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SearchAndTravelHaveExactSavedContinuation(bool travelling)
    {
        using var world = Prepare(out var actor); if (travelling) Target(world, actor);
        string path = Path.Combine(_root, "migration.json"); world.Save(path); using var loaded = WorldSession.Load(path); loaded.Paused = false;
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        for (int i = 0; i < 250; i++)
        {
            world.AdvanceTicks(1); loaded.AdvanceTicks(1);
            if (i % 13 == 0 || i == 249) Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
    }
    [Fact]
    public void LeavingTheRegionKeepsTheSameSearchRouteAndArrival()
    {
        using var near = Prepare(out var actor); Target(near, actor);
        var field = JourneyPreview.PrepareFarField(near);
        near.State.Position = near.Party.Single().Position = actor.Position.Offset(35, 35); near.Chunks.Edit(near.State.Position, Material.Floor);
        string path = Path.Combine(_root, "far.json"); near.Save(path); using var far = WorldSession.Load(path); far.Paused = false;
        far.State.Position = far.Party.Single().Position = field; var remote = far.ActorById(actor.Id);
        var origin = actor.Habitat;
        for (int i = 0; i < 1000 && actor.Habitat == origin; i++)
        {
            near.AdvanceTicks(1); far.AdvanceTicks(1); Assert.Equal(JsonSerializer.Serialize(actor), JsonSerializer.Serialize(remote));
        }
        Assert.NotEqual(origin, actor.Habitat); Assert.Equal(actor.Habitat, remote.Habitat);
    }
    [Fact]
    public void WarmOffscreenMigrationStepsReuseTheRouteWithoutReloadingTerrain()
    {
        using var world = Prepare(out var actor); Target(world, actor); var field = JourneyPreview.PrepareFarField(world);
        world.State.Position = world.Party.Single().Position = field;
        foreach (var caravan in world.State.Actors.Where(a => a.Kind == ActorKind.Caravan))
        { caravan.Traveling = false; caravan.Route.Clear(); caravan.NextTravelTick = world.State.Tick + 1000; }
        world.AdvanceTicks(5); var before = world.ResidentJourneys; int generated = 0;
        world.DiagnosticSink = e => { if (e.Kind == "chunk-generation") generated++; };
        world.AdvanceTicks(50); var after = world.ResidentJourneys;
        Assert.Equal(before.TerrainReads, after.TerrainReads); Assert.Equal(before.Plans, after.Plans); Assert.Equal(0, generated);
        Assert.Equal(before.CommittedSteps + 10, after.CommittedSteps); Assert.NotNull(actor.Migration);
    }
    [Fact]
    public void BlockedTravelRetainsTheAnimalAndAReopenedExitResumesWithoutTeleporting()
    {
        using var world = Prepare(out var actor); Target(world, actor); var position = actor.Position;
        foreach (var (x, y) in WorldSession.Directions) world.Chunks.Edit(position.Offset(x, y), Material.Wall);
        world.AdvanceTicks(40); Assert.Equal(position, actor.Position); Assert.NotNull(actor.Migration); Assert.Contains("blocked", world.WildlifeDescription(actor));
        var target = actor.Migration!.Target!.Value;
        var exit = WorldSession.Directions.Select(d => position.Offset(d.X, d.Y)).MinBy(p => p.Distance(target)); world.Chunks.Edit(exit, Material.Floor);
        Arrive(world, actor);
    }
    [Fact]
    public void ClearedTargetsAreReconsideredInsteadOfInstalledAsNewHomes()
    {
        using var world = Prepare(out var actor); Target(world, actor); var target = actor.Migration!.Target!.Value;
        world.Chunks.Edit(target, Material.Wall); world.AdvanceTicks(5); Assert.NotEqual(target, actor.Migration?.Target);
        Arrive(world, actor); Assert.NotEqual(target, actor.Habitat);
    }
    [Fact]
    public void FleeingAndRestTakePriorityWhileMigrationIntentSurvives()
    {
        using var world = Prepare(out var actor); Target(world, actor); var migration = actor.Migration;
        actor.Body.RestUntil = world.State.Tick + 50; var position = actor.Position; world.AdvanceTicks(20);
        Assert.Equal(position, actor.Position); Assert.Same(migration, actor.Migration); actor.Body.RestUntil = 0;
        world.State.Position = world.Party.Single().Position = position.Offset(1, 0); world.Chunks.Edit(world.State.Position, Material.Floor);
        foreach (var d in WorldSession.Directions) world.Chunks.Edit(position.Offset(d.X, d.Y), Material.Floor);
        world.AdvanceTicks(5); Assert.True(actor.Position.Distance(world.State.Position) > 1); Assert.False(world.IsInCombat); Assert.Same(migration, actor.Migration);
        world.State.Position = world.Party.Single().Position = position.Offset(40, 40); world.Chunks.Edit(world.State.Position, Material.Floor); Arrive(world, actor);
    }
    [Fact]
    public void RealClearingInterruptsAResourceDefenderAndDeathClearsAPlannedTrip()
    {
        using var world = WorldSession.Create(42); var animal = WildlifePreview.Prepare(world, "character.bee"); var home = animal.Habitat!.Value;
        animal.Position = home.Offset(5, 0); world.Chunks.Edit(animal.Position, Material.Floor); animal.Body.RestUntil = 1000;
        world.State.Position = world.Party.Single().Position = home.Offset(1, 0); world.Reveal();
        Assert.True(world.Submit(new(WorldCommandKind.Dig, home)).Success); world.Paused = false; world.AdvanceTicks(25);
        Assert.Equal(Material.Earth, world.Chunks.At(home)); Assert.Equal(MigrationReason.HomeUnavailable, animal.Migration!.Reason);
        // Hunting still uses the original battle rules while the animal rests.
        var hero = world.Party.Single(); hero.Role = 0; hero.BaseSpeed = 30; hero.BasePower = 100;
        world.StartBattle(animal.Id); Assert.True(world.Act(0, 1).Success); Assert.Equal(LifeState.Dead, animal.Life);
        Assert.Null(animal.Migration); Assert.Null(animal.Journey); Assert.Single(world.State.Loot, c => c.SourceActor == animal.Id);
        string path = Path.Combine(_root, "dead.json"); world.Save(path); using var loaded = WorldSession.Load(path); Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
    }
    [Theory]
    [InlineData("cursor")] [InlineData("reason")] [InlineData("origin")] [InlineData("center")] [InlineData("target")]
    [InlineData("since")] [InlineData("progress")] [InlineData("natal")] [InlineData("deadline")] [InlineData("foreign")]
    public void ForgedMigrationStateIsRejectedWithoutRewritingTheSlot(string field)
    {
        using var world = Prepare(out var actor); Target(world, actor); string path = Path.Combine(_root, "bad.json"); world.Save(path);
        var json = JsonNode.Parse(File.ReadAllText(path))!; var people = json["state"]!["actors"]!.AsArray();
        var animal = people.First(a => a!["id"]!.GetValue<long>() == actor.Id)!; var migration = animal["migration"]!;
        switch (field)
        {
            case "cursor": migration["cursor"] = WorldSession.WildlifeSearchCells + 1; break;
            case "reason": migration["reason"] = 999; break;
            case "origin": migration["origin"]!["x"] = 0; break;
            case "center": migration["center"]!["floor"] = 1; break;
            case "target": migration["target"]!["x"] = 0; break;
            case "since": migration["since"] = world.State.Tick + 1; break;
            case "progress": migration["lastProgressTick"] = world.State.Tick + 1; break;
            case "natal": animal["natalHabitat"]!["floor"] = 1; break;
            case "deadline": animal["wildlifeAfter"] = world.State.Tick + WorldSession.TicksPerHour + 1; break;
            case "foreign": people.First(a => a!["kind"]!.GetValue<int>() == (int)ActorKind.Person)!["migration"] = migration.DeepClone(); break;
        }
        File.WriteAllText(path, json.ToJsonString()); string before = File.ReadAllText(path);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(path)); Assert.Equal(before, File.ReadAllText(path));
    }
}
