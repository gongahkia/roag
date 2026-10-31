using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed partial class DungeonTerrainTests
{
    [Fact]
    public void AssemblyPublicCommandsMatchThePinnedOriginalTimingRewardsAndPrivateSight()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-assemblies.json")));
        int commands = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); string mode = input.GetProperty("mode").GetString()!;
            using var world = Floor(input.TryGetProperty("fast", out _) ? ClassKind.Ranger : ClassKind.Adventurer);
            var hero = world.Party.Single(); var combat = world.DungeonCombat; DungeonService node; BellPuzzle? puzzle = null;
            if (mode == "bells")
            {
                puzzle = combat.SpawnBellPuzzle("test:reference-bells", hero.Position.Offset(2, 0), 37);
                Assert.Equal(sample.GetProperty("sequence").EnumerateArray().Select(v => v.GetInt32()), puzzle.Sequence);
                node = combat.State.Services.Nodes.Single(n => n.Id == puzzle.Plaque); hero.Position = world.State.Position = node.Position;
            }
            else if (mode == "sign")
            {
                var room = world.State.Dungeon!.Floors[0].Rooms.First(r => r.Index + 1 != world.RoomAt(hero.Position)!.Value.Room);
                node = combat.SpawnDungeonService(DungeonServiceKind.RouteSign, "test:reference-sign", hero.Position.Offset(1, 0),
                    sign: new(Facing.East, "Treasure", new(RoomArea.Dungeon, 0, hero.Position.Floor, room.Index + 1)));
            }
            else
            {
                node = combat.SpawnDungeonService(DungeonServiceKind.FallingFixture, "test:reference-fixture", hero.Position.Offset(1, 0));
                if (mode != "safe") hero.Position = world.State.Position = node.Position;
                if (mode == "mist") Overlay(world, node.Fixture!.Center, 'm');
            }
            world.Reveal();
            if (mode == "sign")
            {
                string before = JsonSerializer.Serialize(world.State);
                Assert.Contains("Treasure east", combat.DungeonServiceDescription(node.Id, hero.Id));
                Assert.Empty(combat.DungeonServiceChoices(node.Id, hero.Id)); Assert.Equal(before, JsonSerializer.Serialize(world.State));
            }
            int index = 0;
            foreach (var command in sample.GetProperty("commands").EnumerateArray())
            {
                string name = command.GetString()!; long target = node.Id;
                if (puzzle != null)
                {
                    int number = name == "wrong" ? puzzle.Sequence[0] % 3 + 1 : name == "repeat" ? 1 : int.Parse(name, System.Globalization.CultureInfo.InvariantCulture);
                    target = puzzle.Bells[number - 1];
                }
                var intent = name == "leave" ? new TacticalIntent(TacticalAction.Move, Facing.West)
                    : new(TacticalAction.Interact, Target: target, Choice: 1);
                long first = combat.State.NextEvent; var expected = sample.GetProperty("expected")[index++];
                Assert.Equal(expected.GetProperty("valid").GetBoolean(), world.SubmitDungeon(intent).Success);
                Assert.Equal(expected.GetProperty("clock").GetInt64(), combat.Clock); Assert.Equal(expected.GetProperty("hp").GetInt32(), hero.Health);
                Assert.Equal(expected.GetProperty("gold").GetInt32(), world.Count("gold"));
                var updated = puzzle == null ? null : combat.State.Services.Puzzles.Single(p => p.Plaque == puzzle.Plaque);
                Assert.Equal(expected.GetProperty("progress").GetInt32(), updated?.Progress ?? 0);
                Assert.Equal(expected.GetProperty("completed").GetBoolean(), updated?.Completed ?? false);
                var current = combat.State.Services.Nodes.Single(n => n.Id == node.Id);
                Assert.Equal(expected.GetProperty("spent").GetBoolean(), current.UsedBy != 0);
                Assert.Equal(expected.GetProperty("pending").GetBoolean(), current.Fixture?.Pending != null);
                Assert.Equal(expected.GetProperty("fell").GetInt32(), combat.State.Events.Count(e => e.Sequence >= first && e.Kind == "FixtureFell"));
                Assert.Equal(expected.GetProperty("hits").GetInt32(), combat.State.Events.Count(e => e.Sequence >= first && e.Kind == "DamageTaken"));
                TacticalValidation.Validate(world.Plan, world.State); commands++;
            }
        }
        Assert.Equal(10, commands);
    }

    [Fact]
    public void BellPuzzleResetsWrongNotesAndPaysItsOneRewardWithoutConsumingInspectionTime()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var plaque = hero.Position.Offset(2, 0); var puzzle = combat.SpawnBellPuzzle("test:bells", plaque, 37);
        hero.Position = world.State.Position = plaque; world.Reveal();
        string before = JsonSerializer.Serialize(world.State);
        Assert.Contains("Ring bells", combat.DungeonServiceDescription(puzzle.Plaque, hero.Id));
        Assert.Empty(combat.DungeonServiceChoices(puzzle.Plaque, hero.Id)); Assert.Equal(before, JsonSerializer.Serialize(world.State));
        int wrong = puzzle.Sequence[0] % 3 + 1;
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: puzzle.Bells[wrong - 1], Choice: 1)).Success);
        Assert.Equal(0, combat.State.Services.Puzzles.Single(p => p.Plaque == puzzle.Plaque).Progress);
        foreach (int number in puzzle.Sequence)
            Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: puzzle.Bells[number - 1], Choice: 1)).Success);
        Assert.Equal(400, combat.Clock); Assert.Equal(5, world.Count("gold"));
        Assert.True(combat.State.Services.Puzzles.Single(p => p.Plaque == puzzle.Plaque).Completed);
        before = JsonSerializer.Serialize(world.State);
        Assert.False(combat.Commit(new(TacticalAction.Interact, Target: puzzle.Bells[0], Choice: 1)).Success);
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void FallingFixtureLocksOnePaidCrossAndHitsItsOwnerAndOtherOccupantsExactlyOnce()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var node = combat.SpawnDungeonService(DungeonServiceKind.FallingFixture, "test:fixture", hero.Position.Offset(1, 0));
        var target = ReplaceRegular(world, "core:enemy/mossbound_guard"); target.Position = node.Fixture!.Center;
        target.Tactics!.InnateStats = target.Tactics.InnateStats! with { Defense = 0 };
        hero.Position = world.State.Position = node.Position; world.Reveal();
        var intent = new TacticalIntent(TacticalAction.Interact, Target: node.Id, Choice: 1);
        var preview = combat.Preview(intent); Assert.True(preview.Valid, preview.Reason); Assert.Equal(4, preview.Damage); Assert.Equal(5, preview.Cells.Count);
        int hp = hero.Health, enemyHp = target.Health;
        Assert.True(world.SubmitDungeon(intent).Success); Assert.Equal(100, combat.Clock);
        Assert.Equal(hp - 4, hero.Health); Assert.Equal(enemyHp - 4, target.Health);
        Assert.Single(combat.State.Events, e => e.Kind == "FixtureFell");
        Assert.Null(combat.State.Services.Nodes.Single(n => n.Id == node.Id).Fixture!.Pending);
        Assert.False(combat.Commit(intent).Success); Assert.Empty(combat.VisibleAttacks(world.Visible));
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void SavedFixtureFallsOnItsActualFloorAfterTheOwnerPaysForIndividualStairs()
    {
        using var world = Floor(ClassKind.Ranger); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var exit = world.State.Dungeon!.Floors[0].Exit;
        foreach (var p in TacticalCombat.FixtureCross(exit))
        {
            world.Chunks.Edit(p, p == exit ? Material.StairsDown : Material.Floor);
            world.State.Dungeon.Terrain.Remove(Roag.Core.Dungeons.DungeonRunState.CellKey(p));
        }
        hero.Position = world.State.Position = exit;
        var target = ReplaceRegular(world, "core:enemy/mossbound_guard"); target.Position = exit.Offset(1, 0);
        target.Tactics!.InnateStats = target.Tactics.InnateStats! with { Defense = 0 };
        var node = combat.SpawnDungeonService(DungeonServiceKind.FallingFixture, "test:offscreen-fixture", exit.Offset(-1, 0)); world.Reveal();
        Assert.True(world.SubmitDungeon(new(TacticalAction.Interact, Target: node.Id, Choice: 1)).Success);
        Assert.InRange(combat.Clock, 1, 99); Assert.NotNull(combat.State.Services.Nodes.Single(n => n.Id == node.Id).Fixture!.Pending);
        Assert.Contains(combat.VisibleTimeline(), t => t.Kind == ScheduledKind.FixtureImpact);
        int hp = hero.Health, enemyHp = target.Health;
        string root = Path.Combine(Path.GetTempPath(), "roag-fixture-" + Guid.NewGuid().ToString("N"));
        try
        {
            string slot = Path.Combine(root, "campaign.json"); world.Save(slot); using var loaded = WorldSession.Load(slot);
            Assert.True(world.SubmitDungeon(new(TacticalAction.UseStairs)).Success);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.UseStairs)).Success);
            Assert.Equal(2, hero.Position.Floor); Assert.Equal(hp, hero.Health); Assert.Equal(enemyHp - 4, target.Health);
            Assert.DoesNotContain(combat.VisibleAttacks(world.Visible), a => a.Ability == "core:ability/falling_fixture");
            Assert.Equal(JsonSerializer.Serialize(combat.State), JsonSerializer.Serialize(loaded.DungeonCombat.State));
            Assert.Equal(target.Health, loaded.State.Actors.Single(a => a.Id == target.Id).Health);
            TacticalValidation.Validate(world.Plan, world.State); TacticalValidation.Validate(loaded.Plan, loaded.State);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Fact]
    public void ForgedFixtureSchedulesAndBellSequencesCannotBeSaved()
    {
        using var world = Floor(); var hero = world.Party.Single(); var combat = world.DungeonCombat;
        var puzzle = combat.SpawnBellPuzzle("test:bell-validation", hero.Position.Offset(2, 0), 37);
        int puzzleIndex = combat.State.Services.Puzzles.FindIndex(p => p.Plaque == puzzle.Plaque);
        combat.State.Services.Puzzles[puzzleIndex] = puzzle with { Sequence = [1, 1, 3] };
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        combat.State.Services.Puzzles[puzzleIndex] = puzzle;
        var work = combat.State.Queue.Schedule(ScheduledKind.FixtureImpact, 100, referenceId: 999);
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        combat.State.Queue.Cancel(work.Sequence); TacticalValidation.Validate(world.Plan, world.State);
    }
}
