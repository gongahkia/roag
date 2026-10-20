using Ithica.Core.Expeditions;

namespace Ithica.Core.Generation;

internal static class LunarAreas
{
    private readonly record struct Rect(int X, int Y, int Width, int Height)
    {
        public Cell Center => new(X + Width / 2, Y + Height / 2);
    }
    public static Zone Surface(string id, int seed, bool rescue)
    {
        var random = new LunarRandom(seed, 311);
        var bounds = LunarRules.Default.Surface;
        int width = random.Next(bounds.MinWidth, bounds.MaxWidth + 1), height = random.Next(bounds.MinHeight, bounds.MaxHeight + 1);
        var z = new Zone(id, "Surface", "Dust, fractured basalt and the marks around a signal", width, height, Ground.Wall);
        var terrain = new LunarTerrain(seed, width, height);
        z.Generation = new() { Version = 3, Profile = terrain.Profile.Name, NoiseOctaves = LunarRules.Default.NoiseOctaves,
            Algorithm = "Warped gradient fBm / crater bowls and rims / connected approaches", Craters = terrain.Craters.ToList() };
        for (int y = 2; y < height - 2; y++)
            for (int x = 2; x < width - 2; x++)
            {
                var (relief, rim) = terrain.At(x, y);
                z.Set(new(x, y), relief > terrain.Profile.RockThreshold ? Ground.Wall : rim > .5 ? Ground.Path : Ground.Stone);
            }
        Cell entry = random.Next(4) switch
        {
            0 => new(3, random.Next(8, height - 8)), 1 => new(width - 4, random.Next(8, height - 8)),
            2 => new(random.Next(8, width - 8), 3), _ => new(random.Next(8, width - 8), height - 4)
        };
        Cell Choose(Func<Cell, bool> allowed)
        {
            var candidates = new List<Cell>();
            for (int y = 4; y < height - 4; y++) for (int x = 4; x < width - 4; x++)
                if (allowed(new(x, y))) candidates.Add(new(x, y));
            if (candidates.Count == 0) throw new InvalidOperationException("No bounded lunar objective position.");
            return candidates[random.Next(candidates.Count)];
        }
        var first = Choose(p => p.Distance(entry) is >= 8 and <= 14);
        z.Objects.Add(new("return", "Return to shelter", ObjectKind.Return, entry, "stairs-up", Detail: "Travel takes 8 ticks."));
        if (rescue) z.Objects.Add(new("survivor", "A person", ObjectKind.Rescue, first, "resident", Detail: "Share supplies and escort them home."));
        else
        {
            var component = Choose(p => p.Distance(entry) is >= 18 and <= 28 && p.Distance(first) is >= 7 and <= 14);
            z.Objects.Add(new("presence", "Movement", ObjectKind.Encounter, first, "bandit"));
            z.Objects.Add(new("component", "A usable component", ObjectKind.Salvage, component, "crystal", Detail: "presence"));
            LunarPaths.Clear(z, component, 2);
            LunarPaths.Carve(z, first, component, width - 3, radius: 1);
        }
        LunarPaths.Clear(z, entry, 1); LunarPaths.Clear(z, first, 2);
        LunarPaths.Carve(z, entry, first, width - 3, radius: 1);
        LunarPaths.ConnectSurfacePockets(z, entry);
        foreach (var obj in z.Objects) LunarPaths.ProtectRoute(z, entry, obj.Position);
        Paint(z, terrain);
        return z;
    }

