using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonRunTests
{
    [Theory]
    [InlineData(42, ClassKind.Adventurer)]
    [InlineData(-17, ClassKind.Ranger)]
    [InlineData(999, ClassKind.Wayfinder)]
    public void BoundedDungeonConstructionUsesLiveChunksPersistentActorsAndReciprocalStairs(int seed, ClassKind kind)
    {
        using var world = WorldSession.CreateDungeonRun(seed, kind);
        var run = world.State.Dungeon!; var hero = world.Party.Single();
        Assert.Equal(2, run.Floors.Count); Assert.Equal(kind, hero.Tactics!.Build!.Class);
        Assert.Equal("", hero.RoagCharacter); Assert.Equal("", world.State.RoagCombatState); Assert.False(world.State.RoagCombat);
        Assert.Equal(run.Floors[0].Entry, hero.Position); Assert.Equal(hero.Position, world.State.Position);
        Assert.Equal(run.Floors[1].Entry, world.StairLanding(run.Floors[0].Exit));
        Assert.Equal(run.Floors[0].Exit, world.StairLanding(run.Floors[1].Entry));
        Assert.All(run.Floors, floor => Assert.InRange(floor.Rooms.Length, 8, 12));
        Assert.Equal(RoomArea.Dungeon, world.RoomAt(hero.Position)!.Value.Area);
        var actors = world.State.Actors.Where(a => a.Tactics != null).ToArray();
        Assert.True(actors.Length > 5);
        Assert.Equal(actors.Length, actors.Select(a => a.Position).Distinct().Count());
        Assert.All(actors, a => Assert.True(run.Contains(a.Position)));
        Assert.All(actors.Where(a => a.Id != hero.Id), a => Assert.True(EnemyCatalog.Default.Contains(a.Tactics!.DefinitionId)));
        Assert.False(world.CanEnter(run.Floors[0].Origin.Offset(-1, 0), hero.Id));
        Assert.DoesNotContain(world.DungeonCombat.State.Queue.Snapshot().Pending, w => w.Kind == ScheduledKind.Survival);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void NativeStartingClassChoiceIsFreeAndBecomesFixedAtTheFirstPaidAction()
    {
        using var world = WorldSession.CreateDungeonRun(42);
        Assert.True(world.ChooseDungeonClass(ClassKind.Duelist).Success);
        Assert.Equal(ClassKind.Duelist, world.Party.Single().Tactics!.Build!.Class);
        Assert.Equal(0, world.DungeonCombat.Clock);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.False(world.ChooseDungeonClass(ClassKind.Tinkerer).Success);
        Assert.Equal(ClassKind.Duelist, world.Party.Single().Tactics!.Build!.Class);
        Assert.Equal(ActionTiming.Recovery(world.DungeonCombat.Tuning.WaitCost, world.Party.Single().Speed), world.DungeonCombat.Clock);
    }

    [Fact]
    public void BoundedDungeonSavedPendingCombatContinuesWithoutCreatingAnotherEncounter()
    {
        using var world = WorldSession.CreateDungeonRun(42);
        var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var enemy = world.State.Actors.First(a => a.Tactics?.Enemy != null && a.Position.Floor == hero.Position.Floor);
        var position = new[] { hero.Position.Offset(1, 0), hero.Position.Offset(0, 1), hero.Position.Offset(-1, 0), hero.Position.Offset(0, -1) }
            .First(p => world.CanEnter(p));
        enemy.Position = position; enemy.Tactics = EnemyCatalog.Default.Get("core:enemy/mossbound_guard").CreateTactics(); enemy.Health = enemy.MaxHealth;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.NotEmpty(combat.State.Attacks);
        string directory = Path.Combine(Path.GetTempPath(), "roag-dungeon-run-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(directory);
            using var loaded = WorldSession.Load(directory);
            Assert.Equal("roag-world", SessionStore.Format(directory));
            Assert.Equal(JsonSerializer.Serialize(world.State.Dungeon), JsonSerializer.Serialize(loaded.State.Dungeon));
            Assert.Equal(JsonSerializer.Serialize(combat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
            for (int i = 0; i < 3; i++)
            {
                Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
                Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
                Assert.Equal(JsonSerializer.Serialize(combat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
                Assert.Equal(hero.Health, loaded.Party.Single().Health);
                Assert.Equal(world.State.Tick, loaded.State.Tick);
            }
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void BoundedDungeonClockAdvancesOnlyWithPaidCommandsAndDoesNotActivateLaterWorldSystems()
    {
        using var world = WorldSession.CreateDungeonRun(42); world.Paused = false;
        var native = world.DungeonCombat;
        string before = JsonSerializer.Serialize(world.State);
        Assert.Equal(0, world.AdvanceFrame(TimeSpan.FromSeconds(1)));
        Assert.Equal(0, world.AdvanceTicks(50));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        for (int i = 0; i < 12; i++) Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
        Assert.Equal(1200, native.Clock); Assert.Equal(1, world.State.Tick);
        Assert.All(world.State.Actors.Where(a => a.Tactics == null), actor => Assert.Null(actor.Routine));
        Assert.Null(world.State.Battle); Assert.Equal("", world.State.RoagCombatState);
    }

    [Fact]
    public void SavedDungeonMetadataRejectsForgedRoomFootprintsAndDetachedTimelines()
    {
        using var world = WorldSession.CreateDungeonRun(42);
        var run = world.State.Dungeon!; var floor = run.Floors[0];
        run.Floors.Add(floor);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        run.Floors.RemoveAt(2);
        var room = floor.Rooms[0]; floor.Rooms[0] = room with { Width = room.Width + 1 };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        floor.Rooms[0] = room; world.State.Tactics = null;
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
    }

    [Fact]
    public void NativeRoomIdentitySupportsWayfinderPlacementSaveAndPaidReturn()
    {
        using var world = WorldSession.CreateDungeonRun(42, ClassKind.Wayfinder);
        var hero = world.Party.Single(); var destination = hero.Position.Offset(1, 0);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Special, Facing.East)).Success);
        Assert.Equal(RoomArea.Dungeon, hero.Tactics!.Resources.Beacon!.Room.Area);
        string directory = Path.Combine(Path.GetTempPath(), "roag-native-beacon-" + Guid.NewGuid().ToString("N"));
        try
        {
            world.Save(directory);
            using var loaded = WorldSession.Load(directory);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Dash)).Success);
            Assert.Equal(destination, loaded.Party.Single().Position);
            Assert.Null(loaded.Party.Single().Tactics!.Resources.Beacon);
            TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
