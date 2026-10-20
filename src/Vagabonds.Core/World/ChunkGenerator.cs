using System.Security.Cryptography;

namespace Vagabonds.Core.World;

public static class Materials
{
    public static bool Walkable(Material m) => m is not (Material.Tree or Material.Water or Material.Rock or Material.Wall or Material.Storage);
    public static bool Opaque(Material m) => m is Material.Tree or Material.Rock or Material.Wall;
    public static int Hardness(Material m) => m switch { Material.Rock or Material.Wall => 4, Material.Tree => 2, _ => 1 };
    public static string Yield(Material m) => m is Material.Tree or Material.Door ? "wood" : "stone";
    public static string Sprite(Material m, Biome biome, CultureSample? culture = null) => m switch
    {
        Material.Tree => biome == Biome.Marsh ? "willow" : biome == Biome.Snow ? "pine" : "tree",
        Material.Water => "water", Material.Rock => "boulder", Material.Earth => "rock-floor",
        Material.Wall => culture?.Wall ?? "ruin-wall", Material.Floor => culture?.Floor ?? "stone",
        Material.Door => "wood", Material.Bridge => "bridge", Material.StairsDown => "stairs-down", Material.StairsUp => "stairs-up",
        Material.Storage => "chest", Material.Camp => "campfire", _ => biome is Biome.Highland or Biome.Badland ? "rock-floor" : "grass"
    };
}

public sealed record GenerationTrace(string Purpose, string Sample, int PatternCount, int Attempts, int Observations, long Propagations, int FallbackRegions, int RepairedCells, string RawHash, string FinalHash);
public sealed record Footprint(Material[] Cells, int Width, int Height, GenerationTrace Trace);
public sealed record GeneratedChunk(ChunkKey Key, Material[] Tiles, Biome[] Biomes, int[] Cultures, string BaseHash);

