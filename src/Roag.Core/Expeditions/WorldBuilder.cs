namespace Roag.Core.Expeditions;

public static class WorldBuilder
{
    private static readonly Cell[] Directions = [new(0, -1), new(1, 0), new(0, 1), new(-1, 0)];
    public static List<Zone> Create(int seed, GenerationOptions? options = null)
    {
        var town = LegacyWorldBuilder.Create(seed)[0];
        town.Generation.Algorithm = "Authored hub / seeded furnishings";
        DressTown(town, seed);
        var wilds = Forest(seed);
        var crypt = Abbey(seed, options ?? new());
        var zones = new List<Zone> { town, wilds, crypt };
        var validation = Validate(zones);
        if (!validation.Valid)
        {
            // Bounded, playable fallback; never quietly return an invalid quest.
            zones = LegacyWorldBuilder.Create(seed);
            foreach (var zone in zones)
            {
                zone.Generation = new() { Algorithm = "Authored compatibility fallback", UsedFallback = true, FallbackReason = string.Join("; ", validation.Errors) };
                Array.Fill(zone.Safe, true);
                Paint(zone, GenerationSeeds.Stream(seed, (uint)zone.Width));
            }
            var fallback = Validate(zones);
            if (!fallback.Valid) throw new InvalidOperationException("World validation failed: " + string.Join("; ", fallback.Errors));
        }
        foreach (var zone in zones) zone.Environment.Initialize(zone, seed);
        return zones;
    }

    private static void DressTown(Zone town, int seed)
    {
        var random = GenerationSeeds.Stream(seed, 10);
        Paint(town, random);
        // Furniture stays outside NPC approaches and doorways. Houses and roads remain familiar.
        Cell[] sites = [new(5, 5), new(10, 5), new(7, 6), new(25, 5), new(30, 5), new(5, 19), new(27, 19), new(30, 21)];
        var choices = GenerationRules.Default.Hearth.Furniture;
        foreach (var p in sites)
        {
            string sprite = choices[random.Next(choices.Length)];
            town.Objects.Add(new($"furniture-{p.X}-{p.Y}", sprite.Replace('-', ' '), ObjectKind.Scenery, p, sprite));
        }
        Array.Fill(town.Safe, true);
    }

