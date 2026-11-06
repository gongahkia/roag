using System.Text.Json;
using Roag.Core.Dungeons;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class DungeonComplexBlueprintTests
{
    [Theory]
    [InlineData(1)] [InlineData(42)] [InlineData(-2147483648)]
    public void OriginalRouteGraphHasFixedUnclippedPhysicalRoomsAndIndependentIdentities(int seed)
    {
        var plan = WorldPlan.Generate(seed, false, floorCount: 8);
        var parcels = DungeonParcelPlanner.Reserve(plan);
        string before = JsonSerializer.Serialize(plan);
        foreach (var parcel in parcels.Take(2))
        {
            var blueprint = DungeonComplexPlanner.Generate(plan, parcel);
            Assert.Equal(JsonSerializer.Serialize(blueprint), JsonSerializer.Serialize(DungeonComplexPlanner.Generate(plan, parcel)));
            Assert.Equal(18, blueprint.Floors.Length);
            Assert.Equal(7, blueprint.Floors.Select(f => f.Layout.Biome).Distinct().Count());
            Assert.Equal(17, blueprint.Links.Length);
            Assert.Equal(0, blueprint.SurfaceEntrance.Floor);
            Assert.Equal(blueprint.Floors[0].Entry, WorldStairs.Destination(blueprint.SurfaceEntrance, Material.StairsDown));
            var identities = new HashSet<WorldRoomId>();
            foreach (var floor in blueprint.Floors)
            {
                var metadata = floor.Describe();
                Assert.Equal(140, metadata.Width); Assert.Equal(90, metadata.Height);
                Assert.InRange(metadata.Rooms.Length, 8, 12);
                Assert.True(parcel.Contains(metadata.Origin)); Assert.True(parcel.Contains(metadata.Origin.Offset(139, 89)));
                foreach (var room in metadata.Rooms)
                {
                    Assert.True(metadata.Contains(room.Origin)); Assert.True(metadata.Contains(room.Origin.Offset(room.Width - 1, room.Height - 1)));
                    var identity = floor.RoomAt(room.Origin, parcel.Id);
                    Assert.NotNull(identity); Assert.True(identities.Add(identity.Value));
                    Assert.Equal(parcel.Id, identity.Value.Place);
                    Assert.Equal(identity, metadata.RoomAt(room.Origin, parcel.Id));
                }
                foreach (var other in blueprint.Floors.Where(f => f != floor && f.Origin.Floor == floor.Origin.Floor))
                    Assert.True(metadata.Origin.X + metadata.Width <= other.Origin.X || other.Origin.X + other.Layout.Width <= metadata.Origin.X
                        || metadata.Origin.Y + metadata.Height <= other.Origin.Y || other.Origin.Y + other.Layout.Height <= metadata.Origin.Y);
                if (!floor.Elite)
                {
                    var lower = blueprint.Floors.Single(f => f.Branch == floor.Branch && f.Elite);
                    Assert.Equal(lower.Entry, WorldStairs.Destination(floor.Exit, Material.StairsDown));
                    Assert.Equal(floor.Exit, WorldStairs.Destination(lower.Entry, Material.StairsUp));
                }
            }
            Assert.Equal(7, blueprint.Finale.Origin.Floor);
            Assert.Equal(DungeonCatalog.Default.Finale.Id, blueprint.Finale.Rooms.Single().Definition);
            Assert.True(parcel.Contains(blueprint.Finale.Origin));
            foreach (var first in DungeonRoutes.ForStage(1, []))
            {
                var firstLink = blueprint.Links.Single(l => l.From == blueprint.Floors[0].Branch && l.Route == first.Id);
                var choices = blueprint.Links.Where(l => l.From == firstLink.To).Select(l => l.Route).ToArray();
                Assert.Equal(DungeonRoutes.ForStage(2, [first.Id]).Select(r => r.Id), choices);
                foreach (var link in blueprint.Links.Where(l => l.From == firstLink.To))
                    Assert.Contains(blueprint.Links, l => l.From == link.To && l.To == DungeonComplexBlueprint.FinalBranch);
            }
        }
        Assert.Equal(before, JsonSerializer.Serialize(plan));
    }

    [Fact]
    public void PhysicalRoomIdsSelectTheActualRegionAndPlaceAndSurviveMetadataRoundTrip()
    {
        var first = new DungeonFloorState(1, "test:first", new(100, 100, 1), 140, 90,
            [new(0, "test:start", new(110, 110, 1), 7, 7, 0, false)], new(111, 111, 1), new(120, 120, 1));
        var second = first with
        {
            Origin = new(400, 100, 1), Branch = 21, Entry = new(411, 111, 1), Exit = new(420, 120, 1),
            Rooms = [first.Rooms[0] with { Origin = new(410, 110, 1) }]
        };
        var run = new DungeonRunState { Floors = [first, second] };
        Assert.Equal(new WorldRoomId(RoomArea.Dungeon, 0, 1, 1), run.RoomAt(first.Entry));
        Assert.Equal(new WorldRoomId(RoomArea.Dungeon, 3, 1, 337), run.RoomAt(second.Entry, 3));
        Assert.NotEqual(run.RoomAt(second.Entry, 3), run.RoomAt(second.Entry, 4));
        Assert.Null(run.RoomAt(new(300, 100, 1), 3));
        Assert.Null(run.RoomAt(second.Entry with { Floor = 2 }, 3));
        Assert.Null(run.RoomAt(second.Origin, 3)); // A corridor is not a room.
        var copy = JsonSerializer.Deserialize<DungeonRunState>(JsonSerializer.Serialize(run))!;
        Assert.Equal(run.RoomAt(second.Entry, 3), copy.RoomAt(second.Entry, 3));
        // Existing bounded floor JSON retains its original shape and identity.
        Assert.DoesNotContain("\"Branch\"", JsonSerializer.Serialize(first), StringComparison.Ordinal);
    }
}
