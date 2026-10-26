using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.World;
using Roag.Core.Tactics;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class WildlifeRecoveryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-recovery-" + Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }
    private static WorldSession Prepare(out WildlifePopulation population)
    {
        var world = WorldSession.Create(42); var fixture = RecoveryPreview.Prepare(world);
        population = world.State.WildlifePopulations[fixture.Population]; world.Paused = false; return world;
    }
    private static Actor Birth(WorldSession world, WildlifePopulation population)
    {
        world.State.Tick = population.RecoverAt;
        foreach (var actor in world.State.Actors) actor.Body.RestUntil = world.State.Tick + 1000;
        world.AdvanceTicks(50); Assert.Equal(WildlifeRecovery.Alive, population.Recovery); return world.ActorById(population.Actor);
    }
    private static void Hunt(WorldSession world, Actor actor)
    {
        var hero = world.Party.Single(); hero.Role = 0; hero.BasePower = 100; hero.BaseSpeed = 30; hero.Health = hero.MaxHealth;
        world.StartBattle(actor.Id); Assert.True(world.Act(0, 1).Success); Assert.Equal(LifeState.Dead, actor.Life);
    }
    private static Actor Dead(WorldSession world, WorldPosition home)
    {
        long id = world.State.NextActorId++; var actor = ActorFactory.Create(world.Content, "character.rabbit", world.Seed, id, home, 0, 0, 0);
        actor.Habitat = home; actor.Life = LifeState.Dead; actor.Health = 0; actor.WildlifePopulation = id;
        world.State.EcologyChunks.Add(home.Chunk); world.State.Actors.Add(actor);
        world.State.Loot.Add(new() { Id = "wildlife-" + id, SourceActor = id, Position = home, Contents = new() { ["meat"] = 2 } });
        world.State.WildlifePopulations.Add(id, new() { Actor = id, Definition = actor.Archetype, Home = home, Origin = home,
            Deaths = 1, Recovery = WildlifeRecovery.Recovering, RecoverAt = WorldSession.WildlifeRecoveryTicks }); return actor;
    }
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void RealThreeDayPreviewsReturnDistinctIndividualsWithoutDuplicatingRewards(int seed)
    { Assert.True(JsonSerializer.SerializeToNode(RecoveryPreview.Run(ContentCatalog.Default, seed))!["passed"]!.GetValue<bool>()); }
    [Fact]
    public void AQuietPeriodIsRequiredAndBirthSpendsActualWildFood()
    {
        using var world = Prepare(out var population); long old = population.Actor; int count = world.State.Actors.Count;
        world.State.Tick = population.RecoverAt - 1;
        Assert.Equal(LifeState.Dead, world.ActorById(old).Life); Assert.Equal(WildlifeRecovery.Recovering, population.Recovery);
        var before = world.HabitatAt(population.Home); int food = world.Count("food"), experience = world.Party.Single().Experience;
        world.AdvanceTicks(1); var animal = world.ActorById(population.Actor);
        Assert.NotEqual(old, animal.Id); Assert.Equal(1, population.Generation); Assert.Equal(1, population.Deaths); Assert.Equal(count + 1, world.State.Actors.Count);
        Assert.Equal(before.Food - 4, world.HabitatAt(population.Home).Food); Assert.Equal(16, world.HabitatAt(population.Home).Pressure);
        Assert.Equal(food, world.Count("food")); Assert.Equal(experience, world.Party.Single().Experience); Assert.DoesNotContain(animal.Id, world.State.RewardedEncounterIds);
        Assert.Contains(old, world.State.RewardedEncounterIds); Assert.Single(world.State.Loot, c => c.SourceActor == old);
        Assert.Equal(population.Home, animal.Habitat); Assert.Empty(animal.Cargo); Assert.Equal(0, animal.Experience);
    }
    [Theory]
    [InlineData("food")] [InlineData("pressure")] [InlineData("cleared")] [InlineData("blocked")]
    public void ScarcityPressureClearingAndObstructionsDelayRecovery(string condition)
    {
        using var world = Prepare(out var population); world.State.Tick = population.RecoverAt;
        var stock = new HabitatState { Food = condition == "food" ? 0 : 8, Pressure = condition == "pressure" ? 800 : 0, Epoch = world.State.Tick / WorldSession.TicksPerHour };
        world.State.Habitats[WorldSession.HabitatIndex(population.Home.Chunk)] = stock;
        if (condition == "cleared") world.Chunks.Edit(population.Home, Material.Earth);
        if (condition == "blocked") foreach (var (x, y) in WorldSession.Directions) world.Chunks.Edit(population.Home.Offset(x, y), Material.Wall);
        long actor = population.Actor; world.AdvanceTicks(50);
        Assert.Equal(actor, population.Actor); Assert.Equal(0, population.Generation); Assert.Equal(LifeState.Dead, world.ActorById(actor).Life);
        Assert.Equal(condition switch { "food" => WildlifeRecovery.Food, "pressure" => WildlifeRecovery.Pressure, "cleared" => WildlifeRecovery.Habitat, _ => WildlifeRecovery.Occupied }, population.Recovery);
        Assert.True(population.RecoverAt > world.State.Tick); Assert.NotEmpty(world.WildlifePopulationDescription(actor));
    }
    [Fact]
    public void APopulationFollowsItsMigratingIndividualInsteadOfSpawningAtTheVacatedOrigin()
    {
        using var world = WorldSession.Create(42); var fixture = MigrationPreview.Prepare(world); var actor = world.ActorById(fixture.Actor);
        var population = world.State.WildlifePopulations[actor.WildlifePopulation]; world.Paused = false;
        for (int i = 0; i < 1000 && actor.Migration != null; i++) world.AdvanceTicks(1);
        Assert.NotEqual(fixture.Origin, population.Home); Assert.Equal(actor.Habitat, population.Home);
        long id = actor.Id; world.AdvanceTicks(100); Assert.Equal(id, population.Actor); Assert.Equal(0, population.Generation);
        Hunt(world, actor); var replacement = Birth(world, population);
        Assert.Equal(actor.Habitat, replacement.Habitat); Assert.Equal(fixture.Origin, population.Origin); Assert.NotEqual(id, replacement.Id);
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PendingAndBlockedRecoveryHaveExactSavedContinuation(bool blocked)
    {
        using var world = Prepare(out var population); if (blocked) world.Chunks.Edit(population.Home, Material.Earth);
        string path = Path.Combine(_root, "pending.json"); world.Save(path); using var loaded = WorldSession.Load(path); loaded.Paused = false;
        loaded.DetailedBodiesEverywhere = true;
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        for (int i = 0; i < 400; i++)
        {
            world.AdvanceTicks(1); loaded.AdvanceTicks(1);
            if (i % 17 == 0 || i == 399) Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
    }
    [Fact]
    public void NearAndFarObserversGetTheSameNewIdentityAndPopulationAtRecovery()
    {
        using var near = Prepare(out var population); var field = JourneyPreview.PrepareFarField(near);
        string path = Path.Combine(_root, "far.json"); near.Save(path); using var far = WorldSession.Load(path); far.Paused = false;
        far.State.Position = far.Party.Single().Position = field;
        long root = near.ActorById(population.Actor).WildlifePopulation;
        for (int i = 0; i < 50; i++) { near.AdvanceTicks(1); far.AdvanceTicks(1); }
        var distant = far.State.WildlifePopulations[root]; Assert.Equal(JsonSerializer.Serialize(population), JsonSerializer.Serialize(distant));
        Assert.Equal(JsonSerializer.Serialize(near.ActorById(population.Actor)), JsonSerializer.Serialize(far.ActorById(distant.Actor)));
    }
    [Fact]
    public void RetainedUntakenRemainsPauseFurtherRecoveryWithoutDeletingLootOrReferences()
    {
        using var world = Prepare(out var population);
        for (int generation = 1; generation <= WorldSession.WildlifeHistoryLimit; generation++)
        { var animal = Birth(world, population); Assert.Equal(generation, population.Generation); Hunt(world, animal); }
        int actors = world.State.Actors.Count, loot = world.State.Loot.Count; long current = population.Actor;
        world.State.Tick = population.RecoverAt; world.AdvanceTicks(50);
        Assert.Equal(WildlifeRecovery.Remains, population.Recovery); Assert.Equal(current, population.Actor);
        Assert.Equal(actors, world.State.Actors.Count); Assert.Equal(loot, world.State.Loot.Count); Assert.Equal(4, population.History.Count);
        Assert.All(population.History, id => Assert.False(world.State.Loot.Single(c => c.SourceActor == id).Taken));
        string path = Path.Combine(_root, "remains.json"); world.Save(path); using var loaded = WorldSession.Load(path); Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
    }
    [Fact]
    public void OnlyTakenUnreferencedHistoryIsReclaimedAndResidualCargoIsAccountedAsLost()
    {
        using var world = Prepare(out var population);
        for (int i = 0; i < 4; i++) { var animal = Birth(world, population); Hunt(world, animal); }
        long oldest = population.History[0]; var cache = world.State.Loot.Single(c => c.SourceActor == oldest);
        world.State.Position = world.Party.Single().Position = cache.Position.Offset(2, 0); world.Chunks.Edit(world.State.Position, Material.Floor);
        // Actual Loot accepts adjacency, so use a free side distinct from the original home.
        var side = WorldSession.Directions.Select(d => cache.Position.Offset(d.X, d.Y)).First(p => p != population.Home);
        world.Chunks.Edit(side, Material.Floor); world.State.Position = world.Party.Single().Position = side;
        Assert.True(world.Submit(new(WorldCommandKind.Loot, cache.Position)).Success); int meat = world.Count("meat");
        var body = world.ActorById(oldest); body.Cargo["food"] = 1;
        var friend = world.Party.Single(); friend.Memories.Add(new("remembered-hunt", "I remember this animal.", world.State.Tick, population.RecoverAt + 10000, 0, oldest));
        world.State.Tick = population.RecoverAt; world.AdvanceTicks(50); Assert.Equal(WildlifeRecovery.Remains, population.Recovery); Assert.Contains(body, world.State.Actors);
        friend.Memories.Clear(); long balance = world.AccountedResources(), flow = world.RegionalResourceFlow, lost = world.State.ResourcesLost;
        world.AdvanceTicks(WorldSession.TicksPerHour);
        Assert.Equal(WildlifeRecovery.Alive, population.Recovery); Assert.DoesNotContain(body, world.State.Actors); Assert.DoesNotContain(oldest, world.State.RewardedEncounterIds);
        Assert.DoesNotContain(world.State.Loot, c => c.SourceActor == oldest); Assert.Equal(meat, world.Count("meat")); Assert.Equal(lost + 1, world.State.ResourcesLost);
        Assert.Equal(balance + world.RegionalResourceFlow - flow, world.AccountedResources()); Assert.Equal(4, population.History.Count);
        string path = Path.Combine(_root, "reclaimed.json"); world.Save(path); using var loaded = WorldSession.Load(path); Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
    }
    [Fact]
    public void ANewHuntAfterReclaimAndReloadKeepsExactRewardReceiptsAndWorldState()
    {
        using var world = Prepare(out var population);
        for (int i = 0; i < 4; i++) { var animal = Birth(world, population); Hunt(world, animal); }
        var cache = world.State.Loot.Single(c => c.SourceActor == population.History[0]);
        var side = WorldSession.Directions.Select(d => cache.Position.Offset(d.X, d.Y)).First(p => p != population.Home);
        world.Chunks.Edit(side, Material.Floor); world.State.Position = world.Party.Single().Position = side;
        Assert.True(world.Submit(new(WorldCommandKind.Loot, cache.Position)).Success);
        var next = Birth(world, population); Assert.Equal(5, population.Generation);
        string path = Path.Combine(_root, "reclaimed-reward.json"); world.Save(path);
        using var loaded = WorldSession.Load(path);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        Hunt(world, next); Hunt(loaded, loaded.ActorById(next.Id));
        Assert.True(world.State.RewardedEncounterIds.SetEquals(loaded.State.RewardedEncounterIds));
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
    }
    [Fact]
    public void TacticalReferencesRetainOldBodiesUntilTheyCanBeReclaimedWithoutOrphanedParticipants()
    {
        using var world = Prepare(out var population);
        for (int i = 0; i < 4; i++) { var animal = Birth(world, population); Hunt(world, animal); }
        long id = population.History[0]; var cache = world.State.Loot.Single(c => c.SourceActor == id);
        var side = WorldSession.Directions.Select(d => cache.Position.Offset(d.X, d.Y)).First(p => p != population.Home);
        world.Chunks.Edit(side, Material.Floor); world.State.Position = world.Party.Single().Position = side;
        Assert.True(world.Submit(new(WorldCommandKind.Loot, cache.Position)).Success);
        var body = world.ActorById(id);
        body.Tactics = new() { InnateStats = new(20, 1, 1, 0, 100, 0, 150) };
        world.State.Tactics = new() { Participants = [id], NextEvent = 2,
            Events = [new(1, 0, "Died", world.Party.Single().Id, id, 0, body.Position)] };
        Assert.False(world.ReclaimWildlifeHistory(population));
        Assert.Contains(body, world.State.Actors);
        world.State.Tactics.Events.Clear();
        Assert.True(world.ReclaimWildlifeHistory(population));
        Assert.DoesNotContain(body, world.State.Actors);
        Assert.DoesNotContain(id, world.State.Tactics.Participants);
        string path = Path.Combine(_root, "native-history.json"); world.Save(path);
        using var restored = WorldSession.Load(path);
        Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(restored.State));
    }
    [Fact]
    public void LegacyUnsortedRewardReceiptsLoadWithoutRewritingAndSaveInStableOrder()
    {
        using var world = Prepare(out var population); Hunt(world, Birth(world, population));
        string path = Path.Combine(_root, "legacy-reward-order.json"); world.Save(path);
        var json = JsonNode.Parse(File.ReadAllText(path))!;
        json["state"]!["rewardedEncounterIds"] = new JsonArray(world.State.RewardedEncounterIds.OrderDescending().Select(id => JsonValue.Create(id)).ToArray());
        string legacy = json.ToJsonString(); File.WriteAllText(path, legacy);
        using var loaded = WorldSession.Load(path);
        Assert.Equal(legacy, File.ReadAllText(path)); Assert.True(world.State.RewardedEncounterIds.SetEquals(loaded.State.RewardedEncounterIds));
        string output = Path.Combine(_root, "ordered-rewards.json"); loaded.Save(output);
        var ids = JsonNode.Parse(File.ReadAllText(output))!["state"]!["rewardedEncounterIds"]!.AsArray().Select(v => v!.GetValue<long>()).ToArray();
        Assert.Equal(ids.Order(), ids);
    }
    [Fact]
    public void OldestDeadlinesGetTheBoundedBudgetEvenWhenEarlierPopulationsKeepFailing()
    {
        using var world = Prepare(out var population); world.State.Tick = population.RecoverAt;
        var candidates = new List<WildlifePopulation> { population };
        for (int i = 0; i < 27; i++) { var actor = Dead(world, new(32 + i * 64, 32)); candidates.Add(world.State.WildlifePopulations[actor.Id]); }
        foreach (var candidate in candidates) world.State.Habitats[WorldSession.HabitatIndex(candidate.Home.Chunk)] = new() { Food = 8, Pressure = 1000, Epoch = world.State.Tick / WorldSession.TicksPerHour };
        for (int tick = 0; tick < 350; tick++)
        { long before = world.WildlifeRecoveryAttempts; world.AdvanceTicks(1); Assert.InRange(world.WildlifeRecoveryAttempts - before, 0, 4); }
        Assert.All(candidates, p => Assert.Equal(WildlifeRecovery.Pressure, p.Recovery));
    }
    [Fact]
    public void TheGlobalAnimalLimitRetainsBodiesAndDelaysNewBirths()
    {
        using var world = Prepare(out var population);
        // Legacy unregistered bodies still count against the global wildlife cap.
        while (world.State.Actors.Count(a => a.Habitat != null) < WorldSession.WildlifeActorLimit)
        { var actor = Dead(world, population.Home); world.State.WildlifePopulations.Remove(actor.Id); actor.WildlifePopulation = 0; }
        long id = population.Actor; world.State.Tick = population.RecoverAt; world.AdvanceTicks(50);
        Assert.Equal(WildlifeRecovery.Capacity, population.Recovery); Assert.Equal(id, population.Actor);
        Assert.Equal(WorldSession.WildlifeActorLimit, world.State.Actors.Count(a => a.Habitat != null));
    }
    [Theory]
    [InlineData("key")] [InlineData("member")] [InlineData("species")] [InlineData("home")] [InlineData("generation")]
    [InlineData("deaths")] [InlineData("time")] [InlineData("deadline")] [InlineData("status")] [InlineData("history")] [InlineData("null")]
    public void ForgedPopulationStateIsRejectedWithoutRewritingTheSlot(string field)
    {
        using var world = Prepare(out var population); string path = Path.Combine(_root, "bad.json"); world.Save(path); var json = JsonNode.Parse(File.ReadAllText(path))!;
        var registry = json["state"]!["wildlifePopulations"]!.AsObject(); string key = world.ActorById(population.Actor).WildlifePopulation.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var record = registry[key]!;
        switch (field)
        {
            case "key": registry["-1"] = record.DeepClone(); break;
            case "member": record["actor"] = world.State.Party[0]; break;
            case "species": record["definition"] = "character.villager"; break;
            case "home": record["home"]!["floor"] = 1; break;
            case "generation": record["generation"] = -1; break;
            case "deaths": record["deaths"] = 0; break;
            case "time": record["lastDeath"] = world.State.Tick + 1; break;
            case "deadline": record["recoverAt"] = 1; break;
            case "status": record["recovery"] = 999; break;
            case "history": record["history"]!.AsArray().Add(world.State.Party[0]); break;
            case "null": json["state"]!["wildlifePopulations"] = null; break;
        }
        File.WriteAllText(path, json.ToJsonString()); string before = File.ReadAllText(path);
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(path)); Assert.Equal(before, File.ReadAllText(path));
    }
    [Fact]
    public void MissingLegacyPopulationMetadataRegistersLazilyWithoutReplacingAnIndividual()
    {
        using var world = Prepare(out var population); string path = Path.Combine(_root, "old.json"); world.Save(path); var json = JsonNode.Parse(File.ReadAllText(path))!;
        json["state"]!.AsObject().Remove("wildlifePopulations"); foreach (var actor in json["state"]!["actors"]!.AsArray()) actor!.AsObject().Remove("wildlifePopulation");
        File.WriteAllText(path, json.ToJsonString()); using var loaded = WorldSession.Load(path); Assert.Empty(loaded.State.WildlifePopulations);
        long[] identities = loaded.State.Actors.Select(a => a.Id).ToArray(); loaded.Paused = false; loaded.AdvanceTicks(50);
        Assert.Equal(identities, loaded.State.Actors.Select(a => a.Id)); Assert.NotEmpty(loaded.State.WildlifePopulations);
        Assert.Equal(LifeState.Dead, loaded.ActorById(population.Actor).Life); Assert.True(loaded.State.WildlifePopulations[population.Actor].RecoverAt >= loaded.State.Tick + WorldSession.WildlifeRecoveryTicks - 50);
    }
}