    internal static Zone Forest(int seed)
    {
        var rules = GenerationRules.Default.Forest;
        var random = GenerationSeeds.Stream(seed, 20);
        var z = new Zone("wilds", "ASHWOOD / THE OLD ROAD", "A living forest around a winding road", 48, 32, Ground.Grass);
        z.Generation = new() { Algorithm = "Cellular forest and ponds / reserved quest routes", CellularPasses = rules.Passes + rules.PondPasses };
        bool[] initial = Enumerable.Range(0, z.Tiles.Length).Select(_ => random.Next(100) < rules.InitialTreesPercent).ToArray();
        var forest = CellularAutomata.Smooth(initial, z.Width, z.Height, rules.Passes, rules.Birth, rules.Survive);
        for (int y = 0; y < z.Height; y++)
            for (int x = 0; x < z.Width; x++)
                if (forest[y * z.Width + x] || x < 2 || y < 2 || x >= z.Width - 2 || y >= z.Height - 2) z.Set(new(x, y), Ground.Tree);
        var ponds = CellularAutomata.Smooth(Enumerable.Range(0, z.Tiles.Length).Select(_ => random.Next(100) < rules.PondPercent).ToArray(), z.Width, z.Height, rules.PondPasses);
        for (int y = 3; y < z.Height - 3; y++)
            for (int x = 6; x < z.Width - 6; x++)
                if ((y < 12 || y > 22) && ponds[y * z.Width + x]) z.Set(new(x, y), Ground.Water);
        // A continuous river meanders independently of the road; intersections become bridges.
        int riverX = 20 + random.Next(5);
        for (int y = 2; y < z.Height - 2; y++)
        {
            if (y % 3 == 0) riverX = Math.Clamp(riverX + random.Next(3) - 1, 18, 26);
            z.Rect(riverX, y, 2, 1, Ground.Water);
        }
        Cell entry = new(2, 16), exit = new(45, 16);
        Cell hounds = new(10 + random.Next(3), 15 + random.Next(4));
        Cell raiders = new(31 + random.Next(3), 15 + random.Next(4));
        Cell key = new(10 + random.Next(5), 7 + random.Next(5));
        Cell shrine = new(26 + random.Next(4), 7 + random.Next(4));
        Cell camp = new(37 + random.Next(4), 23 + random.Next(4));
        Cell[] road = [entry, hounds, new(23, 14 + random.Next(5)), raiders, exit];
        for (int n = 1; n < road.Length; n++) Trail(z, road[n - 1], road[n], Ground.Path);
        Trail(z, hounds + new Cell(0, -2), key, Ground.Path);
        Trail(z, road[2], shrine, Ground.Path);
        Trail(z, raiders + new Cell(0, 2), camp, Ground.Path);
        Add(z, "wilds-hounds", "Briar hounds", ObjectKind.Encounter, hounds, "wolf");
        Add(z, "wilds-raiders", "Roadside ambush", ObjectKind.Encounter, raiders, "bandit");
        Add(z, "iron-cache", "An abandoned chest", ObjectKind.Chest, key, "chest", "The abbey's iron key, 18 gold, and one potion.");
        Add(z, "forest-shrine", "Shrine of the Wayfarer", ObjectKind.Shrine, shrine, "shrine", "Once: restore 8 mana and reduce everyone's stress by 15.");
        Add(z, "wood-camp", "A sheltered campsite", ObjectKind.Camp, camp, "campfire", "Camp: spend 2 rations. Restore 18 health, 6 mana, and 12 composure.");
        Add(z, "wood-sign", "A weathered inscription", ObjectKind.Talk, key + new Cell(-2, 1), "book", "The iron key rests along a branch of the old road. Follow the east road beneath the abbey. Colored growth beyond the road spreads every four steps; a fresh torch clears nearby hazards.");
        z.Objects.Add(new("to-town", "Return to Hearth", ObjectKind.Portal, entry, "sign", false, "Return to town.", "hearth", new(32, 13)));
        z.Objects.Add(new("to-crypt", "Descend into the abbey", ObjectKind.Portal, exit, "stairs-down", false, "Enter the Sunken Abbey.", "crypt", new(4, 18)));
        foreach (var o in z.Objects) Clearing(z, o.Position, 2, Ground.Grass);
        Paint(z, GenerationSeeds.Stream(seed, 21));
        var dressing = GenerationSeeds.Stream(seed, 22);
        for (int y = 3; y < z.Height - 3; y++)
            for (int x = 3; x < z.Width - 3; x++)
            {
                Cell p = new(x, y);
                if (z.At(p) != Ground.Grass || z.Safe[z.Index(p)] || dressing.Next(100) >= 12) continue;
                string sprite = rules.Props[dressing.Next(rules.Props.Length)];
                z.Objects.Add(new($"forest-prop-{x}-{y}", sprite.Replace('-', ' '), ObjectKind.Scenery, p, sprite));
            }
        return z;
    }