public sealed class ChunkGenerator(WorldPlan plan)
{
    private readonly Dictionary<(bool Town, int Id, int Floor), Footprint> _footprints = [];
    private readonly Queue<(bool Town, int Id, int Floor)> _order = [];
    public int CachedFootprints => _footprints.Count;
    public Footprint GenerateFootprint(bool town, int id, int floor = 0, bool forceFallback = false)
    {
        if (floor is < 0 or >= WorldPlan.Floors || town && floor != 0) throw new ArgumentException("Invalid footprint floor.");
        var key = (town, id, floor);
        if (!forceFallback && _footprints.TryGetValue(key, out var cached)) return cached;
        int culture = town ? plan.Towns[id].Culture : plan.Sites[id].Culture;
        string purpose = town ? "town" : plan.Sites[id].Purpose;
        var model = SampleLibrary.Default.Model(culture);
        const int size = 64;
        var tiles = Enumerable.Repeat(Material.Floor, size * size).ToArray();
        int attempts = 0, observations = 0, fallbacks = 0; long propagations = 0;
        // Four structural regions around a guaranteed central road/plaza. Collapse a whole
        // footprint before slicing chunks, so visitation order cannot change its seams.
        for (int region = 0; region < 4; region++)
        {
            const int w = 22, h = 20;
            int ox = region % 2 == 0 ? 5 : 37, oy = region / 2 == 0 ? 6 : 38;
            var pins = new Dictionary<int, char>();
            for (int x = 0; x < w; x++) { pins[x] = '.'; pins[(h - 1) * w + x] = '.'; }
            for (int y = 0; y < h; y++) { pins[y * w] = '.'; pins[y * w + w - 1] = '.'; }
            var result = forceFallback ? null : model.Solve(w, h, WorldRandom.Hash(plan.Seed, id, floor, region + (town ? 1000 : 2000)), pins);
            char[] cells;
            if (result == null)
            {
                fallbacks++; attempts += 8;
                cells = Enumerable.Repeat('.', w * h).ToArray();
                for (int y = 3; y < h - 3; y++) for (int x = 3; x < w - 3; x++)
                    if (x == 3 || y == 3 || x == w - 4 || y == h - 4) cells[y * w + x] = '#';
                cells[(h - 4) * w + w / 2] = '+';
            }
            else { cells = result.Cells; attempts += result.Attempts; observations += result.Observations; propagations += result.Propagations; }
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tiles[(oy + y) * size + ox + x] = cells[y * w + x] switch { '#' => Material.Wall, '+' => Material.Door, _ => Material.Floor };
        }
        string rawHash = Hash(tiles); int repairs = 0;
        if (!town) for (int i = 0; i < tiles.Length; i++)
            if (tiles[i] == Material.Wall && WorldRandom.Hash(plan.Seed, i, id, floor + 900) % 100 < 6) { tiles[i] = Material.Floor; repairs++; }
        // Validate/connect every floor component. This pass is reported separately from WFC.
        var seen = new bool[tiles.Length]; Flood(size / 2, size / 2);
        for (int i = 0; i < tiles.Length; i++)
        {
            if (seen[i] || !Materials.Walkable(tiles[i])) continue;
            int x = i % size, y = i / size;
            while (x != size / 2) { Carve(x, y); x += Math.Sign(size / 2 - x); }
            while (y != size / 2) { Carve(x, y); y += Math.Sign(size / 2 - y); }
            Flood(i % size, i / size);
        }
        if (!town)
        {
            if (floor > 0) tiles[32 * size + 30] = Material.StairsUp;
            if (floor < WorldPlan.Floors - 1) tiles[32 * size + 34] = Material.StairsDown;
        }
        var trace = new GenerationTrace(purpose, SampleLibrary.Default.Cultures[culture].Sample, model.PatternCount, attempts, observations, propagations, fallbacks, repairs, rawHash, Hash(tiles));
        var footprint = new Footprint(tiles, size, size, trace);
        if (!forceFallback)
        {
            if (_footprints.Count >= 8) _footprints.Remove(_order.Dequeue());
            _footprints[key] = footprint; _order.Enqueue(key);
        }
        return footprint;
        void Carve(int x, int y) { int i = y * size + x; if (!Materials.Walkable(tiles[i])) { tiles[i] = Material.Floor; repairs++; } }
        void Flood(int x, int y)
        {
            var q = new Queue<int>(); int start = y * size + x;
            if (!seen[start]) { seen[start] = true; q.Enqueue(start); }
            while (q.TryDequeue(out int i)) foreach (int d in new[] { -1, 1, -size, size })
            {
                int next = i + d;
                if (next < 0 || next >= tiles.Length || Math.Abs(next % size - i % size) + Math.Abs(next / size - i / size) != 1 || seen[next] || !Materials.Walkable(tiles[next])) continue;
                seen[next] = true; q.Enqueue(next);
            }
        }
    }
    public GeneratedChunk Generate(ChunkKey key)
    {
        if (!plan.Contains(new(key.X * 64, key.Y * 64, key.Floor))) throw new ArgumentException("Chunk outside world.");
        var tiles = new Material[4096]; var biomes = new Biome[4096]; var cultures = Enumerable.Repeat(-1, 4096).ToArray();
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        {
            int i = y * 64 + x; var p = new WorldPosition(key.X * 64 + x, key.Y * 64 + y, key.Floor);
            Biome biome = biomes[i] = plan.BiomeAt(p.X, p.Y);
            tiles[i] = NaturalAt(p, biome);
        }
        if (key.Floor == 0) foreach (var town in plan.Towns) Paint(true, town.Id, town.Center);
        foreach (var site in plan.Sites) Paint(false, site.Id, site.Center);
        // Public roads override surface terrain and become bridges over rivers.
        if (key.Floor == 0) foreach (var road in plan.Roads)
        {
            if (Math.Max(road.A.X, road.B.X) + 1 < key.X * 64 || Math.Min(road.A.X, road.B.X) - 1 >= (key.X + 1) * 64 || Math.Max(road.A.Y, road.B.Y) + 1 < key.Y * 64 || Math.Min(road.A.Y, road.B.Y) - 1 >= (key.Y + 1) * 64) continue;
            for (int i = 0; i < 4096; i++) if (road.Contains(new(key.X * 64 + i % 64, key.Y * 64 + i / 64))) tiles[i] = tiles[i] == Material.Water ? Material.Bridge : Material.Earth;
        }
        return new(key, tiles, biomes, cultures, Hash(tiles));
        void Paint(bool town, int id, WorldPosition center)
        {
            int left = center.X - 32, top = center.Y - 32;
            int x0 = Math.Max(left, key.X * 64), y0 = Math.Max(top, key.Y * 64), x1 = Math.Min(left + 64, (key.X + 1) * 64), y1 = Math.Min(top + 64, (key.Y + 1) * 64);
            if (x0 >= x1 || y0 >= y1) return;
            var f = GenerateFootprint(town, id, key.Floor); int c = town ? plan.Towns[id].Culture : plan.Sites[id].Culture;
            for (int y = y0; y < y1; y++) for (int x = x0; x < x1; x++)
            { int i = (y - key.Y * 64) * 64 + x - key.X * 64; tiles[i] = f.Cells[(y - top) * 64 + x - left]; cultures[i] = c; }
        }
    }
    public Material NaturalAt(WorldPosition p, Biome biome)
    {
        if (p.Floor > 0) return Material.Rock;
        // Coordinate-derived coarse and fine fields, never a per-chunk random stream.
        double river = 1800 + 280 * Math.Sin(p.Y / 230.0 + unchecked((uint)plan.Seed) % 100);
        if (Math.Abs(p.X - river) < 4) return Material.Water;
        uint local = WorldRandom.Hash(plan.Seed, p.X, p.Y, 11) % 1000;
        uint field = WorldRandom.Hash(plan.Seed, p.X / 16, p.Y / 16, 12) % 100;
        int trees = biome switch { Biome.Forest => 330, Biome.Marsh => 150, Biome.Snow => 130, Biome.Grassland => 60, _ => 15 };
        if (local < trees + field) return Material.Tree;
        if (biome == Biome.Marsh && field < 18 && local < 650) return Material.Water;
        if (biome is Biome.Highland or Biome.Badland && local > 850) return Material.Rock;
        return biome is Biome.Highland or Biome.Badland ? Material.Earth : Material.Grass;
    }
    public static string Hash(Material[] cells) => Convert.ToHexString(SHA256.HashData(cells.Select(m => (byte)m).ToArray()));
}

