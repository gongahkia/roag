using System.Text.Json;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class ClimateTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "vagabonds-climate-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_directory)) Directory.Delete(_directory, true); }
    private static long Find(WorldSession world, WorldPosition p, Func<ClimateConditions, bool> predicate, long minimum = 0)
    {
        for (long epoch = minimum / WorldSession.WeatherPeriod; epoch < minimum / WorldSession.WeatherPeriod + 480; epoch++)
        { long tick = epoch * WorldSession.WeatherPeriod + 10; if (tick >= minimum && predicate(WorldSession.ClimateAt(world.Plan, p, tick))) return tick; }
        throw new InvalidDataException("Requested climate absent from this surface region.");
    }
    private static string State(WorldSession world) => JsonSerializer.Serialize(world.State);
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void RegisteredPreviewBuildsRealCoverAndContinuesThroughAnActualWeatherChange(int seed)
    { Assert.True(JsonSerializer.SerializeToNode(WeatherPreview.Run(Roag.Core.Content.ContentCatalog.Default, seed))!["passed"]!.GetValue<bool>()); }
    [Theory]
    [InlineData(0, WorldSeason.Spring)] [InlineData(29, WorldSeason.Spring)] [InlineData(30, WorldSeason.Summer)]
    [InlineData(60, WorldSeason.Autumn)] [InlineData(90, WorldSeason.Winter)] [InlineData(120, WorldSeason.Spring)]
    public void CalendarUsesThirtyDaySeasonsAndRepeatsAtOneHundredTwentyDays(int day, WorldSeason season)
    {
        using var world = WorldSession.Create(42); world.State.Tick = day * WorldSession.TicksPerDay;
        var weather = world.WeatherAt(world.State.Position); Assert.Equal(season, weather.Season);
        Assert.Equal(world.State.Tick + WorldSession.WeatherPeriod, weather.Until);
    }
    [Theory]
    [InlineData(42)] [InlineData(1337)] [InlineData(-42)]
    public void RegionalQueriesStayPureBoundedAndIndependentOfObservationAndRandomStreams(int seed)
    {
        using var world = WorldSession.Create(seed); world.State.Tick = 1_000_000_000_000;
        string before = State(world); int chunks = world.Chunks.ResidentCount; var kinds = new HashSet<WeatherKind>();
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++)
        {
            var p = new WorldPosition(x * 512 + 256, y * 512 + 256); var first = world.WeatherAt(p); kinds.Add(first.Weather);
            Assert.Equal(first, world.WeatherAt(p.Offset(100, 100))); Assert.Equal(first, WorldSession.ClimateAt(world.Plan, p, world.State.Tick));
            var underground = world.WeatherAt(p with { Floor = 2 }); Assert.True(underground.Underground); Assert.False(underground.Severe);
            Assert.Equal(100, underground.GatheringPercent); Assert.Equal(100, underground.ExposedRestPercent); Assert.Equal(0, underground.TravelFatigue);
            Assert.Equal(first, world.WeatherAt(p)); _ = world.ShelteredAt(p); _ = world.WeatherDescription(p);
        }
        Assert.True(kinds.Count > 1); Assert.Equal(before, State(world)); Assert.Equal(chunks, world.Chunks.ResidentCount);
        long bytes = WarmAllocationProbe.Measure(() => { _ = world.WeatherAt(world.State.Position); _ = world.ShelteredAt(world.State.Position); }); Assert.InRange(bytes, 0, 1024);
    }
    [Fact]
    public void TravelEffortIsChargedOnlyForActualExposedStepsAndCampCoverProtectsIt()
    {
        using var world = WorldCollisionTests.Arena(); var hero = world.Party.Single(); var origin = world.State.Position;
        world.State.Tick = Find(world, origin, c => c.Weather == WeatherKind.Wind); hero.Body.Fatigue = 0;
        Assert.True(world.Submit(new(WorldCommandKind.Move, origin.Offset(1, 0))).Success); Assert.Equal(.02, hero.Body.Fatigue);
        string before = State(world); Assert.False(world.Submit(new(WorldCommandKind.Move, origin.Offset(4, 0))).Success); Assert.Equal(before, State(world));
        world.Chunks.Edit(origin.Offset(2, -1), Material.Camp);
        Assert.True(world.Submit(new(WorldCommandKind.Move, origin.Offset(2, 0))).Success); Assert.Equal(.02, hero.Body.Fatigue); Assert.True(world.Sheltered(hero));
        world.Chunks.Edit(origin.Offset(2, -1), Material.Floor); Assert.False(world.Sheltered(hero));
    }
    [Fact]
    public void CarriedDownedCompanionsDoNotPayWalkingEffort()
    {
        using var world = WorldCollisionTests.Arena(); var companion = world.State.Actors.First(a => a.Kind == ActorKind.Person && !a.Recruited);
        companion.Position = world.State.Position.Offset(0, 1); Assert.True(world.Submit(new(WorldCommandKind.Recruit, ActorId: companion.Id)).Success);
        companion.Body.Hunger = 30; companion.Body.Fatigue = 20; companion.Life = LifeState.Downed; companion.Health = 0;
        world.State.Tick = Find(world, world.State.Position, c => c.Weather == WeatherKind.Wind);
        string body = JsonSerializer.Serialize(companion.Body); var old = companion.Position;
        Assert.True(world.Submit(new(WorldCommandKind.Move, world.State.Position.Offset(1, 0))).Success);
        Assert.NotEqual(old, companion.Position); Assert.Equal(body, JsonSerializer.Serialize(companion.Body));
    }
    [Theory]
    [InlineData(WeatherKind.Clear, 100)] [InlineData(WeatherKind.Wind, 90)]
    public void ActualGatheringUsesTheCurrentWeatherAndStillConservesItsOutput(WeatherKind kind, int percent)
    {
        using var world = WorldCollisionTests.Arena(); world.State.Tick = Find(world, world.State.Position, c => c.Weather == kind);
        Assert.True(world.Submit(new(WorldCommandKind.Forage)).Success); var order = Assert.Single(world.State.Work); var hero = world.Party.Single();
        world.Paused = false; world.AdvanceTicks(1); Assert.Equal(world.WorkRate(hero) * percent / 100, order.Progress, 10);
        for (int i = 0; i < 150 && world.State.Work.Count > 0; i++) world.AdvanceTicks(1);
        Assert.Empty(world.State.Work); Assert.Equal(2, order.Output!.Delivered["food"]);
    }
    [Fact]
    public void ExhaustedResourceKeepsItsActualRecoveryDeadlineAcrossWeatherChangesAndReload()
    {
        using var world = WorldSession.Create(42); var fixture = ConsequencePreview.Prepare(world); var node = world.ResourceAt(fixture.Source)!;
        world.Chunks.Delta(fixture.Source.Chunk).Resources[fixture.Source.LocalIndex] = new(node.Definition.Id, 1, 0);
        world.State.Tick = Find(world, fixture.Source, c => c.Weather == WeatherKind.Wind, world.State.Tick);
        Assert.True(world.Submit(new(WorldCommandKind.Harvest, fixture.Source)).Success); world.Paused = false;
        for (int i = 0; i < 150 && world.State.Work.Count > 0; i++) world.AdvanceTicks(1);
        var stock = world.Chunks.ResourceStockAt(fixture.Source)!; Assert.Equal(0, stock.Remaining); Assert.Equal(110, stock.RecoveryPercent);
        Assert.Equal(stock.RecoveryStarted + (node.Definition.RegrowTicks * 110L + 99) / 100, stock.RecoverAt);
        string path = Path.Combine(_directory, "resource.json"); world.Save(path); using var restored = WorldSession.Load(path);
        Assert.Equal(State(world), State(restored)); Assert.Equal(stock, restored.Chunks.ResourceStockAt(fixture.Source));
        world.State.Tick = restored.State.Tick = stock.RecoverAt - 1; Assert.Equal(0, world.ResourceAt(fixture.Source)!.Available);
        world.State.Tick = restored.State.Tick = stock.RecoverAt; Assert.Equal(node.Definition.Capacity, world.ResourceAt(fixture.Source)!.Available);
        Assert.Equal(stock, restored.Chunks.ResourceStockAt(fixture.Source));
    }
    [Theory]
    [InlineData(0)] [InlineData(199)] [InlineData(600)] [InlineData(1000)]
    public void BoundedElapsedRecoveryMatchesAnHourlyReferenceAcrossWeatherAndSeasons(int initialPressure)
    {
        using var world = WorldSession.Create(42); var p = world.State.Position; var view = world.HabitatAt(p); int capacity = view.Capacity;
        var saved = new HabitatState { RecoveryVersion = 1, Food = 0, Pressure = initialPressure, Epoch = 0 };
        world.State.Habitats[WorldSession.HabitatIndex(p.Chunk)] = saved;
        int food = 0, pressure = initialPressure, rate = Math.Max(1, capacity / 8);
        for (int hour = 1; hour <= 2400; hour++)
        {
            world.State.Tick = hour * WorldSession.TicksPerHour; pressure = Math.Max(0, pressure - 20);
            var center = new WorldPosition(p.Chunk.X * 64 + 32, p.Chunk.Y * 64 + 32);
            int growth = Math.Max(1, rate * 100 / world.WeatherAt(center).RecoveryPercent);
            food = Math.Min(capacity, food + (pressure >= 600 ? 0 : pressure >= 200 ? Math.Max(1, growth / 2) : growth));
            var actual = world.HabitatAt(p); Assert.Equal(food, actual.Food); Assert.Equal(pressure, actual.Pressure);
        }
        Assert.Equal(0, saved.Food); Assert.Equal(0, saved.Epoch); world.State.Tick = 1_000_000_000_000;
        Assert.Equal(capacity, world.HabitatAt(p).Food);
        Assert.InRange(WarmAllocationProbe.Measure(() => _ = world.HabitatAt(p)), 0, 1024);
        saved.Epoch = world.State.Tick / WorldSession.TicksPerHour; saved.Food = 1; Assert.Equal(1, world.HabitatAt(p).Food);
        saved.Food = 2; Assert.Equal(2, world.HabitatAt(p).Food);
    }
    [Fact]
    public void DistantAndFullCadenceRestMatchAcrossAWeatherBoundaryAndCampConstruction()
    {
        using var world = WorldCollisionTests.Arena(); var actor = world.State.Actors.First(a => a.Kind == ActorKind.Person && a.HomeTown == 10);
        long rainy = Find(world, actor.Position, c => c.ExposedRestPercent < 100, WorldSession.WeatherPeriod);
        world.State.Tick = rainy - 11; actor.Body.Fatigue = 60; actor.Body.Hunger = 0; actor.Body.RestUntil = world.State.Tick + 500;
        world.Paused = false; world.AdvanceTicks(1); string path = Path.Combine(_directory, "rest.json"); world.Save(path);
        using var detailed = WorldSession.Load(path); detailed.DetailedBodiesEverywhere = true; detailed.Paused = false;
        for (int i = 0; i < 180; i++)
        {
            if (i == 13) foreach (var w in new[] { world, detailed }) w.Chunks.Edit(actor.Position.Offset(0, 1), Material.Camp);
            Assert.Equal(1, world.AdvanceTicks(1)); Assert.Equal(1, detailed.AdvanceTicks(1));
            Assert.Equal(State(world), State(detailed));
        }
    }
    [Fact]
    public void MovingRestingCaravansKeepExactRatesAcrossRegionalWeatherAndReload()
    {
        var report = BodyScheduleAudit.Run(Roag.Core.Content.ContentCatalog.Default, [42], 4200);
        Assert.True(report.Passed); Assert.True(Assert.Single(report.Seeds).SaveContinued);
        Assert.All(report.Seeds[0].Checkpoints, point => Assert.Equal(point.ScheduledHash, point.DetailedHash));
    }
    [Fact]
    public void SevereWeatherInterruptsResidentsWithoutBlameAndTheyActuallyWalkToShelter()
    {
        using var world = WorldCollisionTests.Arena(); var person = world.State.Actors.First(a => a.Kind == ActorKind.Person && a.HomeTown == 0 && a.Job == "guard");
        person.Position = world.State.Position.Offset(4, 0); person.Body.Hunger = person.Body.Fatigue = 0;
        world.State.Tick = Find(world, person.Position, c => c.Severe); world.Paused = false;
        Assert.True(world.Submit(new(WorldCommandKind.Build, world.State.Position.Offset(1, 0), Material: Material.Wall)).Success);
        var order = Assert.Single(world.State.Work); order.Required = 1000;
        Assert.True(world.Submit(new(WorldCommandKind.AssignWork, ActorId: person.Id, WorkId: order.Id)).Success);
        world.AdvanceTicks(5); Assert.Equal(RoutineKind.Shelter, person.Routine!.Kind); Assert.False(world.Sheltered(person));
        var start = person.Position;
        for (int i = 0; i < 500 && !world.Sheltered(person); i++) { var before = person.Position; world.AdvanceTicks(1); Assert.InRange(before.Distance(person.Position), 0, 1); }
        Assert.True(world.Sheltered(person)); Assert.NotEqual(start, person.Position); Assert.Empty(person.Consequences);
        Assert.Equal(person.Id, order.Worker); Assert.Equal(0, order.Progress); Assert.Contains("Sheltering", order.Status);
    }
    [Theory]
    [InlineData(WorldSeason.Spring, 100)] [InlineData(WorldSeason.Summer, 125)]
    [InlineData(WorldSeason.Autumn, 125)] [InlineData(WorldSeason.Winter, 50)]
    public void SeasonalFoodYieldChangesActualRegionalProductionAndItsAccounting(WorldSeason season, int percent)
    {
        using var world = WorldSession.Create(42); var farmer = world.State.Actors.First(a => a.Job == "farmer" && a.HomeTown == 10);
        foreach (var actor in world.State.Actors.Where(a => !a.Recruited && a != farmer)) { actor.Life = LifeState.Dead; actor.Health = 0; }
        farmer.Body.Fatigue = farmer.Body.Hunger = 0; farmer.Body.RestUntil = 0;
        foreach (var faction in world.State.Factions) Array.Fill(faction.Relations, 0);
        long time = Find(world, world.Plan.Towns[farmer.HomeTown].Center, c => c.Season == season && c.Weather == WeatherKind.Clear, (int)season * WorldSession.SeasonDays * WorldSession.TicksPerDay);
        world.State.Tick = time + WorldSession.TicksPerHour - 11; var town = world.State.Towns[farmer.HomeTown];
        int before = town.Stocks["food"], expected = (int)(12 * world.WorkRate(farmer)) * percent / 100;
        long regional = world.State.RegionalResourcesProduced, balance = world.AccountedResources(), flow = world.RegionalResourceFlow;
        world.Paused = false; world.AdvanceTicks(1);
        Assert.Equal(before + expected - 6, town.Stocks["food"]); Assert.Equal(expected, world.State.RegionalResourcesProduced - regional);
        Assert.Equal(balance + world.RegionalResourceFlow - flow, world.AccountedResources());
    }
    [Fact]
    public void ActualCampCoverRestoresTheFullRestRate()
    {
        using var world = WorldCollisionTests.Arena(); var hero = world.Party.Single();
        world.State.Tick = Find(world, hero.Position, c => c.Weather == WeatherKind.Wind); world.Paused = false;
        hero.Body.Hunger = 0; hero.Body.Fatigue = 60; hero.Body.RestUntil = world.State.Tick + 100;
        world.AdvanceTicks(10); double exposed = hero.Body.Fatigue, expected = 60;
        for (int i = 0; i < 10; i++) expected -= world.Simulation.RestPerTick * .8;
        Assert.Equal(expected, exposed);
        world.Chunks.Edit(hero.Position.Offset(0, -1), Material.Camp);
        hero.Body.Fatigue = 60; hero.Body.RestUntil = world.State.Tick + 100; world.AdvanceTicks(10); expected = 60;
        for (int i = 0; i < 10; i++) expected -= world.Simulation.RestPerTick;
        Assert.Equal(expected, hero.Body.Fatigue); Assert.True(hero.Body.Fatigue < exposed);
    }
    [Theory]
    [InlineData("percent")] [InlineData("future")] [InlineData("deadline")] [InlineData("partial")] [InlineData("missing")]
    [InlineData("habitat")] [InlineData("shelter")]
    public void InvalidWeatherRecoveryAndShelterMetadataIsRejectedWithoutRewritingTheSave(string field)
    {
        using var world = WorldSession.Create(42); var fixture = ConsequencePreview.Prepare(world); var node = world.ResourceAt(fixture.Source)!;
        world.Chunks.Delta(fixture.Source.Chunk).Resources[fixture.Source.LocalIndex] = new(node.Definition.Id, 1, 0);
        Assert.True(world.Submit(new(WorldCommandKind.Harvest, fixture.Source)).Success); world.Paused = false;
        for (int i = 0; i < 150 && world.State.Work.Count > 0; i++) world.AdvanceTicks(1);
        var stock = world.Chunks.ResourceStockAt(fixture.Source)!;
        var invalid = field switch
        {
            "percent" => stock with { RecoveryPercent = 999 }, "future" => stock with { RecoveryStarted = world.State.Tick + 1 },
            "deadline" => stock with { RecoverAt = stock.RecoverAt + 1 }, "partial" => stock with { Remaining = 1 },
            "missing" => stock with { RecoveryPercent = 0 }, _ => stock
        };
        world.Chunks.Delta(fixture.Source.Chunk).Resources[fixture.Source.LocalIndex] = invalid;
        if (field == "habitat") world.State.Habitats[WorldSession.HabitatIndex(fixture.Source.Chunk)] = new() { RecoveryVersion = 2 };
        if (field == "shelter") world.ActorById(fixture.Actor).Routine = new(RoutineKind.Shelter, fixture.Source.Offset(30, 0), world.State.Tick, "Sheltering");
        string path = Path.Combine(_directory, "invalid.json"); world.Save(path); string before = File.ReadAllText(path);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(path)); Assert.Equal(before, File.ReadAllText(path));
    }
    [Fact]
    public void LegacyRecoveryDeadlinesAndUnsettledFoodKeepTheirOriginalMeaningUntilNewUse()
    {
        using var world = WorldSession.Create(42); var fixture = ConsequencePreview.Prepare(world); var node = world.ResourceAt(fixture.Source)!;
        long deadline = world.State.Tick + node.Definition.RegrowTicks;
        world.Chunks.Delta(fixture.Source.Chunk).Resources[fixture.Source.LocalIndex] = new(node.Definition.Id, 0, deadline);
        var habitat = new HabitatState { Epoch = world.State.Tick / WorldSession.TicksPerHour - 2 };
        world.State.Habitats[WorldSession.HabitatIndex(world.State.Position.Chunk)] = habitat;
        int available = world.HabitatAt(world.State.Position).Food;
        Assert.Equal(Math.Min(world.HabitatAt(world.State.Position).Capacity, Math.Max(1, world.HabitatAt(world.State.Position).Capacity / 8) * 2), available);
        string path = Path.Combine(_directory, "legacy.json"); world.Save(path); using var restored = WorldSession.Load(path);
        Assert.Equal(State(world), State(restored)); Assert.Equal(deadline, restored.Chunks.ResourceStockAt(fixture.Source)!.RecoverAt);
        Assert.True(world.Submit(new(WorldCommandKind.Forage)).Success); world.Paused = false;
        for (int i = 0; i < 150 && world.State.Work.Count > 0; i++) world.AdvanceTicks(1);
        Assert.Empty(world.State.Work); Assert.Equal(1, habitat.RecoveryVersion);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SavedShelterJourneysMatchNearAndFarEvenOnAnotherObserverFloor(bool otherFloor)
    {
        using var near = WorldSession.Create(42); var fixture = WeatherPreview.Prepare(near, nearChange: false);
        var field = JourneyPreview.PrepareFarField(near); if (otherFloor) { field = field with { Floor = 1 }; near.Chunks.Edit(field, Material.Floor); }
        var actor = near.ActorById(fixture.Resident); actor.Position = fixture.Home.Offset(8, 0); actor.Body.Hunger = actor.Body.Fatigue = 0;
        near.Paused = false; near.AdvanceTicks(5); Assert.NotNull(actor.Journey); Assert.False(near.Sheltered(actor));
        string path = Path.Combine(_directory, "journey.json"); near.Save(path); using var far = WorldSession.Load(path);
        Assert.Equal(State(near), State(far)); far.Paused = false; far.State.Position = far.Party.Single().Position = field;
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(1, near.AdvanceTicks(1)); Assert.Equal(1, far.AdvanceTicks(1));
            Assert.Equal(JsonSerializer.Serialize(actor), JsonSerializer.Serialize(far.ActorById(actor.Id)));
        }
        Assert.True(near.Sheltered(actor)); Assert.True(far.Sheltered(far.ActorById(actor.Id)));
    }
    [Fact]
    public void WarmRecoveryReadsObserveLiveStockPressureAndClearingWithoutAllocating()
    {
        using var world = WorldSession.Create(42); var p = world.State.Position; int capacity = world.HabitatAt(p).Capacity;
        var state = new HabitatState { RecoveryVersion = 1, Food = 0, Pressure = 1000, Epoch = 0 };
        world.State.Habitats[WorldSession.HabitatIndex(p.Chunk)] = state; world.State.Tick = WorldSession.TicksPerHour;
        Assert.Equal(0, world.HabitatAt(p).Food); state.Pressure = 0; Assert.True(world.HabitatAt(p).Food > 0);
        state.Food = capacity - 1; Assert.Equal(capacity, world.HabitatAt(p).Food);
        state.LandLoss[0] = 1; Assert.Equal(capacity - 1, world.HabitatAt(p).Food);
        Assert.InRange(WarmAllocationProbe.Measure(() => _ = world.HabitatAt(p)), 0, 1024);
    }
}