    internal static Zone Abbey(int seed, GenerationOptions options)
    {
        var rules = GenerationRules.Default.Abbey;
        int columns = rules.Columns, rows = rules.Rows, size = rules.ChunkSize;
        var random = GenerationSeeds.Stream(seed, 30);
        var z = new Zone("crypt", "THE SUNKEN ABBEY / BELOW THE BELL", "Cloisters, cisterns, and crumbling vaults", columns * size + 2, rows * size + 2, Ground.Wall);
        var required = new int[columns * rows];
        var visited = new HashSet<Cell>();
        Cell bossRoom = new(columns - 1, 2);
        void Connect(Cell a, Cell b)
        {
            int d = Array.IndexOf(Directions, new Cell(b.X - a.X, b.Y - a.Y));
            required[a.Y * columns + a.X] |= 1 << d;
            required[b.Y * columns + b.X] |= 1 << ((d + 2) % 4);
        }
        // The short main route is fixed in the quest graph; branch topology is seeded.
        for (int x = 0; x < columns; x++)
        {
            visited.Add(new(x, 2));
            if (x > 0) Connect(new(x - 1, 2), new(x, 2));
        }
        while (visited.Count < columns * rows)
        {
            var frontier = visited.OrderBy(p => p.Y).ThenBy(p => p.X).Where(p => p != bossRoom)
                .SelectMany(a => Directions.Select(d => (A: a, B: a + d)))
                .Where(e => e.B.X >= 0 && e.B.Y >= 0 && e.B.X < columns && e.B.Y < rows && !visited.Contains(e.B) && e.B != bossRoom).ToArray();
            var edge = frontier[random.Next(frontier.Length)];
            Connect(edge.A, edge.B);
            visited.Add(edge.B);
        }
        var tiles = new List<WfcTile>();
        for (int mask = 1; mask < 16; mask++)
            foreach (var style in rules.Variants)
                tiles.Add(new($"{mask}:{style.Id}", mask & 1, (mask >> 1) & 1, (mask >> 2) & 1, (mask >> 3) & 1,
                    style.Weight * (5 - System.Numerics.BitOperations.PopCount((uint)mask))));
        bool Allowed(int i, WfcTile tile)
        {
            int x = i % columns, y = i / columns;
            for (int d = 0; d < 4; d++)
            {
                Cell neighbor = new Cell(x, y) + Directions[d];
                bool outside = neighbor.X < 0 || neighbor.Y < 0 || neighbor.X >= columns || neighbor.Y >= rows;
                bool sealedBossEdge = (new Cell(x, y) == bossRoom && d != 3) || (neighbor == bossRoom && d != 1);
                if ((outside || sealedBossEdge) && tile.Socket(d) != 0) return false;
                if ((required[i] & (1 << d)) != 0 && tile.Socket(d) != 1) return false;
            }
            return true;
        }
        var result = new WaveFunctionCollapse(tiles).Solve(columns, rows, Allowed, GenerationSeeds.Mix(unchecked((uint)seed), 31), options.WfcAttempts ?? rules.MaxAttempts);
        z.Generation = new()
        {
            Algorithm = "Quest graph / socket WFC / cellular erosion", CellularPasses = 2,
            WfcAttempts = result?.Attempts ?? (options.WfcAttempts ?? rules.MaxAttempts), Observations = result?.Observations ?? 0,
            Propagations = result?.Propagations ?? 0, UsedFallback = result is null,
            FallbackReason = result is null ? "WFC attempt budget exhausted; exact quest-graph sockets with cloister rooms." : ""
        };
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int i = y * columns + x;
                var tile = result is null ? tiles.First(t => t.Id == $"{required[i]}:cloister") : tiles[result.Tiles[i]];
                int sockets = tile.North | tile.East << 1 | tile.South << 2 | tile.West << 3;
                var style = rules.Variants.Single(v => tile.Id.EndsWith(":" + v.Id, StringComparison.Ordinal));
                z.Generation.Rooms.Add(new(x, y, sockets, style.Id));
                Room(z, x * size + 1, y * size + 1, sockets, style, random);
            }
        Cell Center(int x, int y) => new(x * size + 4, y * size + 4);
        Cell entrance = Center(0, 2), hollows = Center(1 + random.Next(3), 0), slimes = Center(2 + random.Next(3), 4);
        Cell gate = new((columns - 1) * size + 1, 18), boss = Center(columns - 1, 2), relic = boss + new Cell(1, 0);
        Add(z, "abbey-hollows", "The faithful dead", ObjectKind.Encounter, hollows, "skeleton");
        Add(z, "abbey-slimes", "Something in the cistern", ObjectKind.Encounter, slimes, "slime");
        Add(z, "abbey-abbot", "THE HOLLOW ABBOT", ObjectKind.Encounter, boss, "abbot");
        z.Objects.Add(new("iron-gate", "The iron seal", ObjectKind.Gate, gate, "gate", true, "An iron key opens the abbot's chamber."));
        Add(z, "abbey-chest", "Pilgrim's strongbox", ObjectKind.Chest, Center(0, 2) + new Cell(-1, -1), "chest", "24 gold, 2 potions, and a torch.");
        Add(z, "relic", "The ember relic", ObjectKind.Relic, relic, "relic", "The last ember of the abbey. Defeat its guardian first.");
        Add(z, "crypt-camp", "An abandoned brazier", ObjectKind.Camp, Center(3, 3), "brazier", "Camp: spend 2 rations. Restore the party before the final chamber.");
        z.Objects.Add(new("to-forest", "Steps to the surface", ObjectKind.Portal, entrance, "stairs-up", false, "Return to Ashwood.", "wilds", new(43, 16)));
        // Clear scenery from objective footprints without punching new doorways into room walls.
        foreach (var o in z.Objects.Where(o => o.Kind != ObjectKind.Scenery).ToArray())
        {
            z.Objects.RemoveAll(p => p.Kind == ObjectKind.Scenery && p.Position.Distance(o.Position) <= 1);
            z.Set(o.Position, Ground.Stone);
            Protect(z, o.Position, 2);
        }
        return z;
    }

    private static void Room(Zone z, int ox, int oy, int mask, RoomVariant style, RandomStream random)
    {
        var rough = CellularAutomata.Smooth(Enumerable.Range(0, 49).Select(_ => random.Next(100) < 45).ToArray(), 7, 7, 2);
        for (int y = 0; y < 7; y++)
            for (int x = 0; x < 7; x++)
            {
                Cell p = new(ox + x, oy + y);
                bool interior = x > 0 && y > 0 && x < 6 && y < 6;
                bool corner = Math.Abs(x - 3) >= 2 && Math.Abs(y - 3) >= 2;
                Ground ground = interior ? Ground.Stone : Ground.Wall;
                if (interior && (x == 1 || x == 5 || y == 1 || y == 5) && x != 3 && y != 3 && style.Shape == "eroded" && rough[y * 7 + x]) ground = Ground.Wall;
                if (interior && corner && style.Shape == "cistern") ground = Ground.Water;
                if ((y == 0 && x == 3 && (mask & 1) != 0) || (x == 6 && y == 3 && (mask & 2) != 0)
                    || (y == 6 && x == 3 && (mask & 4) != 0) || (x == 0 && y == 3 && (mask & 8) != 0)) ground = Ground.Stone;
                z.Set(p, ground);
                z.GroundSprites[z.Index(p)] = ground == Ground.Wall ? style.WallSprite : ground == Ground.Water ? "water" : style.FloorSprites[random.Next(style.FloorSprites.Length)];
                // Every room has a safe cross. All socket connections and their approaches stay clear.
                if ((x == 3 || y == 3) && ground == Ground.Stone) z.Safe[z.Index(p)] = true;
                if (ground == Ground.Stone && interior && !corner && x != 3 && y != 3 && random.Next(100) < 18)
                {
                    string prop = style.Props[random.Next(style.Props.Length)];
                    z.Objects.Add(new($"crypt-prop-{p.X}-{p.Y}", prop.Replace('-', ' '), ObjectKind.Scenery, p, prop));
                }
            }
    }

    private static void Add(Zone z, string id, string name, ObjectKind kind, Cell p, string sprite, string detail = "") => z.Objects.Add(new(id, name, kind, p, sprite, false, detail));
    private static void Protect(Zone z, Cell p, int radius)
    {
        for (int y = p.Y - radius; y <= p.Y + radius; y++)
            for (int x = p.X - radius; x <= p.X + radius; x++)
                if (z.Inside(new(x, y))) z.Safe[z.Index(new(x, y))] = true;
    }
    private static void Clearing(Zone z, Cell p, int radius, Ground floor)
    {
        for (int y = p.Y - radius; y <= p.Y + radius; y++)
            for (int x = p.X - radius; x <= p.X + radius; x++)
            {
                Cell q = new(x, y);
                if (!z.Inside(q) || x < 2 || x >= z.Width - 2 || y < 2 || y >= z.Height - 2) continue;
                var g = z.At(q);
                z.Set(q, g is Ground.Water or Ground.Bridge ? Ground.Bridge : g == Ground.Path ? Ground.Path : floor);
            }
        Protect(z, p, radius);
    }
    private static void Trail(Zone z, Cell from, Cell to, Ground floor)
    {
        var p = from;
        while (true)
        {
            Clearing(z, p, 1, Ground.Grass);
            z.Set(p, z.At(p) == Ground.Bridge ? Ground.Bridge : floor);
            if (p == to) break;
            p += p.X != to.X ? new(Math.Sign(to.X - p.X), 0) : new(0, Math.Sign(to.Y - p.Y));
        }
    }
    private static void Paint(Zone z, RandomStream random)
    {
        for (int i = 0; i < z.Tiles.Length; i++)
        {
            var sprites = GenerationRules.Default.Materials[z.Tiles[i].ToString()].Sprites;
            if (sprites.Length > 0) z.GroundSprites[i] = sprites[random.Next(sprites.Length)];
        }
    }

    public static QuestValidation Validate(IReadOnlyList<Zone> zones)
    {
        var errors = new List<string>();
        if (zones.Count != 3 || new[] { "hearth", "wilds", "crypt" }.Any(id => zones.Count(z => z.Id == id) != 1)) return new(false, ["Expected Hearth, Ashwood, and Abbey."]);
        var town = zones.Single(z => z.Id == "hearth");
        var wilds = zones.Single(z => z.Id == "wilds");
        var crypt = zones.Single(z => z.Id == "crypt");
        var requiredIds = new Dictionary<string, string[]>
        {
            ["hearth"] = ["inn", "merchant", "elder", "smith", "to-wilds"],
            ["wilds"] = ["wilds-hounds", "wilds-raiders", "iron-cache", "forest-shrine", "wood-camp", "to-town", "to-crypt"],
            ["crypt"] = ["abbey-hollows", "abbey-slimes", "abbey-abbot", "iron-gate", "abbey-chest", "relic", "crypt-camp", "to-forest"]
        };
        foreach (var z in zones)
            foreach (string id in requiredIds[z.Id])
            {
                ObjectKind expected = id switch
                {
                    "inn" => ObjectKind.Inn, "merchant" => ObjectKind.Merchant, "elder" or "smith" => ObjectKind.Talk,
                    "iron-cache" or "abbey-chest" => ObjectKind.Chest, "forest-shrine" => ObjectKind.Shrine,
                    "wood-camp" or "crypt-camp" => ObjectKind.Camp, "iron-gate" => ObjectKind.Gate, "relic" => ObjectKind.Relic,
                    "to-wilds" or "to-town" or "to-crypt" or "to-forest" => ObjectKind.Portal,
                    _ => ObjectKind.Encounter
                };
                if (z.Objects.Count(o => o.Id == id && o.Kind == expected) != 1) errors.Add("Missing/invalid objective: " + id);
            }
        if (errors.Count > 0) return new(false, errors.ToArray());
        foreach (var z in zones)
        {
            if (z.Objects.Select(o => o.Id).Distinct().Count() != z.Objects.Count || z.Objects.Select(o => o.Position).Distinct().Count() != z.Objects.Count) errors.Add(z.Id + ": overlapping/duplicate objects.");
            foreach (var o in z.Objects)
            {
                if (!z.Inside(o.Position) || !z.Walkable(o.Position)) errors.Add(o.Id + ": invalid ground.");
                if (o.Kind == ObjectKind.Portal)
                {
                    var target = zones.SingleOrDefault(t => t.Id == o.Destination);
                    if (target is null || !target.Walkable(o.Arrival) || target.Objects.Any(t => t.Position == o.Arrival && t.Blocks)) errors.Add(o.Id + ": invalid arrival.");
                }
            }
        }
        foreach (var (id, destination) in new[] { ("to-wilds", "wilds"), ("to-town", "hearth"), ("to-crypt", "crypt"), ("to-forest", "wilds") })
            if (zones.SelectMany(z => z.Objects).Single(o => o.Id == id).Destination != destination) errors.Add(id + ": incorrect destination.");
        if (errors.Count > 0) return new(false, errors.ToArray());
        HashSet<Cell> Flood(Zone z, Cell start, bool open, bool avoidEncounters = false)
        {
            var blocked = z.Objects.Where(o => o.Blocks && !(open && o.Kind == ObjectKind.Gate) || avoidEncounters && o.Kind == ObjectKind.Encounter).Select(o => o.Position).ToHashSet();
            var reached = new HashSet<Cell>();
            var queue = new Queue<Cell>();
            if (z.Walkable(start) && !blocked.Contains(start)) { reached.Add(start); queue.Enqueue(start); }
            while (queue.TryDequeue(out var p))
                foreach (var n in z.Neighbors(p))
                    if (z.Walkable(n) && !blocked.Contains(n) && reached.Add(n)) queue.Enqueue(n);
            return reached;
        }
        bool Approachable(Zone z, WorldObject o, HashSet<Cell> reached) => reached.Contains(o.Position) || z.Neighbors(o.Position).Any(reached.Contains);
        foreach (var z in zones)
        {
            Cell start = z.Id == "hearth" ? new(18, 16) : zones.SelectMany(t => t.Objects).First(o => o.Kind == ObjectKind.Portal && o.Destination == z.Id).Arrival;
            var reached = Flood(z, start, true, true);
            foreach (var o in z.Objects.Where(o => o.Kind != ObjectKind.Scenery))
                if (!Approachable(z, o, reached)) errors.Add(o.Id + ": no encounter-free approach.");
        }
        var arrival = wilds.Objects.Single(o => o.Id == "to-crypt").Arrival;
        var locked = Flood(crypt, arrival, false);
        var unlocked = Flood(crypt, arrival, true);
        foreach (string id in new[] { "abbey-abbot", "relic" })
        {
            var o = crypt.Objects.Single(o => o.Id == id);
            if (Approachable(crypt, o, locked)) errors.Add(id + ": reachable before opening the seal.");
            if (!Approachable(crypt, o, unlocked)) errors.Add(id + ": unreachable after opening the seal.");
        }
        foreach (var o in crypt.Objects.Where(o => o.Kind != ObjectKind.Scenery && o.Id is not ("abbey-abbot" or "relic")))
            if (!Approachable(crypt, o, locked)) errors.Add(o.Id + ": unavailable before the gate.");
        return new(errors.Count == 0, errors.ToArray());
    }
}
