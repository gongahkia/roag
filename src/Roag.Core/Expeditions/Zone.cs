namespace Roag.Core.Expeditions;

public sealed class Zone(string id, string name, string subtitle, int width, int height, Ground fill)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public string Subtitle { get; } = subtitle;
    public int Width { get; } = width;
    public int Height { get; } = height;
    public Ground[] Tiles { get; } = Enumerable.Repeat(fill, width * height).ToArray();
    public bool[] Seen { get; set; } = new bool[width * height];
    public bool[] Visible { get; } = new bool[width * height];
    public List<WorldObject> Objects { get; } = [];
    public string?[] GroundSprites { get; } = new string?[width * height];
    public bool[] Safe { get; } = new bool[width * height];
    public HazardField Environment { get; } = new(width, height);
    public GenerationReport Generation { get; set; } = new();
    public int Index(Cell p) => p.Y * Width + p.X;
    public bool Inside(Cell p) => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
    public Ground At(Cell p) => Inside(p) ? Tiles[Index(p)] : Ground.Void;
    public void Set(Cell p, Ground tile)
    {
        if (Inside(p))
        {
            Tiles[Index(p)] = tile;
        }
    }
    public static bool Solid(Ground g) => g is Ground.Wall or Ground.Tree or Ground.Water or Ground.Void;
    public bool Walkable(Cell p) => Inside(p) && !Solid(At(p));
    public void Rect(int x, int y, int w, int h, Ground tile)
    {
        for (int yy = y; yy < y + h; yy++)
        {
            for (int xx = x; xx < x + w; xx++)
            {
                Set(new(xx, yy), tile);
            }
        }
    }
    public IEnumerable<Cell> Neighbors(Cell p)
    {
        yield return new(p.X, p.Y - 1);
        yield return new(p.X - 1, p.Y);
        yield return new(p.X + 1, p.Y);
        yield return new(p.X, p.Y + 1);
    }
    public List<Cell> FindPath(Cell start, IEnumerable<Cell> goals, Func<Cell, bool>? allowed = null)
    {
        var targets = goals.Where(Inside).ToHashSet();
        var queue = new Queue<Cell>();
        var parents = new Dictionary<Cell, Cell> { { start, start } };
        queue.Enqueue(start);
        while (queue.TryDequeue(out var p))
        {
            if (targets.Contains(p))
            {
                var path = new List<Cell>();
                while (p != start)
                {
                    path.Add(p);
                    p = parents[p];
                }
                path.Reverse();
                return path;
            }
            foreach (var n in Neighbors(p))
            {
                if (Walkable(n) && (allowed?.Invoke(n) ?? true) && parents.TryAdd(n, p))
                {
                    queue.Enqueue(n);
                }
            }
        }
        return [];
    }
    public void Reveal(Cell origin, int radius)
    {
        Array.Clear(Visible);
        for (int y = Math.Max(0, origin.Y - radius); y <= Math.Min(Height - 1, origin.Y + radius); y++)
        {
            for (int x = Math.Max(0, origin.X - radius); x <= Math.Min(Width - 1, origin.X + radius); x++)
            {
                var p = new Cell(x, y);
                if ((x - origin.X) * (x - origin.X) + (y - origin.Y) * (y - origin.Y) > radius * radius)
                {
                    continue;
                }

                if (LineOfSight(origin, p))
                {
                    Visible[Index(p)] = Seen[Index(p)] = true;
                }
            }
        }
    }
    private bool LineOfSight(Cell a, Cell b)
    {
        int x = a.X, y = a.Y, dx = Math.Abs(b.X - x), dy = -Math.Abs(b.Y - y), sx = x < b.X ? 1 : -1, sy = y < b.Y ? 1 : -1, error = dx + dy;
        while (x != b.X || y != b.Y)
        {
            if ((x != a.X || y != a.Y) && Solid(At(new(x, y))))
            {
                return false;
            }

            int e = error * 2;
            if (e >= dy)
            {
                error += dy;
                x += sx;
            }
            if (e <= dx)
            {
                error += dx;
                y += sy;
            }
        }
        return true;
    }
}
// Retained verbatim for version-one save compatibility and deterministic fallback.
internal static class LegacyWorldBuilder
{
    public static List<Zone> Create(int seed)
    {
        var town = new Zone("hearth", "HEARTH / THE LAST LIGHT", "A place to mend before the road", 36, 26, Ground.Grass);
        town.Rect(0, 0, 36, 2, Ground.Tree);
        town.Rect(0, 24, 36, 2, Ground.Tree);
        town.Rect(0, 0, 2, 26, Ground.Tree);
        town.Rect(34, 0, 2, 26, Ground.Tree);
        town.Rect(2, 12, 33, 3, Ground.Path);
        town.Rect(17, 4, 3, 20, Ground.Path);
        House(town, 4, 4, 8, 6);
        House(town, 24, 4, 8, 6);
        House(town, 4, 18, 8, 5);
        House(town, 25, 18, 7, 5);
        Add(town, "inn", "The Copper Kettle", ObjectKind.Inn, 8, 8, "innkeeper", true, "Rest: 12 gold. Fully restores health, mana, and composure.");
        Add(town, "merchant", "The Provisioner", ObjectKind.Merchant, 27, 8, "merchant", true, "Expedition kit: 10 gold for 2 potions, 2 torches, and 2 rations.");
        Add(town, "elder", "Warden Elowen", ObjectKind.Talk, 16, 12, "elder", true, "The abbey bell has rung beneath the earth. Find the iron key in Ashwood, recover the ember relic, and bring it home.");
        Add(town, "smith", "Old Garran", ObjectKind.Talk, 9, 20, "smith", true, "The hollow abbot gathers his strength every third round. Guard when the bell begins to toll. A marked enemy takes more damage.");
        Add(town, "well", "Village well", ObjectKind.Scenery, 18, 8, "well", true);
        Add(town, "hearth-fire", "The last light", ObjectKind.Scenery, 18, 13, "campfire");
        town.Objects.Add(new("to-wilds", "Road to Ashwood", ObjectKind.Portal, new(34, 13), "sign", false, "Enter Ashwood.", "wilds", new(3, 16)));
        var wilds = new Zone("wilds", "ASHWOOD / THE OLD ROAD", "The trees remember the sun", 48, 32, Ground.Grass);
        for (int y = 0; y < wilds.Height; y++)
        {
            for (int x = 0; x < wilds.Width; x++)
            {
                uint hash = unchecked((uint)(x * 374761393 + y * 668265263 + seed * 31));
                hash = (hash ^ (hash >> 13)) * 1274126177;
                if (x < 2 || y < 2 || x >= 46 || y >= 30 || hash % 7 < 2)
                {
                    wilds.Set(new(x, y), Ground.Tree);
                }
            }
        }

        wilds.Rect(18, 2, 3, 28, Ground.Water);
        wilds.Rect(2, 14, 44, 5, Ground.Grass);
        wilds.Rect(2, 16, 44, 1, Ground.Path);
        wilds.Rect(18, 14, 3, 5, Ground.Bridge);
        wilds.Rect(8, 9, 8, 7, Ground.Grass);
        wilds.Rect(22, 9, 6, 7, Ground.Grass);
        wilds.Rect(34, 15, 8, 8, Ground.Grass);
        Add(wilds, "wilds-hounds", "Briar hounds", ObjectKind.Encounter, 11, 16, "wolf");
        Add(wilds, "wilds-raiders", "Roadside ambush", ObjectKind.Encounter, 31, 16, "bandit");
        Add(wilds, "iron-cache", "An abandoned chest", ObjectKind.Chest, 13, 11, "chest", false, "The abbey's iron key, 18 gold, and one potion.");
        Add(wilds, "forest-shrine", "Shrine of the Wayfarer", ObjectKind.Shrine, 24, 11, "shrine", false, "Once: restore 8 mana and reduce everyone's stress by 15.");
        Add(wilds, "wood-camp", "A sheltered campsite", ObjectKind.Camp, 38, 21, "campfire", false, "Camp: spend 2 rations. Restore 18 health, 6 mana, and 12 composure.");
        Add(wilds, "wood-sign", "A weathered inscription", ObjectKind.Talk, 9, 11, "book", false, "An iron key rests in the old roadside cache. The east road leads beneath the abbey. E interacts with nearby objects.");
        wilds.Objects.Add(new("to-town", "Return to Hearth", ObjectKind.Portal, new(2, 16), "sign", false, "Return to town.", "hearth", new(32, 13)));
        wilds.Objects.Add(new("to-crypt", "Descend into the abbey", ObjectKind.Portal, new(45, 16), "stairs-down", false, "Enter the Sunken Abbey.", "crypt", new(4, 16)));
        var crypt = new Zone("crypt", "THE SUNKEN ABBEY / BELOW THE BELL", "Keep a flame between you and the dark", 48, 34, Ground.Wall);
        crypt.Rect(3, 10, 11, 14, Ground.Stone);
        crypt.Rect(18, 3, 12, 10, Ground.Stone);
        crypt.Rect(18, 21, 12, 10, Ground.Stone);
        crypt.Rect(35, 10, 10, 15, Ground.Stone);
        crypt.Rect(8, 15, 32, 3, Ground.Stone);
        crypt.Rect(23, 8, 3, 18, Ground.Stone);
        Add(crypt, "abbey-hollows", "The faithful dead", ObjectKind.Encounter, 24, 7, "skeleton");
        Add(crypt, "abbey-slimes", "Something in the cistern", ObjectKind.Encounter, 24, 26, "slime");
        Add(crypt, "abbey-abbot", "THE HOLLOW ABBOT", ObjectKind.Encounter, 40, 16, "abbot");
        Add(crypt, "iron-gate", "The iron seal", ObjectKind.Gate, 34, 16, "gate", true, "An iron key opens the abbot's chamber.");
        // One doorway, rather than a three-cell bypass around the seal.
        crypt.Set(new(34, 15), Ground.Wall);
        crypt.Set(new(34, 17), Ground.Wall);
        Add(crypt, "abbey-chest", "Pilgrim's strongbox", ObjectKind.Chest, 8, 12, "chest", false, "24 gold, 2 potions, and a torch.");
        Add(crypt, "relic", "The ember relic", ObjectKind.Relic, 43, 16, "relic", false, "The last ember of the abbey. Defeat its guardian first.");
        Add(crypt, "crypt-camp", "An abandoned brazier", ObjectKind.Camp, 24, 29, "brazier", false, "Camp: spend 2 rations. Restore the party before the final chamber.");
        crypt.Objects.Add(new("to-forest", "Steps to the surface", ObjectKind.Portal, new(3, 16), "stairs-up", false, "Return to Ashwood.", "wilds", new(43, 16)));
        foreach (var (x, y, sprite) in new[] { (6, 11, "grave"), (10, 11, "grave"), (6, 21, "bones"), (11, 21, "urn"), (20, 4, "coffin"), (27, 4, "coffin"), (20, 11, "candle"), (28, 11, "candle"), (37, 11, "column"), (43, 11, "column"), (37, 23, "column"), (43, 23, "column"), (21, 23, "mushroom"), (27, 28, "mushroom") })
        {
            Add(crypt, $"prop-{x}-{y}", sprite, ObjectKind.Scenery, x, y, sprite, true);
        }
        // All map objects intentionally sit on traversable ground (except the blocking gate).
        foreach (var zone in new[] { town, wilds, crypt })
        {
            foreach (var o in zone.Objects)
            {
                if (Zone.Solid(zone.At(o.Position)))
                {
                    zone.Set(o.Position, zone.Id == "crypt" ? Ground.Stone : Ground.Path);
                }
            }
        }

        Array.Fill(town.Seen, true);
        return [town, wilds, crypt];
    }
    private static void Add(Zone z, string id, string name, ObjectKind kind, int x, int y, string sprite, bool blocks = false, string detail = "") => z.Objects.Add(new(id, name, kind, new(x, y), sprite, blocks, detail));
    private static void House(Zone z, int x, int y, int w, int h)
    {
        z.Rect(x, y, w, h, Ground.Wall);
        z.Rect(x + 1, y + 1, w - 2, h - 2, Ground.Wood);
        z.Set(new(x + w / 2, y + h - 1), Ground.Wood);
    }
}
