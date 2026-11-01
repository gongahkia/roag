using Roag.Core.Tactics;

namespace Roag.Core.Dungeons;

public enum DungeonBranchKind { Spur, Braid, Switchback, Hub, FalseEnd }
public enum DungeonBranchNodeKind { Clue, Key, Plate, Brazier, Gate, Enemy, Reward }
public sealed record DungeonBranchNode(DungeonBranchNodeKind Kind, RoomPoint Position);
public sealed record DungeonBranch(DungeonBranchKind Kind, int Corridor, RoomPoint[] Cells, DungeonBranchNode[] Nodes);
public sealed record DungeonCorridor(int From, int To, RoomPoint[] Cells, RoomPoint[] Alcoves, RoomPoint[] Banks, RoomPoint[] Pockets);

public static partial class DungeonLayoutGenerator
{
    private static bool InsideRoom(RoomPoint p, IReadOnlyList<DungeonRoomPlacement> rooms)
        => rooms.Any(r => p.X >= r.X && p.X < r.X + r.Shape.Width && p.Y >= r.Y && p.Y < r.Y + r.Shape.Height);

    internal static RoomPoint[] CorridorPath(RoomPoint a, RoomPoint b, bool horizontal, string strategy, RoagRandom random)
    {
        var cells = new List<RoomPoint>();
        void Line(RoomPoint first, RoomPoint last)
        {
            int x = first.X, y = first.Y;
            while (true)
            {
                var p = new RoomPoint(x, y); if (cells.Count == 0 || cells[^1] != p) cells.Add(p);
                if (p == last) break;
                x += Math.Sign(last.X - x); y += Math.Sign(last.Y - y);
            }
        }
        int offset = strategy == "archive" ? 0 : random.NextOneBased(3) - 2;
        int middle = horizontal ? Math.Clamp(a.X + (b.X - a.X) / 2 + offset, a.X + 2, b.X - 2)
            : Math.Clamp(a.Y + (b.Y - a.Y) / 2 + offset, a.Y + 2, b.Y - 2);
        RoomPoint first = horizontal ? new(middle, a.Y) : new(a.X, middle), last = horizontal ? new(middle, b.Y) : new(b.X, middle);
        Line(a, first); Line(first, last); Line(last, b);
        if (strategy == "warrens" && cells.Count >= 12)
        {
            int from = cells.Count / 3 - 1, to = cells.Count * 2 / 3 - 1;
            var p = cells[from]; var q = cells[to];
            if ((p.X == q.X || p.Y == q.Y) && to - from >= 5)
            {
                var tail = cells.Skip(to + 1).ToArray(); cells.RemoveRange(from + 1, cells.Count - from - 1);
                int bend = random.NextOneBased(2) == 1 ? 2 : -2;
                var startBend = p.Y == q.Y ? new RoomPoint(p.X, p.Y + bend) : new(p.X + bend, p.Y);
                var endBend = p.Y == q.Y ? new RoomPoint(q.X, q.Y + bend) : new(q.X + bend, q.Y);
                Line(p, startBend); Line(startBend, endBend); Line(endBend, q); cells.AddRange(tail);
            }
        }
        return cells.ToArray();
    }

