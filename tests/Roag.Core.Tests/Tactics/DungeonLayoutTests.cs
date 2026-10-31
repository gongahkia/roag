using System.Text.Json;
using Roag.Core.Dungeons;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class DungeonLayoutTests
{
    [Fact]
    public void NativeLayoutsKeepWholeAuthoredFootprintsAndReachEveryRoomAcrossAllBiomes()
    {
        foreach (var biome in DungeonCatalog.Default.Biomes) for (int seed = -8; seed < 8; seed++)
        {
            var layout = DungeonLayoutGenerator.Generate(seed, biome.Id);
            Assert.Equal(140, layout.Width); Assert.Equal(90, layout.Height);
            Assert.InRange(layout.Rooms.Length, 8, 12);
            Assert.Contains("start", layout.Rooms[0].Shape.Roles);
            Assert.Contains("elite", layout.Rooms[^1].Shape.Roles);
            foreach (var room in layout.Rooms)
            {
                Assert.True(room.X > 0 && room.Y > 0 && room.X + room.Shape.Width < layout.Width && room.Y + room.Shape.Height < layout.Height);
                for (int y = 0; y < room.Shape.Height; y++)
                    Assert.Equal(room.Shape.Rows[y], layout.Rows[room.Y + y].Substring(room.X, room.Shape.Width));
            }
            var seen = new HashSet<RoomPoint> { layout.Start }; var queue = new Queue<RoomPoint>(); queue.Enqueue(layout.Start);
            while (queue.TryDequeue(out var point))
                foreach (var (dx, dy) in new[] { (0, -1), (1, 0), (0, 1), (-1, 0) })
                {
                    var next = new RoomPoint(point.X + dx, point.Y + dy);
                    // Barricades are destructible routes; water/walls remain impassable.
                    if (next.X < 0 || next.X >= layout.Width || next.Y < 0 || next.Y >= layout.Height
                        || layout.Rows[next.Y][next.X] is '#' or '~' || !seen.Add(next)) continue;
                    queue.Enqueue(next);
                }
            Assert.Contains(layout.Elite, seen);
            foreach (var room in layout.Rooms) foreach (var socket in room.Shape.Sockets)
                Assert.Contains(room.At(new(socket.X, socket.Y)), seen);
        }
    }

    [Fact]
    public void NativeLayoutRandomnessIsDeterministicAndUsesTheOriginalCatalog()
    {
        var a = DungeonLayoutGenerator.Generate(42, "core:biome/overgrown_ruins");
        var b = DungeonLayoutGenerator.Generate(42, "core:biome/overgrown_ruins");
        var other = DungeonLayoutGenerator.Generate(43, "core:biome/overgrown_ruins");
        Assert.Equal(JsonSerializer.Serialize(a), JsonSerializer.Serialize(b));
        Assert.NotEqual(JsonSerializer.Serialize(a), JsonSerializer.Serialize(other));
        Assert.All(a.Rooms, room => Assert.True(DungeonCatalog.Default.Rooms.ContainsKey(room.Shape.Id)));
        Assert.Throws<InvalidOperationException>(() => DungeonLayoutGenerator.Generate(42, "missing"));
    }
}
