using Ithica.Core.Expeditions;

namespace Ithica.Core.Generation;

internal static class LunarPaths
{
    public static void Clear(Zone z, Cell p, int radius, bool protect = true)
    {
        for (int y = p.Y - radius; y <= p.Y + radius; y++)
            for (int x = p.X - radius; x <= p.X + radius; x++)
            {
                var q = new Cell(x, y);
                if (x < 2 || y < 2 || x >= z.Width - 2 || y >= z.Height - 2) continue;
                z.Set(q, Ground.Stone);
                if (protect) z.Safe[z.Index(q)] = true;
            }
    }
    public static HashSet<Cell> Reachable(Zone z, Cell start, Cell? blocked = null, bool safeOnly = false)
    {
        var seen = new HashSet<Cell>(); var queue = new Queue<Cell>();
        if (z.Walkable(start) && start != blocked && (!safeOnly || z.Safe[z.Index(start)])) { queue.Enqueue(start); seen.Add(start); }
        while (queue.TryDequeue(out var p))
            foreach (var n in z.Neighbors(p))
                if (n != blocked && z.Walkable(n) && (!safeOnly || z.Safe[z.Index(n)]) && seen.Add(n)) queue.Enqueue(n);
        return seen;
    }
    public static List<Cell> Carve(Zone z, Cell start, Cell goal, int maxX, bool protect = true, int radius = 0)
    {
        // Uniformly bounded Dijkstra routing prefers existing open terrain and has stable tie-breaking.
        var cost = Enumerable.Repeat(int.MaxValue, z.Tiles.Length).ToArray();
        var parent = Enumerable.Repeat(-1, z.Tiles.Length).ToArray();
        var queue = new PriorityQueue<Cell, (int Cost, int Cell)>();
        cost[z.Index(start)] = 0; queue.Enqueue(start, (0, z.Index(start)));
        while (queue.TryDequeue(out var p, out var priority))
        {
            if (priority.Cost != cost[z.Index(p)]) continue;
            if (p == goal) break;
            foreach (var n in z.Neighbors(p))
            {
                if (n.X < 2 || n.Y < 2 || n.X > maxX || n.Y >= z.Height - 2) continue;
                int index = z.Index(n), next = priority.Cost + (z.Walkable(n) ? 2 : 9);
                if (next >= cost[index]) continue;
                cost[index] = next; parent[index] = z.Index(p); queue.Enqueue(n, (next, index));
            }
        }
        if (cost[z.Index(goal)] == int.MaxValue) throw new InvalidOperationException("No bounded lunar corridor route.");
        var path = new List<Cell>(); int at = z.Index(goal), first = z.Index(start);
        while (at != first) { path.Add(new(at % z.Width, at / z.Width)); at = parent[at]; }
        path.Add(start); path.Reverse();
        foreach (var p in path) Clear(z, p, radius, protect);
        return path;
    }
    public static void ConnectSurfacePockets(Zone z, Cell entry)
    {
        var connected = Reachable(z, entry);
        for (int y = 2; y < z.Height - 2; y++)
            for (int x = 2; x < z.Width - 2; x++)
            {
                var p = new Cell(x, y);
                if (!z.Walkable(p) || connected.Contains(p)) continue;
                var nearest = connected.OrderBy(q => q.Distance(p)).ThenBy(q => z.Index(q)).First();
                Carve(z, p, nearest, z.Width - 3, false);
                connected = Reachable(z, entry);
            }
    }
    public static void ProtectRoute(Zone z, Cell from, Cell to)
    {
        if (from == to) { z.Safe[z.Index(from)] = true; return; }
        var path = z.FindPath(from, [to]);
        if (path.Count == 0) throw new InvalidOperationException("A lunar objective lost its approach.");
        z.Safe[z.Index(from)] = true;
        foreach (var p in path) z.Safe[z.Index(p)] = true;
    }
}