    public static Zone Compartments(string id, int seed)
    {
        var random = new LunarRandom(seed, 321);
        var rules = LunarRules.Default.Compartments;
        int width = random.Next(rules.MinWidth, rules.MaxWidth + 1), height = random.Next(rules.MinHeight, rules.MaxHeight + 1);
        var z = new Zone(id, "Below the surface", "Passages cut through old pressure walls", width, height, Ground.Wall);
        z.Generation = new() { Version = 3, Profile = "Compartment network", NoiseOctaves = rules.DamageOctaves,
            Algorithm = "BSP chambers / connected graph and loops / noise damage / isolated sealed vault" };
        var erosion = new GradientNoise(unchecked((int)GenerationSeeds.Mix(unchecked((uint)seed), 322)));
        int vaultWidth = random.Next(9, 14), vaultHeight = random.Next(9, 14);
        var vault = new Rect(width - vaultWidth - 2, random.Next(3, height - vaultHeight - 2), vaultWidth, vaultHeight);
        var leaves = new List<Rect>();
        void Partition(Rect r, int depth)
        {
            bool horizontal = r.Width >= rules.MinPartition * 2, vertical = r.Height >= rules.MinPartition * 2;
            if (depth >= rules.MaxDepth || !horizontal && !vertical) { leaves.Add(r); return; }
            bool splitX = horizontal && (!vertical || (r.Width > r.Height ? random.Next(4) != 0 : random.Next(4) == 0));
            int size = splitX ? r.Width : r.Height, cut = random.Next(rules.MinPartition, size - rules.MinPartition + 1);
            if (splitX) { Partition(new(r.X, r.Y, cut, r.Height), depth + 1); Partition(new(r.X + cut, r.Y, r.Width - cut, r.Height), depth + 1); }
            else { Partition(new(r.X, r.Y, r.Width, cut), depth + 1); Partition(new(r.X, r.Y + cut, r.Width, r.Height - cut), depth + 1); }
        }
        Partition(new(2, 2, vault.X - 4, height - 4), 0);
        var rooms = new List<Rect>();
        foreach (var leaf in leaves)
        {
            int left = random.Next(1, 3), top = random.Next(1, 3);
            var room = new Rect(leaf.X + left, leaf.Y + top, leaf.Width - left - random.Next(1, 3), leaf.Height - top - random.Next(1, 3));
            string kind = new[] { "intact", "service", "damaged" }[random.Next(3)];
            rooms.Add(room); z.Generation.Chambers.Add(new(room.X, room.Y, room.Width, room.Height, kind));
            for (int y = room.Y; y < room.Y + room.Height; y++)
                for (int x = room.X; x < room.X + room.Width; x++)
                {
                    bool edge = x == room.X || y == room.Y || x == room.X + room.Width - 1 || y == room.Y + room.Height - 1;
                    bool cross = x == room.Center.X || y == room.Center.Y;
                    if (kind != "damaged" || !edge || cross || erosion.Fractal(x / 4.5, y / 4.5, rules.DamageOctaves) < .08) z.Set(new(x, y), Ground.Stone);
                }
        }
        // Eroded corners may otherwise leave one-cell pockets detached from a chamber's interior.
        foreach (var room in rooms)
        {
            var interior = LunarPaths.Reachable(z, room.Center);
            for (int y = room.Y; y < room.Y + room.Height; y++)
                for (int x = room.X; x < room.X + room.Width; x++)
                    if (z.Walkable(new(x, y)) && !interior.Contains(new(x, y))) z.Set(new(x, y), Ground.Wall);
        }
        // A randomized minimum spanning graph keeps every chamber reachable; extra edges create alternative routes.
        var connected = new HashSet<int> { 0 }; var edges = new HashSet<(int, int)>();
        while (connected.Count < rooms.Count)
        {
            var options = (from a in connected.Order() from b in Enumerable.Range(0, rooms.Count) where !connected.Contains(b)
                select (A: a, B: b, Cost: rooms[a].Center.Distance(rooms[b].Center) + random.Next(4))).ToList();
            var best = options.OrderBy(e => e.Cost).ThenBy(e => e.A).ThenBy(e => e.B).First();
            edges.Add((Math.Min(best.A, best.B), Math.Max(best.A, best.B))); connected.Add(best.B);
        }
        for (int a = 0; a < rooms.Count; a++)
            for (int b = a + 1; b < rooms.Count; b++)
                if (rooms[a].Center.Distance(rooms[b].Center) < 22 && random.NextDouble() < rules.LoopChance) edges.Add((a, b));
        foreach (var (a, b) in edges.OrderBy(e => e.Item1).ThenBy(e => e.Item2))
        {
            var from = rooms[a].Center; var to = rooms[b].Center;
            var bend = random.Next(2) == 0 ? new Cell(from.X, to.Y) : new Cell(to.X, from.Y);
            LunarPaths.Carve(z, from, bend, vault.X - 2); LunarPaths.Carve(z, bend, to, vault.X - 2);
        }
        var entryRoom = rooms.OrderBy(r => r.Center.X).ThenBy(r => Math.Abs(r.Center.Y - height / 2)).First();
        var entry = entryRoom.Center;
        var terminal = rooms.Where(r => r != entryRoom).OrderByDescending(r => r.Center.Distance(entry)).ThenBy(r => r.Y).First().Center;
        Cell gate = new(vault.X, random.Next(vault.Y + 2, vault.Y + vault.Height - 2));
        var nearest = rooms.OrderBy(r => r.Center.Distance(gate)).First().Center;
        LunarPaths.Carve(z, nearest, gate + new Cell(-1, 0), vault.X - 1);
        z.Rect(vault.X + 1, vault.Y + 1, vault.Width - 2, vault.Height - 2, Ground.Stone);
        z.Set(gate, Ground.Stone);
        var boss = vault.Center; var salvage = new Cell(vault.X + vault.Width - 2, boss.Y);
        z.Generation.Chambers.Add(new(vault.X, vault.Y, vault.Width, vault.Height, "sealed"));
        z.Objects.Add(new("return", "Return to shelter", ObjectKind.Return, entry, "stairs-up", Detail: "Travel takes 8 ticks."));
        z.Objects.Add(new("terminal", "Damaged pump", ObjectKind.Terminal, terminal, "well", Detail: "Repair with 2 carried water. Securing it supplies home with water."));
        z.Objects.Add(new("seal", "Sealed passage", ObjectKind.Gate, gate, "gate", true, "Restore the damaged pump to cycle the seal."));
        z.Objects.Add(new("presence-heavy", "Movement", ObjectKind.Encounter, boss, "bandit"));
        z.Objects.Add(new("component", "A usable component", ObjectKind.Salvage, salvage, "crystal", Detail: "presence-heavy"));
        foreach (var obj in z.Objects) { z.Safe[z.Index(obj.Position)] = true; LunarPaths.ProtectRoute(z, entry, obj.Position); }
        Paint(z, null);
        return z;
    }
    private static void Paint(Zone z, LunarTerrain? terrain)
    {
        for (int y = 0; y < z.Height; y++)
            for (int x = 0; x < z.Width; x++)
            {
                int i = y * z.Width + x;
                z.GroundSprites[i] = z.Tiles[i] == Ground.Wall ? terrain is null ? "cracked-wall" : "boulder"
                    : terrain is null ? "flagstone" : terrain.At(x, y).Height < -.12 ? "grit" : "rock-floor";
            }
    }
}
