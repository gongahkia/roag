using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class DungeonComplexTerrainTests
{
    [Fact]
    public void InstallationPaintsActualPairedStairsAndSealedRoutesWithoutTimeOrDiscovery()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        Assert.DoesNotContain("\"Complexes\"", JsonSerializer.Serialize(world.State), StringComparison.Ordinal);
        var blueprint = Blueprint(world); var clock = world.DungeonCombat.Clock;
        var positions = world.State.Actors.Select(a => a.Position).ToArray();
        var inventory = JsonSerializer.Serialize(world.State.Inventory);
        var discovered = world.State.Chunks.Sum(c => c.Discovered.Count);
        var complex = world.InstallDungeonComplexTerrain(blueprint);
        Assert.Equal(clock, world.DungeonCombat.Clock);
        Assert.Equal(positions, world.State.Actors.Select(a => a.Position));
        Assert.Equal(inventory, JsonSerializer.Serialize(world.State.Inventory));
        Assert.Equal(discovered, world.State.Chunks.Sum(c => c.Discovered.Count));
        Assert.Equal(blueprint.Floors[0].Entry, world.StairLanding(blueprint.SurfaceEntrance));
        foreach (var upper in blueprint.Floors.Where(f => !f.Elite))
        {
            var lower = blueprint.Floors.Single(f => f.Branch == upper.Branch && f.Elite);
            Assert.Equal(lower.Entry, world.StairLanding(upper.Exit));
            Assert.Equal(upper.Exit, world.StairLanding(lower.Entry));
        }
        foreach (var passage in blueprint.Passages)
        {
            Assert.Equal(passage.Bottom, world.StairLanding(passage.Top));
            Assert.Equal(passage.Top, world.StairLanding(passage.Bottom));
            Assert.Equal(Material.Wall, world.Chunks.At(passage.Barrier));
            foreach (var route in passage.Routes) Assert.Equal(Material.Wall, world.Chunks.At(route.Barrier));
            Assert.All(passage.UpperCells.Where(p => p != passage.Top && p != passage.Barrier),
                p => Assert.Equal(Material.Floor, world.Chunks.At(p)));
        }
        Assert.Equal(19, complex.Run.Floors.Count);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Fact]
    public void TwoComplexesKeepIndependentRoomsOverlaysEditsAndExactOrdinarySaveContinuation()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var first = world.InstallDungeonComplexTerrain(Blueprint(world));
        var second = world.InstallDungeonComplexTerrain(Blueprint(world, 2));
        foreach (var complex in new[] { first, second })
            foreach (var floor in complex.Run.Floors)
            {
                Assert.Equal(floor.RoomAt(floor.Entry, complex.Parcel.Id), world.RoomAt(floor.Entry));
                Assert.Equal(complex.Parcel.Id, world.RoomAt(floor.Entry)!.Value.Place);
                Assert.Equal(world.RoomAt(floor.Entry), WorldSession.SnapshotRoomAt(world.State, world.Chunks.Generator, floor.Entry));
            }
        var key = first.Run.Terrain.First(p => p.Value == 'm' && world.Chunks.At(Position(p.Key)) == Material.Floor).Key;
        var mist = Position(key);
        Assert.True(world.DungeonMistBlocks(mist));
        Assert.True(WorldSession.SnapshotDungeonMistBlocks(world.State, world.Chunks, mist));
        world.Chunks.Edit(mist, Material.Wall);
        Assert.Null(world.DungeonTerrainAt(mist));
        Assert.False(WorldSession.SnapshotDungeonMistBlocks(world.State, world.Chunks, mist));
        world.Chunks.Edit(mist, Material.Floor);
        Assert.True(world.DungeonMistBlocks(mist));
        var changed = second.Run.Floors[0].Entry.Offset(1, 0);
        world.Chunks.Edit(changed, Material.Wall);
        string before = JsonSerializer.Serialize(world.State);
        Assert.Throws<InvalidOperationException>(() => world.InstallDungeonComplexTerrain(Blueprint(world, 2)));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));

        string directory = Path.Combine(Path.GetTempPath(), "roag-physical-complex-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "campaign.json");
        try
        {
            world.Save(path);
            using var loaded = WorldSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(Material.Wall, loaded.Chunks.At(changed));
            Assert.Equal(world.RoomAt(second.Run.Floors[0].Entry), loaded.RoomAt(second.Run.Floors[0].Entry));
            Assert.True(loaded.DungeonMistBlocks(mist));
            Assert.True(world.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.True(loaded.SubmitDungeon(new(TacticalAction.Wait)).Success);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void SplitMembersKeepPrivateComplexVisionAndTheirOwnSavedUtilityRoom()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var first = world.InstallDungeonComplexTerrain(Blueprint(world));
        var second = world.InstallDungeonComplexTerrain(Blueprint(world, 2));
        var hero = world.Party.Single();
        var companion = world.State.Actors.Single(a => a.Id == world.NativeAdventurers[0].Actor);
        hero.Position = world.State.Position = new[] { companion.Position.Offset(0, -1), companion.Position.Offset(1, 0),
            companion.Position.Offset(0, 1), companion.Position.Offset(-1, 0) }.First(p => world.CanEnter(p, hero.Id));
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Recruit, Target: companion.Id)).Success);
        Assert.True(world.SplitDungeonMember(companion.Id).Success);
        // Explicit unit placement isolates simultaneous ownership/perception;
        // this is not a rendered cross-world travel acceptance journey.
        hero.Position = first.Run.Floors[0].Entry;
        companion.Position = second.Run.Floors[0].Entry;
        var firstRoom = world.RoomAt(hero.Position); var secondRoom = world.RoomAt(companion.Position);
        Assert.Equal(1, firstRoom!.Value.Place); Assert.Equal(2, secondRoom!.Value.Place);
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        Assert.Contains(hero.Position, world.Visible); Assert.DoesNotContain(companion.Position, world.Visible);
        Assert.True(world.SelectDungeonMember(companion.Id).Success);
        Assert.Contains(companion.Position, world.Visible); Assert.DoesNotContain(hero.Position, world.Visible);
        companion.Tactics!.Supplies.Utility = new(UtilityKind.ReturnToken, new(companion.Position, secondRoom.Value));
        TacticalValidation.Validate(world.Plan, world.State);
        var marker = companion.Tactics.Supplies.Utility.Marker!;
        companion.Tactics.Supplies.Utility = new(UtilityKind.ReturnToken, marker with { Room = firstRoom.Value });
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
        companion.Tactics.Supplies.Utility = new(UtilityKind.ReturnToken, marker);
        string directory = Path.Combine(Path.GetTempPath(), "roag-private-complex-" + Guid.NewGuid().ToString("N"));
        try
        {
            string path = Path.Combine(directory, "campaign.json"); world.Save(path);
            using var loaded = WorldSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(world.State), JsonSerializer.Serialize(loaded.State));
            Assert.Equal(secondRoom, loaded.RoomAt(loaded.DungeonMember.Position));
            Assert.DoesNotContain(hero.Position, loaded.Visible);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void PhysicalComplexEntranceUsesTheOrdinaryPaidMovementAndBlockedLandingRules()
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var blueprint = Blueprint(world); world.InstallDungeonComplexTerrain(blueprint);
        var hero = world.Party.Single(); var entrance = blueprint.SurfaceEntrance; var landing = blueprint.Floors[0].Entry;
        hero.Position = world.State.Position = entrance.Offset(-1, 0);
        Assert.True(world.SelectDungeonMember(hero.Id).Success);
        long clock = world.DungeonCombat.Clock;
        world.Chunks.Edit(landing, Material.Wall);
        Assert.False(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        Assert.Equal(clock, world.DungeonCombat.Clock); Assert.Equal(entrance.Offset(-1, 0), hero.Position);
        world.Chunks.Edit(landing, Material.StairsUp);
        Assert.True(world.SubmitDungeon(new(TacticalAction.Move, Facing.East)).Success);
        Assert.Equal(landing, hero.Position); Assert.True(world.DungeonCombat.Clock > clock);
        Assert.NotNull(world.RoomAt(hero.Position));
        Assert.DoesNotContain(entrance, world.Visible);
        Assert.True(world.SubmitDungeon(new(TacticalAction.UseStairs)).Success);
        Assert.Equal(entrance, hero.Position);
        TacticalValidation.Validate(world.Plan, world.State);
    }

    [Theory]
    [InlineData("edited")] [InlineData("observed")] [InlineData("occupied")]
    public void InitialConstructionRejectsExistingPlayerWorldStateBeforeAnyPainting(string kind)
    {
        using var world = WorldSession.CreateNativeCampaign(42); var blueprint = Blueprint(world);
        var cell = blueprint.Floors[0].Entry;
        if (kind == "edited") world.Chunks.Edit(cell, Material.Wall);
        else if (kind == "observed") world.Chunks.Delta(cell.Chunk).Discovered.Add(cell.LocalIndex);
        else world.Party.Single().Position = world.State.Position = cell;
        string before = JsonSerializer.Serialize(world.State);
        Assert.Throws<InvalidOperationException>(() => world.InstallDungeonComplexTerrain(blueprint));
        Assert.Equal(before, JsonSerializer.Serialize(world.State));
        Assert.Null(world.State.NativeCampaign!.Complexes);
    }

    [Theory]
    [InlineData("parcel")] [InlineData("seed")] [InlineData("room")] [InlineData("branch")] [InlineData("overlay")] [InlineData("duplicate")]
    public void SaveValidationRejectsMovedOrAlteredComplexOwnership(string kind)
    {
        using var world = WorldSession.CreateNativeCampaign(42);
        var complex = world.InstallDungeonComplexTerrain(Blueprint(world)); var registry = world.State.NativeCampaign!.Complexes!;
        switch (kind)
        {
            case "parcel": registry[0] = complex with { Parcel = complex.Parcel with { X = complex.Parcel.X + 1 } }; break;
            case "seed": registry[0] = complex with { Seed = complex.Seed + 1 }; break;
            case "room":
                var floor = complex.Run.Floors[0];
                complex.Run.Floors[0] = floor with { Rooms = [floor.Rooms[0] with { Width = floor.Rooms[0].Width - 1 }, .. floor.Rooms.Skip(1)] }; break;
            case "branch": complex.Run.Floors[0] = complex.Run.Floors[0] with { Branch = 21 }; break;
            case "overlay": complex.Run.Terrain[DungeonRunState.CellKey(world.Party.Single().Position)] = 'm'; break;
            case "duplicate": registry.Add(complex); break;
        }
        Assert.Throws<InvalidDataException>(() => TacticalValidation.Validate(world.Plan, world.State));
    }

    private static DungeonComplexBlueprint Blueprint(WorldSession world, int id = 1)
        => DungeonComplexPlanner.Generate(world.Plan, DungeonParcelPlanner.Reserve(world.Plan)[id - 1]);

    private static WorldPosition Position(long key) => new((int)(key % WorldPlan.Size),
        (int)(key / WorldPlan.Size % WorldPlan.Size), (int)(key / WorldPlan.Size / WorldPlan.Size));
}
