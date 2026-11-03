using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeBreedingTests
{
    private static (WorldSession World, Actor Parent) Prepare()
    {
        var world = NativeSocietyTests.AtTick(49); var hero = world.Party.Single();
        var habitat = WildlifePreview.Find(world, "character.rabbit");
        hero.Position = world.State.Position = habitat.Home; world.Reveal();
        var parent = world.State.Actors.Single(a => a.Habitat == habitat.Home);
        foreach (var (x, y) in WorldSession.Directions) world.Chunks.Edit(habitat.Home.Offset(x, y), Material.Floor);
        hero.Position = world.State.Position = world.Plan.Towns[0].Center with { Floor = 1 };
        world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal();
        // Explicitly isolate a mature, due brood at the next paid world phase.
        world.State.NativeCampaign!.Breeding.After[parent.Id] = 50;
        return (world, parent);
    }

    private static void CrossPhase(WorldSession world)
    {
        for (int i = 0; i < 12; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(50, world.State.Tick);
    }

    private static Actor AddNeighbour(WorldSession world, Actor parent)
    {
        var home = parent.Habitat!.Value;
        var cell = WorldSession.Directions.Select(d => home.Offset(d.X, d.Y)).First(p => world.CanEnter(p));
        var neighbour = ActorFactory.Create(world.Content, parent.Archetype, world.Seed, world.State.NextActorId++, cell,
            parent.HomeTown, parent.Culture, parent.FactionId);
        neighbour.Habitat = neighbour.NatalHabitat = home; neighbour.Job = "wildlife";
        neighbour.WildlifePopulation = neighbour.Id;
        world.State.WildlifePopulations.Add(neighbour.Id, new() { Actor = neighbour.Id, Definition = neighbour.Archetype, Origin = home, Home = home });
        world.State.Actors.Add(neighbour); world.DungeonCombat.RegisterRetainedEnemy(neighbour, world.DungeonCombat.Clock + 1000000);
        world.State.NativeCampaign!.Breeding.After[neighbour.Id] = world.State.Tick + WorldSession.NativeBroodTicks;
        return neighbour;
    }

    private static void Kill(WorldSession world, Actor animal)
        => world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, 0, animal.Position.Offset(1, 0),
            [animal.Position], "test:previous-wildlife-fall", AttackDelivery.Area, DamageElement.Physical,
            1000, world.DungeonCombat.Clock));

    [Fact]
    public void AHealthyLivingPopulationUsesFiniteHabitatFoodToRaiseANewNativeIndividualOffscreen()
    {
        var (world, parent) = Prepare(); using var dispose = world;
        var home = parent.Habitat!.Value; int food = world.HabitatAt(home).Food, count = world.State.Actors.Count;
        long next = world.State.NextActorId; var before = parent.Position;
        string inspected = JsonSerializer.Serialize(world.State);
        Assert.Contains("raise new inhabitants", world.WildlifePopulationDescription(parent.Id));
        Assert.Equal(inspected, JsonSerializer.Serialize(world.State));
        CrossPhase(world);
        var child = world.ActorById(next);
        Assert.Equal(count + 1, world.State.Actors.Count); Assert.Equal(next + 1, world.State.NextActorId);
        Assert.Equal(parent.Archetype, child.Archetype); Assert.Equal(home, child.Habitat); Assert.Equal(home, child.NatalHabitat);
        Assert.Equal(LifeState.Alive, child.Life); Assert.NotNull(child.Tactics?.Enemy?.Retained);
        Assert.True(child.Position.Distance(home) <= 1); Assert.NotEqual(parent.Position, child.Position);
        Assert.Equal(before, parent.Position); Assert.Equal(food - WorldSession.WildlifeBirthFood, world.HabitatAt(home).Food);
        Assert.Equal(new NativeWildlifeBirth(parent.Id, 50), world.State.NativeCampaign!.Breeding.Births[child.Id]);
        Assert.Equal(50 + WorldSession.NativeBroodTicks, world.State.NativeCampaign.Breeding.After[parent.Id]);
        Assert.Equal(50 + WorldSession.NativeBroodTicks, world.State.NativeCampaign.Breeding.After[child.Id]);
        Assert.DoesNotContain(child.Position, world.Visible); Assert.Contains("Born on day", world.WildlifePopulationDescription(child.Id));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData("food")] [InlineData("pressure")] [InlineData("space")] [InlineData("crowded")]
    [InlineData("hunger")] [InlineData("fatigue")] [InlineData("wounds")] [InlineData("altered-home")] [InlineData("summoned")]
    public void UnviableOrUnreadyPopulationsCannotCreateAChildOrSpendBirthFood(string reason)
    {
        var (world, parent) = Prepare(); using var dispose = world;
        var home = parent.Habitat!.Value;
        if (reason is "food" or "pressure")
        {
            var view = world.HabitatAt(home);
            world.State.Habitats[WorldSession.HabitatIndex(home.Chunk)] = new() { RecoveryVersion = 1, Food = reason == "food" ? 0 : view.Food,
                Pressure = reason == "pressure" ? 200 : 0, Epoch = world.State.Tick / WorldSession.TicksPerHour };
        }
        if (reason == "space") foreach (var (x, y) in WorldSession.Directions)
        { var cell = home.Offset(x, y); if (cell != parent.Position) world.Chunks.Edit(cell, Material.Wall); }
        if (reason == "crowded") AddNeighbour(world, parent);
        if (reason == "hunger") parent.Body.Hunger = 40;
        if (reason == "fatigue") parent.Body.Fatigue = 80;
        if (reason == "wounds") parent.Health = 1;
        if (reason == "altered-home") world.Chunks.Edit(home, Material.Floor);
        if (reason == "summoned") parent.Tactics!.RewardExempt = true;
        int food = world.HabitatAt(home).Food, count = world.State.Actors.Count;
        long next = world.State.NextActorId;
        CrossPhase(world);
        Assert.Equal(count, world.State.Actors.Count); Assert.Equal(next, world.State.NextActorId);
        Assert.Equal(food, world.HabitatAt(home).Food); Assert.Empty(world.State.NativeCampaign!.Breeding.Births);
        Assert.Equal(50 + WorldSession.TicksPerHour, world.State.NativeCampaign.Breeding.After[parent.Id]);
    }

    [Fact]
    public void BirthUsesASurvivingParentAndVacantLedgerWhilePreservingTheDeadIndividualAndItsRemains()
    {
        var (world, parent) = Prepare(); using var dispose = world;
        var previous = AddNeighbour(world, parent); Kill(world, previous);
        Assert.Equal(LifeState.Dead, previous.Life);
        var population = world.State.WildlifePopulations[previous.Id];
        long death = population.LastDeath, deadline = population.RecoverAt, childId = world.State.NextActorId;
        var remains = Assert.Single(world.State.Loot, c => c.SourceActor == previous.Id);
        CrossPhase(world);
        var child = world.ActorById(childId);
        Assert.Equal(previous.Id, child.WildlifePopulation); Assert.Equal(child.Id, population.Actor);
        Assert.Equal(1, population.Generation); Assert.Equal(1, population.Deaths); Assert.Contains(previous.Id, population.History);
        Assert.Equal(death, population.LastDeath); Assert.Equal(0, population.RecoverAt);
        Assert.True(world.State.Tick < deadline); Assert.Equal(LifeState.Dead, previous.Life);
        Assert.Same(remains, Assert.Single(world.State.Loot, c => c.SourceActor == previous.Id));
        Assert.Equal(parent.Id, world.State.NativeCampaign!.Breeding.Births[child.Id].Parent);
        Assert.False(child.Tactics!.RewardsClaimed); Assert.True(previous.Tactics!.RewardsClaimed);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void LocalBreedingContinuesDuringActualPartyCombatOnADifferentFloor()
    {
        var (world, _) = Prepare(); using var dispose = world;
        var hero = world.Party.Single(); var threat = world.State.Actors.First(a => a.Kind == ActorKind.Raider && a.Tactics?.Enemy != null);
        threat.Position = hero.Position.Offset(1, 0); world.Chunks.Edit(threat.Position, Material.Floor); world.Reveal();
        Assert.True(world.IsInCombat);
        CrossPhase(world);
        Assert.Single(world.State.NativeCampaign!.Breeding.Births);
        Assert.Equal(LifeState.Alive, hero.Life);
    }

    [Fact]
    public void PendingAndCompletedBroodsRoundTripAndContinueExactlyWithoutInspectionOrDuplicateBirths()
    {
        var (world, parent) = Prepare(); using var dispose = world;
        string root = Path.Combine(Path.GetTempPath(), "roag-native-brood-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            CrossPhase(world); CrossPhase(loaded);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            world.Save(root); using var born = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(born.State));
            long next = world.State.NextActorId; string inspected = JsonSerializer.Serialize(world.State);
            foreach (long id in world.State.NativeCampaign!.Breeding.Births.Keys) Assert.NotEmpty(world.WildlifePopulationDescription(id));
            Assert.Equal(inspected, JsonSerializer.Serialize(world.State));
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success); Assert.True(born.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(born.State));
            Assert.Equal(next, world.State.NextActorId); Assert.Single(world.State.NativeCampaign.Breeding.Births);
            Assert.True(world.State.NativeCampaign.Breeding.After[parent.Id] > world.State.Tick);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void AnExtinctPopulationCannotReturnAtItsOldTimerWithoutALivingParent()
    {
        using var world = WorldSession.Create(42);
        var habitat = WildlifePreview.Find(world, "character.rabbit"); var hero = world.Party.Single();
        hero.Position = world.State.Position = habitat.Home; world.Reveal();
        var animal = world.State.Actors.Single(a => a.Habitat == habitat.Home);
        hero.Role = 0; hero.BaseSpeed = 30; hero.BasePower = 100;
        world.StartBattle(animal.Id); Assert.True(world.Act(0, 1).Success);
        Assert.Equal(LifeState.Dead, animal.Life);
        var population = world.State.WildlifePopulations[animal.Id];
        world.State.Tick = population.RecoverAt - 1; world.State.NativeCampaign = new();
        var combat = world.CreateTacticalSimulation(new(EnableSurvival: true));
        combat.Register(hero, new() { Build = CharacterBuild.Create(ClassKind.Adventurer, 42), AutoEat = false });
        foreach (var enemy in world.State.Actors.Where(a => a.Life == LifeState.Alive && a.Kind is ActorKind.Beast or ActorKind.Raider))
            combat.RegisterRetainedEnemy(enemy, combat.Clock + 1000000);
        hero.Position = world.State.Position = world.Plan.Towns[0].Center with { Floor = 1 };
        world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal();
        Assert.Same(hero, combat.AdvanceToPartyDecision());
        long next = world.State.NextActorId;
        for (int i = 0; i < 12; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(population.RecoverAt, world.State.Tick); Assert.Equal(LifeState.Dead, animal.Life);
        Assert.Equal(animal.Id, population.Actor); Assert.Equal(0, population.Generation);
        Assert.Equal(next, world.State.NextActorId); Assert.Empty(world.State.NativeCampaign.Breeding.Births);
        Assert.Equal(0, world.WildlifeRecoveryAttempts);
    }

    [Theory]
    [InlineData("future")] [InlineData("self")] [InlineData("unrelated")] [InlineData("missing-lineage")]
    [InlineData("early-brood")] [InlineData("null-record")]
    public void InvalidBirthProvenanceAndUnrelatedSharedHomesCannotLoad(string reason)
    {
        var (world, _) = Prepare(); using var dispose = world; CrossPhase(world);
        var record = Assert.Single(world.State.NativeCampaign!.Breeding.Births);
        var breeding = world.State.NativeCampaign.Breeding;
        if (reason == "future") breeding.Births[record.Key] = record.Value with { Tick = world.State.Tick + 1 };
        if (reason == "self") breeding.Births[record.Key] = record.Value with { Parent = record.Key };
        if (reason == "unrelated") breeding.Births[record.Key] = record.Value with { Parent = 1000 };
        if (reason == "missing-lineage") breeding.Births.Remove(record.Key);
        if (reason == "early-brood") breeding.After[record.Key] = world.State.Tick + 1;
        if (reason == "null-record") breeding.Births[record.Key] = null!;
        string root = Path.Combine(Path.GetTempPath(), "roag-invalid-brood-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root);
            Assert.Throws<InvalidDataException>(() => { using var invalid = WorldSession.Load(root); });
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
