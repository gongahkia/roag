using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class NativeBreedingMigrationTests
{
    private static void Wait(WorldSession world, int decisions)
    {
        for (int i = 0; i < decisions; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
    }

    [Theory]
    [InlineData("character.rabbit")]
    [InlineData("character.bat")]
    public void ARealCompletedWallDisplacesBothFamilyMembersThroughPaidPrivateMigrationAndExactSaves(string species)
    {
        var (world, parent) = NativeBreedingTests.Prepare(species); using var dispose = world;
        NativeBreedingTests.CrossPhase(world);
        var child = world.ActorById(Assert.Single(world.State.NativeCampaign!.Breeding.Births).Key);
        var home = parent.Habitat!.Value;
        // Isolate a finished, supplied NPC construction slice at the real home.
        // Clear an approach, forcing new homes beyond that edited approach.
        for (int y = -3; y <= 3; y++) for (int x = -3; x <= 3; x++)
            if ((x, y) != (0, 0)) world.Chunks.Edit(home.Offset(x, y), Material.Floor);
        parent.Position = home.Offset(1, 0); child.Position = home.Offset(-1, 0);
        var worker = world.ActorById(1000); worker.Position = home.Offset(0, 1); worker.Journey = null;
        worker.Routine = new(RoutineKind.AssignedWork, home, world.State.Tick, "Construction fixture");
        var order = new WorkOrder { Id = world.State.NextWorkId++, Kind = WorkKind.Build, Definition = "recipe.wall",
            Target = home, Before = world.Chunks.At(home), After = Material.Wall, Required = 15, Progress = 14.9,
            Worker = worker.Id, RequestedBy = world.Party.Single().Id, PinnedWorker = true, SupplyVersion = 1,
            Supplies = [new() { Stack = "stone", Count = 2, Phase = SupplyPhase.Delivered }], Reserved = new() { ["stone"] = 2 } };
        world.State.Inventory["stone"] -= 2; world.State.Work.Add(order);
        foreach (var animal in new[] { parent, child })
        {
            world.State.Tactics!.Queue.CancelActor(animal.Id, ScheduledKind.ActorReady);
            world.State.Tactics.Queue.Schedule(ScheduledKind.ActorReady, world.DungeonCombat.Clock + 100, animal.Id);
        }
        Wait(world, 12);
        Assert.Equal(Material.Wall, world.Chunks.At(home)); Assert.DoesNotContain(order, world.State.Work);
        Assert.Equal(world.State.Tick, parent.WildlifeAfter); Assert.Equal(world.State.Tick, child.WildlifeAfter);
        Wait(world, 48);
        Assert.True(parent.Migration != null || parent.Habitat != home);
        Assert.True(child.Migration != null || child.Habitat != home);
        int count = world.State.Actors.Count; long next = world.State.NextActorId;
        var explored = world.State.Chunks.SelectMany(c => c.Discovered.Select(index => (c.Key, index))).ToHashSet();
        string root = Path.Combine(Path.GetTempPath(), "roag-native-family-trip-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root); using var loaded = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            for (int i = 0; i < 2500 && (parent.Habitat == home || child.Habitat == home); i++)
            {
                Wait(world, 1); Wait(loaded, 1);
                foreach (var step in world.State.Tactics!.Events.Where(e => e.Kind == "Moved" && (e.Source == parent.Id || e.Source == child.Id)))
                {
                    Assert.NotNull(step.From); Assert.Equal(1, step.From.Value.Distance(step.Position));
                }
                if (i % 31 == 0) Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            }
            Assert.NotEqual(home, parent.Habitat); Assert.NotEqual(home, child.Habitat);
            Assert.Null(parent.Migration); Assert.Null(child.Migration);
            Assert.Null(parent.Journey); Assert.Null(child.Journey);
            Assert.Equal(count, world.State.Actors.Count); Assert.Equal(next, world.State.NextActorId);
            Assert.NotEqual(parent.Position, child.Position);
            Assert.Contains(world.State.Tactics!.Events, e => e.Kind == "Moved" && e.Source == parent.Id);
            Assert.Contains(world.State.Tactics.Events, e => e.Kind == "Moved" && e.Source == child.Id);
            Assert.True(explored.SetEquals(world.State.Chunks.SelectMany(c => c.Discovered.Select(index => (c.Key, index)))));
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            world.Save(root); using var arrived = WorldSession.Load(root);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(arrived.State));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void ANearbyTravellerBehindAWallCannotFreezeAnAnimalsMigrationSearch()
    {
        var (world, parent) = NativeBreedingTests.Prepare(); using var dispose = world;
        var home = parent.Habitat!.Value; parent.Position = home;
        var hero = world.Party.Single(); hero.Position = world.State.Position = home.Offset(2, 0);
        world.Chunks.Edit(hero.Position, Material.Floor); world.Chunks.Edit(home.Offset(1, 0), Material.Wall);
        world.Chunks.Edit(home, Material.Floor); parent.WildlifeAfter = world.State.Tick;
        world.State.NativeCampaign!.Breeding.After[parent.Id] = world.State.Tick + WorldSession.NativeBroodTicks;
        world.Reveal(); Assert.Null(world.NativeFleeThreat(parent));
        NativeBreedingTests.CrossPhase(world);
        Assert.True(parent.Habitat != home || parent.Migration is { Cursor: > 0 });
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RelatedAnimalsCannotShareAReservationOutsideTheirAlreadySharedHabitat(bool differentTargets)
    {
        var (world, parent) = NativeBreedingTests.Prepare(); using var dispose = world;
        NativeBreedingTests.CrossPhase(world);
        var child = world.ActorById(Assert.Single(world.State.NativeCampaign!.Breeding.Births).Key);
        var home = parent.Habitat!.Value; var target = home.Offset(home.Chunk.X < 63 ? 64 : -64, 0);
        world.State.EcologyChunks.Add(target.Chunk);
        foreach (var animal in new[] { parent, child })
        {
            animal.NatalHabitat ??= home;
            animal.Migration = new() { Origin = home, Center = animal.Position, Target = differentTargets && animal == child ? target.Offset(0, target.Y % 64 < 63 ? 1 : -1) : target,
                Reason = MigrationReason.HomeUnavailable, Since = world.State.Tick, LastProgressTick = world.State.Tick, LastPosition = animal.Position };
        }
        string root = Path.Combine(Path.GetTempPath(), "roag-native-family-reservation-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(root);
            var error = Assert.Throws<InvalidDataException>(() => { using var invalid = WorldSession.Load(root); });
            Assert.Contains("Duplicate wildlife home or migration reservation", error.Message);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
}
