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
            var door = Assert.Single(layout.Doors);
            Assert.DoesNotContain(door.Room, layout.CriticalRooms);
            Assert.Contains(door.Position, layout.Rooms[door.Room].Shape.Sockets.Select(s => layout.Rooms[door.Room].At(new(s.X, s.Y))));
            foreach (var room in layout.Rooms)
            {
                Assert.True(room.X > 0 && room.Y > 0 && room.X + room.Shape.Width < layout.Width && room.Y + room.Shape.Height < layout.Height);
                for (int y = 0; y < room.Shape.Height; y++) for (int x = 0; x < room.Shape.Width; x++)
                {
                    char actual = layout.Rows[room.Y + y][room.X + x], original = room.Shape.Rows[y][x];
                    if (!room.Shape.Editable.Contains(new(x + 1, y + 1))) Assert.Equal(original, actual);
                    else Assert.Contains(actual, biome.Strategy == "canals" ? original == '~' ? "~." : "#~" : original == '#' ? "#." : "~#");
                }
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

    [Fact]
    public void BiomeCorridorsKeepConnectedLanesAndOptionalBranchesOutsideAuthoredRooms()
    {
        var kinds = new HashSet<DungeonBranchKind>(); var banks = new HashSet<string>(); var pockets = new HashSet<string>(); int absent = 0;
        foreach (var biome in DungeonCatalog.Default.Biomes) for (int seed = -8; seed < 8; seed++)
        {
            var layout = DungeonLayoutGenerator.Generate(seed, biome.Id);
            foreach (var corridor in layout.Corridors)
            {
                Assert.Equal(layout.Rooms[corridor.From].Socket(corridor.From / 4 == corridor.To / 4 ? "E" : "S"), corridor.Cells[0]);
                Assert.Equal(layout.Rooms[corridor.To].Socket(corridor.From / 4 == corridor.To / 4 ? "W" : "N"), corridor.Cells[^1]);
                for (int i = 1; i < corridor.Cells.Length; i++) Assert.Equal(1,
                    Math.Abs(corridor.Cells[i].X - corridor.Cells[i - 1].X) + Math.Abs(corridor.Cells[i].Y - corridor.Cells[i - 1].Y));
                if (corridor.Banks.Length > 0) banks.Add(biome.Strategy);
                if (corridor.Pockets.Length > 0) pockets.Add(biome.Strategy);
                foreach (var p in corridor.Alcoves.Concat(corridor.Banks).Concat(corridor.Pockets)) Assert.Equal('.', layout.Rows[p.Y][p.X]);
            }
            if (layout.Branch is not { } branch) { absent++; continue; }
            kinds.Add(branch.Kind); var primary = layout.Corridors.SelectMany(c => c.Cells).ToHashSet();
            Assert.All(branch.Cells, p =>
            {
                Assert.Equal('.', layout.Rows[p.Y][p.X]); Assert.DoesNotContain(p, primary);
                Assert.DoesNotContain(layout.Rooms, r => p.X >= r.X && p.X < r.X + r.Shape.Width && p.Y >= r.Y && p.Y < r.Y + r.Shape.Height);
            });
        }
        Assert.Equal(5, kinds.Count); Assert.True(absent > 0);
        Assert.Equal(new[] { "archive", "canals" }, banks.Order());
        Assert.Equal(new[] { "courtyards", "forge", "observatory" }, pockets.Order());
    }
}