    private static DungeonCorridor DecorateCorridor(int from, int to, RoomPoint[] cells, string strategy,
        IReadOnlyList<DungeonRoomPlacement> rooms, char[][] grid)
    {
        var alcoves = new List<RoomPoint>(); var banks = new List<RoomPoint>(); var pockets = new List<RoomPoint>();
        bool Safe(RoomPoint p) => p.X > 0 && p.Y > 0 && p.X < grid[0].Length - 1 && p.Y < grid.Length - 1 && !InsideRoom(p, rooms);
        void Carve(RoomPoint p, List<RoomPoint> list) { if (Safe(p)) { grid[p.Y][p.X] = '.'; list.Add(p); } }
        for (int i = 6; i < cells.Length - 6; i += 8)
        {
            var p = cells[i]; bool horizontal = cells[i - 1].Y == p.Y && cells[i + 1].Y == p.Y;
            int side = (i + 1) % 2 == 0 ? 1 : -1;
            Carve(new(p.X + (horizontal ? 0 : side), p.Y + (horizontal ? side : 0)), alcoves);
        }
        if (strategy is "canals" or "archive")
        {
            int run = 0, bestStart = 0, bestEnd = 0, length = 0;
            for (int i = 2; i < cells.Length; i++)
            {
                var p = cells[i - 2]; var q = cells[i - 1]; var r = cells[i];
                bool same = q.X - p.X == r.X - q.X && q.Y - p.Y == r.Y - q.Y;
                if (same && i != cells.Length - 1) continue;
                int end = same ? i : i - 1;
                if (end - run + 1 > length) { bestStart = run; bestEnd = end; length = end - run + 1; }
                run = i - 1;
            }
            if (length >= 9)
            {
                var first = cells[bestStart + 2]; var last = cells[bestEnd - 2]; bool horizontal = first.Y == last.Y;
                foreach (int side in new[] { 2, -2 })
                {
                    var extra = new List<RoomPoint>();
                    for (int step = 1; step <= 2; step++) extra.Add(new(first.X + (horizontal ? 0 : side / 2 * step), first.Y + (horizontal ? side / 2 * step : 0)));
                    int steps = Math.Abs(last.X - first.X) + Math.Abs(last.Y - first.Y);
                    for (int step = 1; step <= steps; step++) extra.Add(new(first.X + (horizontal ? Math.Sign(last.X - first.X) * step : side), first.Y + (horizontal ? side : Math.Sign(last.Y - first.Y) * step)));
                    for (int step = 1; step <= 2; step++) extra.Add(new(last.X + (horizontal ? 0 : side - side / 2 * step), last.Y + (horizontal ? side - side / 2 * step : 0)));
                    if (!extra.All(Safe)) continue;
                    foreach (var p in extra) Carve(p, banks);
                    break;
                }
            }
        }
        else if (strategy is "courtyards" or "observatory" or "forge")
        {
            int stride = strategy == "courtyards" ? 10 : strategy == "observatory" ? 9 : 11;
            for (int i = 5; i < cells.Length - 5; i += stride)
            {
                var p = cells[i];
                if (strategy == "courtyards")
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) Carve(new(p.X + dx, p.Y + dy), pockets);
                else if (strategy == "observatory")
                    foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }) Carve(new(p.X + dx, p.Y + dy), pockets);
                else
                {
                    int dx = (i + 1) % 2 == 0 ? 1 : -1;
                    foreach (int dy in new[] { 0, 1, -1 }) Carve(new(p.X + dx, p.Y + dy), pockets);
                }
            }
        }
        return new(from, to, cells, alcoves.ToArray(), banks.ToArray(), pockets.ToArray());
    }

    /// <summary>The five original optional branch designs, rotated around a cardinal lane.</summary>
    public static DungeonBranch DesignBranch(DungeonBranchKind kind, RoomPoint origin, int dx, int dy, int side, int corridor = 0)
    {
        if (!Enum.IsDefined(kind) || Math.Abs(dx) + Math.Abs(dy) != 1 || side is not (-1 or 1)) throw new ArgumentException("Invalid branch orientation.");
        var cells = new HashSet<RoomPoint>(); var nodes = new List<DungeonBranchNode>();
        RoomPoint At(int u, int v) => new(origin.X + u * dx - v * dy * side, origin.Y + u * dy + v * dx * side);
        void Carve(int x, int y, int tx, int ty)
        {
            while (true)
            {
                if (y != 0) cells.Add(At(x, y));
                if (x == tx && y == ty) break;
                x += Math.Sign(tx - x); y += Math.Sign(ty - y);
            }
        }
        void Node(DungeonBranchNodeKind node, int u, int v) => nodes.Add(new(node, At(u, v)));
        switch (kind)
        {
            case DungeonBranchKind.Spur:
                Carve(3, 0, 3, 3); Carve(3, 3, 5, 3); Node(DungeonBranchNodeKind.Clue, 3, 2); Node(DungeonBranchNodeKind.Reward, 5, 3); break;
            case DungeonBranchKind.Braid:
                Carve(1, 0, 1, 2); Carve(1, 2, 6, 2); Carve(6, 2, 6, 0); Carve(4, 2, 4, 4);
                Node(DungeonBranchNodeKind.Key, 2, 2); Node(DungeonBranchNodeKind.Gate, 4, 3); Node(DungeonBranchNodeKind.Reward, 4, 4); break;
            case DungeonBranchKind.Switchback:
                Carve(2, 0, 2, 3); Carve(5, 0, 5, 2); Carve(5, 2, 6, 2); Carve(6, 2, 6, 3);
                Node(DungeonBranchNodeKind.Plate, 2, 3); Node(DungeonBranchNodeKind.Gate, 6, 2); Node(DungeonBranchNodeKind.Reward, 6, 3); break;
            case DungeonBranchKind.Hub:
                Carve(3, 0, 3, 3); Carve(5, 0, 5, -3); Carve(3, 1, 5, 1);
                Node(DungeonBranchNodeKind.Brazier, 3, 3); Node(DungeonBranchNodeKind.Brazier, 5, -3); Node(DungeonBranchNodeKind.Reward, 4, 1); break;
            case DungeonBranchKind.FalseEnd:
                Carve(2, 0, 2, 3); Carve(2, 3, 6, 3); Carve(6, 3, 6, 0); Carve(5, 2, 5, 3);
                Node(DungeonBranchNodeKind.Clue, 3, 3); Node(DungeonBranchNodeKind.Gate, 4, 3); Node(DungeonBranchNodeKind.Enemy, 5, 3); Node(DungeonBranchNodeKind.Reward, 5, 2); break;
        }
        return new(kind, corridor, cells.OrderBy(p => p.Y).ThenBy(p => p.X).ToArray(), nodes.ToArray());
    }

    private static DungeonBranch? AttachBranch(IReadOnlyList<DungeonCorridor> corridors, IReadOnlyList<DungeonRoomPlacement> rooms,
        char[][] grid, RoomPoint start, RoagRandom random)
    {
        if (random.NextOneBased(100) > 70) return null;
        var candidates = new List<(int Corridor, RoomPoint Origin, int Dx, int Dy, int Side)>();
        for (int c = 0; c < corridors.Count; c++)
        {
            var cells = corridors[c].Cells;
            for (int i = 2; i < cells.Length - 9; i++)
            {
                int dx = cells[i + 1].X - cells[i].X, dy = cells[i + 1].Y - cells[i].Y;
                if (Enumerable.Range(i, 8).Any(n => InsideRoom(cells[n], rooms))
                    || Enumerable.Range(i + 2, 6).Any(n => cells[n].X - cells[n - 1].X != dx || cells[n].Y - cells[n - 1].Y != dy)) continue;
                foreach (int side in new[] { -1, 1 }) candidates.Add((c, cells[i], dx, dy, side));
            }
        }
        if (candidates.Count == 0) return null;
        int kind = random.NextOneBased(5) - 1, first = random.NextOneBased(candidates.Count) - 1;
        var primary = corridors.SelectMany(c => c.Cells).ToHashSet();
        for (int offset = 0; offset < candidates.Count; offset++)
        {
            var c = candidates[(first + offset) % candidates.Count];
            var branch = DesignBranch((DungeonBranchKind)((kind + offset) % 5), c.Origin, c.Dx, c.Dy, c.Side, c.Corridor);
            if (branch.Cells.Any(p => p.X <= 0 || p.Y <= 0 || p.X >= grid[0].Length - 1 || p.Y >= grid.Length - 1 || InsideRoom(p, rooms) || primary.Contains(p))
                || branch.Nodes.Any(n => n.Kind == DungeonBranchNodeKind.Enemy && Math.Abs(n.Position.X - start.X) + Math.Abs(n.Position.Y - start.Y) < 5)) continue;
            foreach (var p in branch.Cells) grid[p.Y][p.X] = '.';
            return branch;
        }
        return null;
    }
}
