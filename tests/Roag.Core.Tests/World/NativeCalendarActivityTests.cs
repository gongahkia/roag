using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.Tactics;
using Roag.Core.World;
using Roag.Tests.Content;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeCalendarActivityTests
{
    // Real seeded habitats, explicit calendar boundaries and a distant observer.
    // Other creature readiness is deferred by the society fixture; body time
    // still advances only through ordinary, paid party commands.
    private static (WorldSession World, Actor Animal) AnimalAt(string species, long tick)
    {
        var world = NativeSocietyTests.AtTick(tick); var hero = world.Party.Single();
        var habitat = WildlifePreview.Find(world, species);
        hero.Position = world.State.Position = habitat.Home; world.Reveal();
        var animal = world.State.Actors.Single(a => a.Habitat == habitat.Home);
        hero.Position = world.State.Position = world.Plan.Towns[0].Center with { Floor = 1 };
        world.Chunks.Edit(hero.Position, Material.Floor); world.Reveal();
        animal.Body.Fatigue = 20; animal.Body.Hunger = 0;
        return (world, animal);
    }

    private static void Wait(WorldSession world, int turns = 12)
    {
        for (int i = 0; i < turns; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
    }

    [Theory]
    [InlineData("character.rabbit", 49, true)] [InlineData("character.rabbit", 1849, false)]
    [InlineData("character.bee", 49, true)] [InlineData("character.bee", 1849, false)]
    [InlineData("character.spider", 49, false)] [InlineData("character.spider", 1849, true)]
    [InlineData("character.bat", 49, false)] [InlineData("character.bat", 1849, true)]
    public void SpeciesActivityChangesActualOffscreenRestAndHealingWithoutRevealingItsHabitat(string species, long tick, bool rests)
    {
        var (world, animal) = AnimalAt(species, tick); using var dispose = world;
        animal.Health--; int health = animal.Health; var at = animal.Position;
        string inspected = JsonSerializer.Serialize(world.State);
        Assert.Equal(rests, world.CalendarResting(animal, world.State.Tick));
        Assert.Contains(rests ? "Resting now" : "Awake now", world.WildlifeDescription(animal));
        Assert.Equal(inspected, JsonSerializer.Serialize(world.State));
        Wait(world);
        Assert.Equal(rests ? animal.MaxHealth : health, animal.Health);
        Assert.Equal(rests, animal.Body.Fatigue < 20); Assert.Equal(at, animal.Position);
        Assert.DoesNotContain(at, world.Visible);
        Assert.True(world.DungeonCombat.Clock > tick * world.State.Tactics!.Tuning.UnitsPerWorldTick);
    }

    [Theory]
    [InlineData(1799, false)] [InlineData(5999, true)]
    public void CalendarRestChangesAtDawnAndDuskAndContinuesExactlyAcrossOrdinarySaves(long tick, bool after)
    {
        var (world, animal) = AnimalAt("character.rabbit", tick); using var dispose = world;
        Assert.Equal(!after, world.CalendarResting(animal, tick));
        string root = Path.Combine(Path.GetTempPath(), "roag-native-activity-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Wait(world); Wait(loaded);
            Assert.Equal(after, world.CalendarResting(animal, world.State.Tick));
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            world.Save(root); using var boundary = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(boundary.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SleepingDefendersWakeOnlyForPrivatelyVisibleCloseThreatsOrRealProvocation(bool wall)
    {
        var (world, animal) = AnimalAt("character.bee", 49); using var dispose = world;
        var hero = world.Party.Single(); var home = animal.Position;
        for (int y = -5; y <= 5; y++) for (int x = -5; x <= 5; x++) world.Chunks.Edit(home.Offset(x, y), Material.Floor);
        hero.Position = world.State.Position = WorldSession.Directions.Select(d => home.Offset(d.X * 3, d.Y * 3))
            .First(p => p.Distance(animal.Habitat!.Value) <= world.Ecology(animal)!.Notice);
        world.Reveal(); Assert.Contains(hero.Position, world.VisionFrom(animal.Position));
        Assert.False(world.IsHostile(animal)); Assert.Null(world.DungeonCombat.SelectEnemyTarget(animal.Id));
        Assert.True(world.CalendarResting(animal, world.State.Tick));
        hero.Position = world.State.Position = home.Offset(2, 0);
        if (wall) world.Chunks.Edit(home.Offset(1, 0), Material.Wall);
        world.Reveal();
        Assert.Equal(!wall, world.IsHostile(animal));
        Assert.Equal(wall, world.CalendarResting(animal, world.State.Tick));
        Assert.Equal(wall ? null : (long?)hero.Id, world.DungeonCombat.SelectEnemyTarget(animal.Id));
        // An actual owned positive hit wakes it even through a blocked sightline;
        // it still gains no hidden target position from that damage.
        world.DungeonCombat.ResolveAttack(new(world.State.Tactics!.NextAction++, hero.Id, hero.Position,
            [home], "test:wake-defender", AttackDelivery.Area, DamageElement.Physical, 1, world.DungeonCombat.Clock));
        Assert.True(animal.ProvokedUntil > world.State.Tick); Assert.False(world.CalendarResting(animal, world.State.Tick));
        Assert.True(world.IsHostile(animal));
        if (wall) Assert.Null(world.DungeonCombat.SelectEnemyTarget(animal.Id));
    }

    [Fact]
    public void DuskDoesNotEraseAnActualPrivateTargetObservationAndRestResumesAfterItsOrdinaryMemoryExpires()
    {
        var (world, animal) = AnimalAt("character.bee", 5999); using var dispose = world;
        var hero = world.Party.Single(); var home = animal.Position;
        for (int y = -2; y <= 2; y++) for (int x = -2; x <= 3; x++) world.Chunks.Edit(home.Offset(x, y), Material.Floor);
        hero.Position = world.State.Position = home.Offset(2, 0); world.Reveal();
        Wait(world, 1); Assert.Equal(hero.Id, animal.Tactics!.Enemy!.Awareness!.Target);
        // Keep the observed traveller within the real territory, hidden behind
        // a new wall. Leaving its leash would correctly retire the memory.
        world.Chunks.Edit(home.Offset(1, 0), Material.Wall); world.Reveal();
        Wait(world, 11); Assert.True(world.IsNight);
        Assert.NotNull(animal.Tactics.Enemy.Awareness); Assert.False(world.CalendarResting(animal, world.State.Tick));
        Wait(world, 36); Assert.Null(animal.Tactics.Enemy.Awareness);
        Assert.True(world.CalendarResting(animal, world.State.Tick));
    }

    [Fact]
    public void ACalendarRestingParentCannotProduceAnAwakeBroodAtAnOtherwiseDuePhase()
    {
        var (world, animal) = AnimalAt("character.rabbit", 49); using var dispose = world;
        foreach (var (x, y) in WorldSession.Directions) world.Chunks.Edit(animal.Habitat!.Value.Offset(x, y), Material.Floor);
        world.State.NativeCampaign!.Breeding.After[animal.Id] = 50;
        int food = world.HabitatAt(animal.Habitat!.Value).Food;
        Wait(world);
        Assert.Empty(world.State.NativeCampaign.Breeding.Births);
        Assert.Equal(food, world.HabitatAt(animal.Habitat.Value).Food);
        Assert.Equal(50 + WorldSession.TicksPerHour, world.State.NativeCampaign.Breeding.After[animal.Id]);
    }

    [Fact]
    public void HungerAndMigrationInterruptScheduledWildlifeRestWithoutSpendingPartyFood()
    {
        var (world, animal) = AnimalAt("character.rabbit", 49); using var dispose = world;
        Assert.True(world.CalendarResting(animal, world.State.Tick));
        animal.Body.Hunger = 40; Assert.False(world.CalendarResting(animal, world.State.Tick));
        animal.Body.ForageProgress = 29.9; int food = world.Count("food");
        Wait(world); Assert.True(animal.Cargo.GetValueOrDefault("food") > 0); Assert.Equal(food, world.Count("food"));
        animal.Body.Hunger = 0; animal.Body.ForageProgress = 0;
        animal.Position = animal.Habitat!.Value.Offset(3, 0);
        Assert.False(world.CalendarResting(animal, world.State.Tick));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void InactiveWildlifeReturnsHomeThroughPaidMovementThenWaitsOnItsOwnRecovery(bool away)
    {
        var (world, animal) = AnimalAt("character.rabbit", 49); using var dispose = world;
        var home = animal.Habitat!.Value;
        for (int x = 0; x <= 3; x++) world.Chunks.Edit(home.Offset(x, 0), Material.Floor);
        animal.Position = home.Offset(away ? 3 : 1, 0);
        var combat = world.DungeonCombat;
        world.State.Tactics!.Queue.CancelActor(animal.Id, ScheduledKind.ActorReady);
        world.State.Tactics.Queue.Schedule(ScheduledKind.ActorReady, combat.Clock, animal.Id);
        Assert.True(combat.Commit(new(TacticalAction.Wait)).Success);
        Assert.Equal(animal.Id, combat.AdvanceToDecision()!.Id);
        long clock = combat.Clock; var at = animal.Position;
        string memory = JsonSerializer.Serialize(animal.Tactics!.Enemy!.Retained);
        var preview = combat.PreviewWorldEnemyTurn();
        Assert.Equal(away ? EnemyIntentKind.Move : EnemyIntentKind.Wait, preview.Kind);
        Assert.Equal(preview, combat.CommitWorldEnemyTurn());
        Assert.Equal(away ? 1 : 0, at.Distance(animal.Position));
        Assert.True(world.CalendarResting(animal, world.State.Tick));
        Assert.Equal(memory, JsonSerializer.Serialize(animal.Tactics.Enemy.Retained));
        var ready = Assert.Single(world.State.Tactics.Timeline.Pending, e => e.ActorId == animal.Id && e.Kind == ScheduledKind.ActorReady);
        Assert.Equal(clock + ActionTiming.Recovery(100, animal.Speed), ready.Due);
    }

    [Fact]
    public void AResidentWhoActuallyNoticesACasualtyStopsCalendarRestForTheirPaidRescue()
    {
        using var world = NativeSocietyTests.AtTick(1794);
        var person = world.ActorById(1004); var hero = world.Party.Single();
        var home = world.Plan.Towns[person.HomeTown].Center.Offset((int)(person.Id % 5) * 3 - 6, -18);
        for (int y = -3; y <= 3; y++) for (int x = -3; x <= 3; x++) world.Chunks.Edit(home.Offset(x, y), Material.Floor);
        person.Position = home; hero.Position = world.State.Position = home.Offset(2, 0);
        person.Body.Fatigue = 20; person.Trust[hero.Id] = 40; person.TraitId = "trait.generous";
        foreach (var other in world.State.Actors.Where(a => !a.Recruited && a.Kind == ActorKind.Person)) other.Cargo.Remove("medicine");
        person.Cargo["medicine"] = 1; world.State.Towns[person.HomeTown].Stocks["medicine"]--;
        world.Reveal(); Wait(world); Assert.True(world.CalendarResting(person, world.State.Tick));
        Roag.Core.Tests.Tactics.DungeonAbandonmentTests.Down(world, hero);
        Assert.Equal(hero.Id, person.Tactics!.Resident!.Target);
        Assert.False(world.CalendarResting(person, world.State.Tick));
        Assert.Single(world.State.Tactics!.Events, e => e.Kind == "RescueNoticed" && e.Source == person.Id);
        Assert.Equal(1, person.Cargo["medicine"]); Assert.False(person.Recruited);
    }

    [Fact]
    public void OvernightResidentsSleepOnlyAfterReachingHomeAndResumeTheirDaytimeRoutinesAtDawn()
    {
        using var world = NativeSocietyTests.AtTick(1794);
        var person = world.ActorById(1000); var home = world.Plan.Towns[person.HomeTown].Center.Offset((int)(person.Id % 5) * 3 - 6, -18);
        for (int y = -2; y <= 2; y++) for (int x = -2; x <= 4; x++) world.Chunks.Edit(home.Offset(x, y), Material.Floor);
        person.Position = home.Offset(3, 0); person.Body.Fatigue = 20;
        Wait(world); Assert.Equal(RoutineKind.Rest, person.Routine!.Kind);
        Assert.False(world.CalendarResting(person, world.State.Tick)); Assert.True(person.Body.Fatigue > 20);
        // Explicitly position the same resident at its selected physical home to
        // isolate sleep rates and the following dawn, rather than fake travel.
        person.Position = person.Routine.Target; person.Body.Fatigue = 20;
        Wait(world); Assert.True(world.CalendarResting(person, world.State.Tick)); Assert.True(person.Body.Fatigue < 20);
        Wait(world, 48); Assert.Equal(6, world.HourOfDay);
        Assert.False(world.CalendarResting(person, world.State.Tick));
        Assert.NotEqual(RoutineKind.Rest, person.Routine!.Kind); Assert.Equal(0, person.Body.RestUntil);
    }

    [Fact]
    public void ActivityAuthoringRejectsUnknownValuesAndRetainsAlwaysAsTheOptionalDefault()
    {
        var bad = ContentTests.Edited("character.rabbit", n => n["ecology"]!["activity"] = "moon-phase");
        Assert.Contains(bad.Issues, i => i.Field == "ecology.activity");
        var invalidNotice = ContentTests.Edited("character.bee", n => n["ecology"]!["sleepingNotice"] = 4);
        Assert.Contains(invalidNotice.Issues, i => i.Field == "ecology.sleepingNotice");
        var missing = ContentTests.Edited("character.rabbit", n => n["ecology"]!.AsObject().Remove("activity"));
        Assert.Empty(missing.Issues); Assert.Equal("always", missing.Get<CharacterDefinition>("character.rabbit").Ecology!.Activity);
    }
}
