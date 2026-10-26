using Roag.Core.Expeditions;
using Roag.Core.Generation;

namespace Roag.Core.World;

internal static class StructuralLayouts
{
    private readonly record struct Room(int X, int Y, int Width, int Height)
    { public (int X, int Y) Center => (X + Width / 2, Y + Height / 2); }
    public static string Family(string purpose, int floor) => purpose switch
    {
        "mine" => floor % 2 == 1 ? "caverns" : "bsp-workings",
        "fort" => "bsp-fort",
        "shrine" when floor % 2 == 1 => "ring-sanctuary",
        _ => "overlapping-wfc"
    };
    public static Material[] Generate(string family, int seed) => Generate(family, seed, out _);
    public static Material[] Generate(string family, int seed, out int[] roomCells)
    {
        var random = new RandomStream(unchecked((uint)seed));
        int Range(int min, int max) => min + random.Next(max - min);
        var cells = Enumerable.Repeat(Material.Wall, 4096).ToArray();
        var roomsByCell = new int[4096];
        int nextRoom = 1;
        void Floor(int x, int y) { if (x is > 1 and < 62 && y is > 1 and < 62) cells[y * 64 + x] = Material.Floor; }
        void Rect(int x, int y, int w, int h) { for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) Floor(xx, yy); }
        void RoomRect(int x, int y, int w, int h)
        {
            Rect(x, y, w, h);
            int id = nextRoom++;
            for (int yy = Math.Max(2, y); yy < Math.Min(62, y + h); yy++)
                for (int xx = Math.Max(2, x); xx < Math.Min(62, x + w); xx++)
                    if (roomsByCell[yy * 64 + xx] == 0) roomsByCell[yy * 64 + xx] = id;
        }
        void Corridor((int X, int Y) a, (int X, int Y) b, bool wide = false)
        {
            int x = a.X, y = a.Y;
            while (x != b.X) { Floor(x, y); if (wide) Floor(x, y + 1); x += Math.Sign(b.X - x); }
            while (y != b.Y) { Floor(x, y); if (wide) Floor(x + 1, y); y += Math.Sign(b.Y - y); }
            Floor(x, y);
        }
        if (family == "ring-sanctuary")
        {
            int radius = Range(18, 26), width = Range(2, 5);
            for (int y = 3; y < 61; y++) for (int x = 3; x < 61; x++)
                if (Math.Abs(Math.Sqrt((x - 32) * (x - 32) + (y - 32) * (y - 32)) - radius) < width) Floor(x, y);
            RoomRect(28, 28, 9, 9);
            var points = new[] { (32, 32 - radius), (32 + radius, 32), (32, 32 + radius), (32 - radius, 32) };
            foreach (var point in points) { RoomRect(point.Item1 - 3, point.Item2 - 3, Range(6, 9), Range(6, 9)); Corridor((32, 32), point); }
        }
        else
        {
            var rooms = new List<Room>();
            void Partition(Room region, int depth)
            {
                bool vertical = region.Width > region.Height || region.Width == region.Height && random.Next(2) == 0;
                int length = vertical ? region.Width : region.Height;
                if (depth < 4 && length >= 23)
                {
                    int cut = Range(10, length - 9);
                    if (vertical) { Partition(new(region.X, region.Y, cut, region.Height), depth + 1); Partition(new(region.X + cut, region.Y, region.Width - cut, region.Height), depth + 1); }
                    else { Partition(new(region.X, region.Y, region.Width, cut), depth + 1); Partition(new(region.X, region.Y + cut, region.Width, region.Height - cut), depth + 1); }
                    return;
                }
                int w = Range(5, Math.Max(6, region.Width - 2)), h = Range(5, Math.Max(6, region.Height - 2));
                var room = new Room(region.X + Range(1, region.Width - w), region.Y + Range(1, region.Height - h), w, h);
                rooms.Add(room); RoomRect(room.X, room.Y, room.Width, room.Height);
            }
            Partition(new(3, 3, 58, 58), 0);
            var connected = new List<Room> { rooms[0] }; var remaining = rooms.Skip(1).ToList();
            while (remaining.Count > 0)
            {
                var edge = (from a in connected from b in remaining
                    orderby Math.Abs(a.Center.X - b.Center.X) + Math.Abs(a.Center.Y - b.Center.Y), b.Y, b.X select (a, b)).First();
                Corridor(edge.a.Center, edge.b.Center, family == "bsp-fort"); connected.Add(edge.b); remaining.Remove(edge.b);
            }
            for (int n = 0; n < rooms.Count / 3; n++) Corridor(rooms[random.Next(rooms.Count)].Center, rooms[random.Next(rooms.Count)].Center);
            Corridor((32, 32), rooms.MinBy(r => Math.Abs(r.Center.X - 32) + Math.Abs(r.Center.Y - 32)).Center, true);
            if (family == "caverns")
            {
                var noise = new GradientNoise(seed);
                for (int y = 3; y < 61; y++) for (int x = 3; x < 61; x++)
                {
                    if (noise.Fractal(x / 9.0, y / 9.0, 3) > -.05)
                        for (int yy = y - 1; yy <= y + 1; yy++) for (int xx = x - 1; xx <= x + 1; xx++) Floor(xx, yy);
                }
            }
            else if (family == "bsp-fort")
            {
                foreach (var room in rooms)
                {
                    int x = room.Center.X, y = room.Y - 1;
                    if (x is > 2 and < 61 && y is > 2 and < 61 && cells[(y - 1) * 64 + x] == Material.Floor) cells[y * 64 + x] = Material.Door;
                }
            }
        }
        RoomRect(28, 30, 9, 5); roomCells = roomsByCell; return cells;
    }
}
