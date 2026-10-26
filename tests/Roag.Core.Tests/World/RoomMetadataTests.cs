using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class RoomMetadataTests
{
    [Fact]
    public void WfcRoomsExcludeExteriorWallsAndDoorThresholds()
    {
        string[] rows = ["...........", ".#########.", ".#...#...#.", ".#...+...#.", ".#...#...#.", ".##+######.", "..........."];
        var tiles = rows.SelectMany(row => row.Select(c => c switch { '#' => Material.Wall, '+' => Material.Door, _ => Material.Floor })).ToArray();
        var rooms = ChunkGenerator.EnclosedRooms(tiles, rows[0].Length, rows.Length);
        int At(int x, int y) => rooms[y * rows[0].Length + x];
        Assert.True(At(2, 2) > 0);
        Assert.True(At(6, 2) > 0);
        Assert.NotEqual(At(2, 2), At(6, 2));
        Assert.Equal(At(2, 2), At(4, 4));
        Assert.Equal(At(6, 2), At(8, 4));
        Assert.Equal(0, At(5, 3)); Assert.Equal(0, At(3, 5)); Assert.Equal(0, At(0, 0)); Assert.Equal(0, At(1, 1));
    }

    [Theory]
    [InlineData("bsp-fort")]
    [InlineData("bsp-workings")]
    [InlineData("ring-sanctuary")]
    [InlineData("caverns")]
    public void StructuralLayoutsKeepDistinctRoomsAndUnassignedConnectingPassages(string family)
    {
        var cells = StructuralLayouts.Generate(family, 173, out var rooms);
        Assert.Equal(cells.Length, rooms.Length);
        Assert.True(rooms.Where(r => r > 0).Distinct().Count() >= 5);
        Assert.Contains(Enumerable.Range(0, cells.Length), i => cells[i] == Material.Floor && rooms[i] == 0);
        Assert.All(Enumerable.Range(0, cells.Length).Where(i => rooms[i] > 0), i => Assert.Equal(Material.Floor, cells[i]));
    }

    [Fact]
    public void GeneratedRoomIdsSurviveFootprintCacheEvictionAndSeparateFloors()
    {
        var plan = WorldPlan.Generate(42); var generator = new ChunkGenerator(plan);
        var site = plan.Sites.First(s => s.Purpose == "fort");
        var footprint = generator.GenerateFootprint(false, site.Id, 1);
        int tile = Array.FindIndex(footprint.RoomCells, r => r > 0);
        var p = new WorldPosition(site.Center.X - 32 + tile % 64, site.Center.Y - 32 + tile / 64, 1);
        var room = generator.RoomAt(p);
        Assert.NotNull(room);
        foreach (var other in plan.Sites.Take(10)) generator.GenerateFootprint(false, other.Id, 2);
        Assert.Equal(room, generator.RoomAt(p));
        Assert.Equal(footprint.RoomCells, generator.GenerateFootprint(false, site.Id, 1).RoomCells);
        Assert.NotEqual(room, generator.RoomAt(p with { Floor = 2 }));
        Assert.Null(generator.RoomAt(new(100, 100, 1)));
        Assert.Equal(new WorldRoomId(RoomArea.OpenSurface, 0, 0, 0), generator.RoomAt(new(100, 100)));
    }
}
