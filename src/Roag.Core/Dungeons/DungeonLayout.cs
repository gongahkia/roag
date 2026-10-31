using Roag.Core.Tactics;
using Roag.Core.World;

namespace Roag.Core.Dungeons;

public sealed record DungeonRoomPlacement(int Index, int X, int Y, PlacedRoom Shape, string Role)
{
    public RoomPoint At(RoomPoint local) => new(X + local.X - 1, Y + local.Y - 1);
    public RoomPoint Socket(string facing)
    {
        var socket = Shape.Sockets.Single(s => s.Facing == facing);
        return At(new(socket.X, socket.Y));
    }
}

/// <summary>Construction result, consumed into actual world chunks; never another live terrain authority.</summary>
public sealed record DungeonLayout(string Biome, int Width, int Height, string[] Rows,
    DungeonRoomPlacement[] Rooms, RoomPoint Start, RoomPoint Elite, int[] CriticalRooms);

public static class DungeonLayoutGenerator
{
    public static DungeonLayout Generate(int seed, string biomeId)
    {
        var catalog = DungeonCatalog.Default;
        _ = catalog.Biomes.Single(b => b.Id == biomeId);
        var rules = catalog.Generation;
        var random = new RoagRandom((int)(WorldRandom.Hash(seed, 0, salt: 9400) % (RoagRandom.Modulus - 1)) + 1);
        int count = rules.RoomMin + random.NextOneBased(rules.RoomMax - rules.RoomMin + 1) - 1;
        const int columns = 4, slotWidth = 32, slotHeight = 26, margin = 6;
        var grid = Enumerable.Range(0, rules.Height).Select(_ => Enumerable.Repeat('#', rules.Width).ToArray()).ToArray();
        var rooms = new List<DungeonRoomPlacement>();
        var edges = new List<(int From, int To)>();
        for (int i = 0; i < count; i++)
        {
            string role = i == 0 ? "start" : i == count - 1 ? "elite"
                : random.NextOneBased(5) switch { 1 => "treasure", 2 => "landmark", _ => "combat" };
            var choices = catalog.ForRole(biomeId, role);
            var room = choices[random.NextOneBased(choices.Count) - 1];
            var shape = room.Transform(room.Rotations[random.NextOneBased(room.Rotations.Length) - 1], room.Mirror && random.NextOneBased(2) == 1);
            int x = margin + i % columns * slotWidth + (slotWidth - shape.Width) / 2;
            int y = margin + i / columns * slotHeight + (slotHeight - shape.Height) / 2;
            var placement = new DungeonRoomPlacement(i, x, y, shape, role); rooms.Add(placement);
            for (int yy = 0; yy < shape.Height; yy++) for (int xx = 0; xx < shape.Width; xx++)
                grid[y + yy][x + xx] = shape.Rows[yy][xx];
        }
        // A connected spine with extra physical loops. Corridors connect authored
        // sockets through the gaps between rooms, preserving every room footprint.
        for (int i = 1; i < rooms.Count; i++)
        {
            if (i % columns > 0) Connect(rooms[i - 1], rooms[i], horizontal: true);
            else Connect(rooms[i - columns], rooms[i], horizontal: false);
            if (i >= columns && i % columns > 0 && random.NextOneBased(3) == 1)
                Connect(rooms[i - columns], rooms[i], horizontal: false);
        }
        RoomPoint Marker(DungeonRoomPlacement room, string kind)
        {
            if (!room.Shape.Markers.TryGetValue(kind, out var markers) || markers.Length == 0)
                throw new InvalidDataException("Required room anchor is missing: " + kind);
            return room.At(markers[0]);
        }
        var previous = new Dictionary<int, int> { [0] = -1 }; var queue = new Queue<int>(); queue.Enqueue(0);
        while (queue.TryDequeue(out int index))
            foreach (int next in edges.Where(e => e.From == index || e.To == index).Select(e => e.From == index ? e.To : e.From).Order())
                if (previous.TryAdd(next, index)) queue.Enqueue(next);
        var critical = new List<int>();
        for (int index = rooms.Count - 1; index >= 0; index = previous[index]) critical.Add(index);
        return new(biomeId, rules.Width, rules.Height, grid.Select(row => new string(row)).ToArray(), rooms.ToArray(),
            Marker(rooms[0], "player"), Marker(rooms[^1], "elite"), critical.ToArray());

        void Connect(DungeonRoomPlacement first, DungeonRoomPlacement second, bool horizontal)
        {
            edges.Add((first.Index, second.Index));
            var start = first.Socket(horizontal ? "E" : "S"); var end = second.Socket(horizontal ? "W" : "N");
            if (horizontal)
            {
                int middle = (first.X + first.Shape.Width - 1 + second.X) / 2;
                Segment(start, new(middle, start.Y)); Segment(new(middle, start.Y), new(middle, end.Y)); Segment(new(middle, end.Y), end);
            }
            else
            {
                int middle = (first.Y + first.Shape.Height - 1 + second.Y) / 2;
                Segment(start, new(start.X, middle)); Segment(new(start.X, middle), new(end.X, middle)); Segment(new(end.X, middle), end);
            }
        }
        void Segment(RoomPoint first, RoomPoint last)
        {
            int x = first.X, y = first.Y;
            while (true)
            {
                if (grid[y][x] == '#') grid[y][x] = '.';
                if (x == last.X && y == last.Y) break;
                x += Math.Sign(last.X - x); y += Math.Sign(last.Y - y);
            }
        }
    }
}