public sealed class ChunkCache(WorldPlan plan, WorldState state)
{
    private readonly Dictionary<ChunkKey, (GeneratedChunk Chunk, long Used)> _resident = [];
    private readonly Dictionary<ChunkKey, ChunkDelta> _deltas = state.Chunks.ToDictionary(c => c.Key);
    private long _use;
    public ChunkGenerator Generator { get; } = new(plan);
    public int ResidentCount => _resident.Count;
    public const int Capacity = 25;
    public GeneratedChunk Get(ChunkKey key)
    {
        if (_resident.TryGetValue(key, out var item)) { _resident[key] = (item.Chunk, ++_use); return item.Chunk; }
        var chunk = Generator.Generate(key);
        if (_resident.Count >= Capacity) _resident.Remove(_resident.MinBy(kv => kv.Value.Used).Key);
        _resident[key] = (chunk, ++_use); return chunk;
    }
    public Material At(WorldPosition p)
    {
        if (!plan.Contains(p)) return Material.Rock;
        return _deltas.TryGetValue(p.Chunk, out var delta) && delta.Edits.TryGetValue(p.LocalIndex, out var edit) ? edit : Get(p.Chunk).Tiles[p.LocalIndex];
    }
    public bool Seen(WorldPosition p) => plan.Contains(p) && _deltas.TryGetValue(p.Chunk, out var delta) && delta.Discovered.Contains(p.LocalIndex);
    public ChunkDelta Delta(ChunkKey key)
    {
        if (_deltas.TryGetValue(key, out var delta)) return delta;
        delta = new() { Key = key }; _deltas[key] = delta; state.Chunks.Add(delta); return delta;
    }
    public void Edit(WorldPosition p, Material material) { Delta(p.Chunk).Edits[p.LocalIndex] = material; state.NavigationRevision++; }
    public void Prefetch(WorldPosition center)
    {
        for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
        { var k = center.Chunk with { X = center.Chunk.X + dx, Y = center.Chunk.Y + dy }; if (k.X >= 0 && k.Y >= 0 && k.X < 64 && k.Y < 64) Get(k); }
    }
}
