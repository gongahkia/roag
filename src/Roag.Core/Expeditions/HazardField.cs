namespace Roag.Core.Expeditions;

public enum HazardKind : byte { None, Fire, Spores, Corruption }

public sealed class HazardField(int width, int height)
{
    public HazardKind[] Cells { get; } = new HazardKind[width * height];
    public byte[] Age { get; } = new byte[width * height];
    public byte[] Fuel { get; } = new byte[width * height];
    public HazardKind[] Sources { get; } = new HazardKind[width * height];
    public byte[] Suppressed { get; } = new byte[width * height];
    public int Turns { get; set; }
    public int Ticks { get; set; }
    private HazardKind[] _next = new HazardKind[width * height];
    private byte[] _nextAge = new byte[width * height];

    public void Initialize(Zone zone, int seed)
    {
        if (zone.Id == "hearth" || zone.Generation.Version == 1 || zone.Generation.Algorithm == "Authored compatibility fallback") return;
        for (int i = 0; i < Fuel.Length; i++) Fuel[i] = GenerationRules.Default.Materials[zone.Tiles[i].ToString()].Fuel;
        var random = GenerationSeeds.Stream(seed, zone.Id == "wilds" ? 40U : 41U);
        // Separate sources make the three rule sets observable without blanket coverage.
        foreach (var kind in new[] { HazardKind.Fire, HazardKind.Spores, HazardKind.Corruption })
        {
            if (kind == HazardKind.Fire && zone.Id == "crypt") continue;
            var candidates = Enumerable.Range(0, Cells.Length).Where(i => !zone.Safe[i]
                && (kind == HazardKind.Fire ? Fuel[i] > 0 : zone.Walkable(new(i % width, i / width)))
                && Cells[i] == HazardKind.None && !zone.Objects.Any(o => zone.Index(o.Position) == i)).ToArray();
            if (candidates.Length == 0) continue;
            string targetId = kind switch
            {
                HazardKind.Fire => "wood-camp",
                HazardKind.Spores => zone.Id == "crypt" ? "abbey-slimes" : "forest-shrine",
                _ => zone.Id == "crypt" ? "abbey-hollows" : "wilds-raiders"
            };
            Cell target = zone.Objects.FirstOrDefault(o => o.Id == targetId)?.Position ?? new(width / 2, height / 2);
            int nearest = candidates.Min(i => new Cell(i % width, i / width).Distance(target));
            candidates = candidates.Where(i => new Cell(i % width, i / width).Distance(target) <= nearest + 1).ToArray();
            int source = candidates[random.Next(candidates.Length)];
            Cells[source] = kind;
            Age[source] = 1;
            if (kind != HazardKind.Fire) Sources[source] = kind;
        }
    }

    public bool Advance(Zone zone, int seed)
    {
        Turns++;
        if (Turns % GenerationRules.Default.Hazards.StepsPerTick != 0) return false;
        Tick(zone, seed);
        return true;
    }

    public void Tick(Zone zone, int seed)
    {
        // Two flat reusable buffers; scan direction cannot affect spread.
        Ticks++;
        Array.Clear(_next);
        Array.Clear(_nextAge);
        for (int i = 0; i < Cells.Length; i++)
        {
            if (zone.Safe[i]) continue;
            if (Suppressed[i] > 0) { Suppressed[i]--; continue; }
            Cell p = new(i % width, i / width);
            var kind = Cells[i];
            if (kind != HazardKind.None)
            {
                int limit = kind switch { HazardKind.Fire => 5, HazardKind.Spores => 4, _ => 7 };
                if (kind == HazardKind.Fire && Fuel[i] > 0) Fuel[i]--;
                if (kind == HazardKind.Fire && Fuel[i] == 0)
                    zone.GroundSprites[i] = zone.Tiles[i] == Ground.Tree ? "dead-tree" : zone.Tiles[i] == Ground.Grass ? "grit" : zone.GroundSprites[i];
                if (Age[i] < limit && (kind != HazardKind.Fire || Fuel[i] > 0))
                {
                    _next[i] = kind;
                    _nextAge[i] = (byte)(Age[i] + 1);
                }
                else Suppressed[i] = 3;
                continue;
            }
            if (Sources[i] != HazardKind.None)
            {
                _next[i] = Sources[i];
                _nextAge[i] = 1;
                continue;
            }
            int fire = 0, spores = 0, corruption = 0;
            foreach (var n in zone.Neighbors(p))
            {
                if (!zone.Inside(n)) continue;
                switch (Cells[zone.Index(n)])
                {
                    case HazardKind.Fire: fire++; break;
                    case HazardKind.Spores: spores++; break;
                    case HazardKind.Corruption: corruption++; break;
                }
            }
            uint noise = GenerationSeeds.Mix(unchecked((uint)seed), unchecked((uint)(i * 97 + Ticks * 31)));
            if (fire > 0 && Fuel[i] > 0 && noise % 4 < fire) _next[i] = HazardKind.Fire;
            else if (spores > 0 && zone.Walkable(p) && noise % 3 < spores) _next[i] = HazardKind.Spores;
            else if (corruption > 0 && zone.Walkable(p) && noise % 5 < corruption) _next[i] = HazardKind.Corruption;
            if (_next[i] != HazardKind.None) _nextAge[i] = 1;
        }
        Array.Copy(_next, Cells, Cells.Length);
        Array.Copy(_nextAge, Age, Age.Length);
    }

    public int ClearNear(Zone zone, Cell origin, int radius)
    {
        int cleared = 0;
        for (int y = Math.Max(0, origin.Y - radius); y <= Math.Min(height - 1, origin.Y + radius); y++)
            for (int x = Math.Max(0, origin.X - radius); x <= Math.Min(width - 1, origin.X + radius); x++)
            {
                Cell p = new(x, y);
                if (p.Distance(origin) > radius) continue;
                int i = zone.Index(p);
                if (Cells[i] != HazardKind.None || Sources[i] != HazardKind.None) cleared++;
                Cells[i] = Sources[i] = HazardKind.None;
                Age[i] = 0;
                Suppressed[i] = GenerationRules.Default.Hazards.SuppressionTicks;
            }
        return cleared;
    }
    public bool Nearby(Zone zone, Cell origin, int radius) => Enumerable.Range(0, Cells.Length)
        .Any(i => (Cells[i] != HazardKind.None || Sources[i] != HazardKind.None) && origin.Distance(new(i % width, i / width)) <= radius);
}
