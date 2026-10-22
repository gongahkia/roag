using System.Text.Json.Serialization;
using Vagabonds.Core.Expeditions;

namespace Vagabonds.Core.World;

public readonly record struct WorldPosition(int X, int Y, int Floor = 0)
{
    public ChunkKey Chunk => new(X / WorldPlan.ChunkSize, Y / WorldPlan.ChunkSize, Floor);
    public int LocalIndex => Y % WorldPlan.ChunkSize * WorldPlan.ChunkSize + X % WorldPlan.ChunkSize;
    public int Distance(WorldPosition other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y) + Math.Abs(Floor - other.Floor) * 10000;
    public WorldPosition Offset(int x, int y) => this with { X = X + x, Y = Y + y };
}

public readonly record struct ChunkKey(int X, int Y, int Floor = 0)
{
    public override string ToString() => $"{Floor}_{X}_{Y}";
}

public enum Biome { Grassland, Forest, Marsh, Highland, Badland, Snow }
public enum Material { Grass, Tree, Water, Rock, Earth, Floor, Wall, Door, Bridge, StairsDown, StairsUp, Storage, Camp }
public enum ActorKind { Person, Beast, Raider, Patrol, Caravan }
public enum LifeState { Alive, Downed, Dead }
public enum ArcKind { TradeConflict, SpreadingBlight, ContestedSuccession }
public enum ArcStatus { Active, Resolved, Failed }

public static class WorldRandom
{
    public static uint Hash(int seed, int x, int y = 0, int salt = 0)
    {
        unchecked
        {
            uint h = (uint)seed ^ (uint)x * 0x9e3779b9u ^ (uint)y * 0x85ebca6bu ^ (uint)salt * 0xc2b2ae35u;
            h ^= h >> 16; h *= 0x7feb352du; h ^= h >> 15; h *= 0x846ca68bu; return h ^ (h >> 16);
        }
    }
    public static RandomStream Stream(int seed, int x, int y = 0, int salt = 0) => new(Hash(seed, x, y, salt));
    public static double Noise(int seed, int x, int y, int scale, int salt)
    {
        int gx = x / scale, gy = y / scale;
        double fx = x % scale / (double)scale, fy = y % scale / (double)scale;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        double a = Hash(seed, gx, gy, salt) / (double)uint.MaxValue, b = Hash(seed, gx + 1, gy, salt) / (double)uint.MaxValue;
        double c = Hash(seed, gx, gy + 1, salt) / (double)uint.MaxValue, d = Hash(seed, gx + 1, gy + 1, salt) / (double)uint.MaxValue;
        return (a + (b - a) * fx) * (1 - fy) + (c + (d - c) * fx) * fy;
    }
}

public sealed record TownPlan(int Id, string Name, int Culture, int FactionId, WorldPosition Center);
public sealed record SitePlan(int Id, string Name, int Culture, string Purpose, WorldPosition Center);
public sealed record BiomeRegion(Biome Biome, int X, int Y);
public sealed record RoadPlan(int Id, int FromTown, int ToTown, WorldPosition A, WorldPosition B)
{
    public int Length => A.Distance(B);
    public bool Contains(WorldPosition p, int width = 1) => p.Floor == 0 &&
        (Math.Abs(p.Y - A.Y) <= width && p.X >= Math.Min(A.X, B.X) && p.X <= Math.Max(A.X, B.X) ||
         Math.Abs(p.X - B.X) <= width && p.Y >= Math.Min(A.Y, B.Y) && p.Y <= Math.Max(A.Y, B.Y));
}

public sealed class WorldPlan
{
    public const int GenerationVersion = 2;
    public const int ChunkSize = 64;
    public const int ChunksAcross = 64;
    public const int Size = ChunkSize * ChunksAcross;
    public const int Floors = 4;
    [JsonRequired]
    public int Seed { get; init; }
    [JsonRequired]
    public int Version { get; init; } = GenerationVersion;
    [JsonRequired]
    public string SampleHash { get; init; } = SampleLibrary.Default.Hash;
    [JsonRequired]
    public string Id { get; init; } = "";
    [JsonRequired]
    public List<TownPlan> Towns { get; init; } = [];
    [JsonRequired]
    public List<SitePlan> Sites { get; init; } = [];
    [JsonRequired]
    public List<RoadPlan> Roads { get; init; } = [];
    [JsonRequired]
    public List<BiomeRegion> BiomeRegions { get; init; } = [];
    [JsonRequired]
    public List<string> History { get; init; } = [];
    public bool Contains(WorldPosition p) => p.X >= 0 && p.Y >= 0 && p.X < Size && p.Y < Size && p.Floor is >= 0 and < Floors;
    public Biome BiomeAt(int x, int y)
    {
        double wx = x + (WorldRandom.Noise(Seed, x, y, 512, 21) - .5) * 360;
        double wy = y + (WorldRandom.Noise(Seed, x, y, 512, 22) - .5) * 360;
        Biome selected = Biome.Grassland; double best = double.MaxValue;
        foreach (var region in BiomeRegions)
        {
            double distance = (wx - region.X) * (wx - region.X) + (wy - region.Y) * (wy - region.Y);
            if (distance < best) { best = distance; selected = region.Biome; }
        }
        return selected;
    }
    public static WorldPlan Generate(int seed)
    {
        var r = WorldRandom.Stream(seed, 0, salt: 700);
        string[] starts = ["Amber", "Briar", "Cairn", "Dawn", "Ember", "Fern", "Grey", "Hollow"];
        string[] ends = ["ford", "wick", "mere", "haven", "watch", "bridge"];
        var plan = new WorldPlan { Seed = seed, Id = $"world-{unchecked((uint)seed):x8}" };
        var biomes = Enum.GetValues<Biome>();
        for (int i = biomes.Length - 1; i > 0; i--) { int j = r.Next(i + 1); (biomes[i], biomes[j]) = (biomes[j], biomes[i]); }
        for (int i = 0; i < 6; i++) plan.BiomeRegions.Add(new(biomes[i], 500 + i % 3 * 1400 + r.Next(250), 800 + i / 3 * 2100 + r.Next(350)));
        for (int y = 0; y < 4; y++) for (int x = 0; x < 6; x++)
        {
            int i = y * 6 + x;
            var p = new WorldPosition(260 + x * 650 + r.Next(120), 320 + y * 940 + r.Next(150));
            plan.Towns.Add(new(i, starts[r.Next(starts.Length)] + ends[i % 6] + $" {i + 1}", i % 6, i % 6, p));
        }
        // A connected lattice, rather than independent roads that can strand a town.
        foreach (var t in plan.Towns)
        {
            if (t.Id % 6 < 5) AddRoad(t, plan.Towns[t.Id + 1]);
            if (t.Id < 18) AddRoad(t, plan.Towns[t.Id + 6]);
        }
        for (int y = 0; y < 6; y++) for (int x = 0; x < 8; x++)
        {
            int i = y * 8 + x;
            var p = new WorldPosition(160 + x * 500 + r.Next(100), 160 + y * 650 + r.Next(100));
            if (plan.Towns.Any(t => Math.Abs(t.Center.X - p.X) < 96 && Math.Abs(t.Center.Y - p.Y) < 96)) p = p.Offset(0, 220);
            string purpose = new[] { "ruin", "mine", "shrine", "fort" }[i % 4];
            plan.Sites.Add(new(i, $"{starts[r.Next(8)]} {purpose} {i + 1}", i % 6, purpose, p));
        }
        foreach (var t in plan.Towns) plan.History.Add($"{t.Name} was founded by the {SampleLibrary.Default.Cultures[t.Culture].Name} people near the old trade roads.");
        return plan;
        void AddRoad(TownPlan a, TownPlan b) => plan.Roads.Add(new(plan.Roads.Count, a.Id, b.Id, a.Center, b.Center));
    }
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (Towns == null || Sites == null || Roads == null || History == null || BiomeRegions == null || Towns.Any(t => t == null) || Sites.Any(t => t == null) || Roads.Any(r => r == null) || BiomeRegions.Any(r => r == null)) return ["Missing world plan data."];
        if (BiomeRegions.Count != 6 || BiomeRegions.Select(b => b.Biome).Distinct().Count() != 6 || BiomeRegions.Any(b => !Enum.IsDefined(b.Biome) || !Contains(new(b.X, b.Y)))) errors.Add("Invalid biome geography.");
        if (Towns.Count != 24 || Sites.Count != 48) errors.Add("World requires 24 towns and 48 sites.");
        if (Towns.Any(t => !Contains(t.Center) || t.Center.Floor != 0) || Sites.Any(t => !Contains(t.Center) || t.Center.Floor != 0)) errors.Add("Settlement/site outside surface world.");
        if (Towns.Any(t => t.Culture is < 0 or >= 6 || t.FactionId is < 0 or >= 6 || t.Name == null) || Sites.Any(s => s.Culture is < 0 or >= 6 || s.Name == null || s.Purpose is not ("ruin" or "mine" or "shrine" or "fort"))) errors.Add("Invalid culture/site purpose.");
        if (Roads.Count > 100 || Roads.Any(r => r.FromTown is < 0 or >= 24 || r.ToTown is < 0 or >= 24)) errors.Add("Invalid roads.");
        if (Roads.Any(r => !Contains(r.A) || !Contains(r.B) || r.A.Floor != 0 || r.B.Floor != 0)) errors.Add("Road endpoints outside surface.");
        if (Towns.Count == 24 && Roads.All(r => r.FromTown is >= 0 and < 24 && r.ToTown is >= 0 and < 24) && Roads.Any(r => r.A != Towns[r.FromTown].Center || r.B != Towns[r.ToTown].Center)) errors.Add("Road endpoints miss their towns.");
        var reached = new HashSet<int> { 0 };
        for (int i = 0; i < Towns.Count; i++) foreach (var road in Roads)
            if (reached.Contains(road.FromTown) || reached.Contains(road.ToTown)) { reached.Add(road.FromTown); reached.Add(road.ToTown); }
        if (reached.Count != Towns.Count) errors.Add("Disconnected road graph.");
        if (Towns.Select(t => t.FactionId).Distinct().Count() != 6) errors.Add("Missing faction.");
        return errors;
    }
}

public sealed class Actor
{
    [JsonRequired]
    public long Id { get; set; }
    [JsonRequired]
    public string Name { get; set; } = "";
    [JsonRequired]
    public ActorKind Kind { get; set; }
    [JsonRequired]
    public WorldPosition Position { get; set; }
    [JsonRequired]
    public int HomeTown { get; set; }
    [JsonRequired]
    public int FactionId { get; set; }
    [JsonRequired]
    public int Culture { get; set; }
    [JsonRequired]
    public string Background { get; set; } = "";
    [JsonRequired]
    public string Trait { get; set; } = "";
    [JsonRequired]
    public string Motivation { get; set; } = "";
    [JsonRequired]
    public string Job { get; set; } = "";
    [JsonRequired]
    public long FriendId { get; set; }
    [JsonRequired]
    public int Role { get; set; }
    [JsonRequired]
    public int Level { get; set; } = 1;
    [JsonRequired]
    public int Experience { get; set; }
    [JsonRequired]
    public int Health { get; set; }
    [JsonRequired]
    public int Mana { get; set; }
    [JsonRequired]
    public int Stress { get; set; }
    [JsonRequired]
    public LifeState Life { get; set; }
    [JsonRequired]
    public bool Recruited { get; set; }
    [JsonRequired]
    public Dictionary<string, int> Cargo { get; set; } = [];
    [JsonRequired]
    public int? DestinationTown { get; set; }
    [JsonRequired]
    public long NextTravelTick { get; set; }
    [JsonRequired]
    public long DepartureTick { get; set; }
    [JsonRequired]
    public bool Traveling { get; set; }
    [JsonRequired]
    public List<int> Route { get; set; } = [];
    public int MaxHealth => Kind == ActorKind.Person ? new Hero { Role = Role, Level = Level }.MaxHealth : Kind == ActorKind.Beast ? 18 : 30;
    public int MaxMana => new Hero { Role = Role, Level = Level }.MaxMana;
    public string Sprite => Kind switch { ActorKind.Beast => "wolf", ActorKind.Raider => "bandit", ActorKind.Caravan => "merchant", _ => new Hero { Role = Role }.Sprite };
}

public sealed class TownState
{
    [JsonRequired]
    public int Id { get; set; }
    [JsonRequired]
    public Dictionary<string, int> Stocks { get; set; } = new() { ["food"] = 100, ["wood"] = 100, ["stone"] = 100, ["medicine"] = 25 };
    [JsonRequired]
    public int Vacancies { get; set; }
    [JsonRequired]
    public int Shortage { get; set; }
    [JsonRequired]
    public int RaidLosses { get; set; }
    public int Price(string item) => 2 + Math.Max(0, 100 - Stocks.GetValueOrDefault(item)) / 10;
}
public sealed class FactionState
{
    [JsonRequired]
    public int Id { get; set; }
    [JsonRequired]
    public string Name { get; set; } = "";
    [JsonRequired]
    public int[] Relations { get; set; } = new int[6];
}
public sealed class WorldArc
{
    [JsonRequired]
    public ArcKind Kind { get; set; }
    [JsonRequired]
    public ArcStatus Status { get; set; }
    [JsonRequired]
    public int TownId { get; set; }
    [JsonRequired]
    public int SiteId { get; set; }
    [JsonRequired]
    public long PatronId { get; set; }
    [JsonRequired]
    public int Progress { get; set; }
    [JsonRequired]
    public string Objective { get; set; } = "";
    [JsonRequired]
    public string Outcome { get; set; } = "";
}
public sealed class RegionalTask
{
    [JsonRequired]
    public string Id { get; set; } = "";
    [JsonRequired]
    public int TownId { get; set; }
    [JsonRequired]
    public int? RoadId { get; set; }
    [JsonRequired]
    public string Description { get; set; } = "";
    [JsonRequired]
    public bool Complete { get; set; }
}
public sealed class ChunkDelta
{
    [JsonRequired]
    public ChunkKey Key { get; set; }
    [JsonRequired]
    public Dictionary<int, Material> Edits { get; set; } = [];
    [JsonRequired]
    public HashSet<int> Discovered { get; set; } = [];
    [JsonRequired]
    public Dictionary<int, Dictionary<string, int>> Storage { get; set; } = [];
}
public sealed class WorldState
{
    [JsonRequired]
    public long Tick { get; set; }
    [JsonRequired]
    public uint CombatRandom { get; set; }
    [JsonRequired]
    public int PartyOrdinal { get; set; }
    [JsonRequired]
    public long NextActorId { get; set; } = 100000;
    [JsonRequired]
    public List<Actor> Actors { get; set; } = [];
    [JsonRequired]
    public List<long> Party { get; set; } = [];
    [JsonRequired]
    public WorldPosition Position { get; set; }
    [JsonRequired]
    public Dictionary<string, int> Inventory { get; set; } = new() { ["food"] = 8, ["wood"] = 20, ["stone"] = 12, ["medicine"] = 3, ["gold"] = 25 };
    [JsonRequired]
    public List<TownState> Towns { get; set; } = [];
    [JsonRequired]
    public List<FactionState> Factions { get; set; } = [];
    // Manifest state omits this list; immutable chunk references restore it separately.
    public List<ChunkDelta> Chunks { get; set; } = [];
    [JsonRequired]
    public List<string> Journal { get; set; } = [];
    [JsonRequired]
    public HashSet<int> VisitedSites { get; set; } = [];
    [JsonRequired]
    public WorldArc Arc { get; set; } = new();
    [JsonRequired]
    public List<RegionalTask> Tasks { get; set; } = [];
    [JsonRequired]
    public WorldBattle? Battle { get; set; }
    [JsonRequired]
    public bool AwaitingContinuation { get; set; }
    [JsonRequired]
    public long NavigationRevision { get; set; }
    [JsonRequired]
    public long ResourcesProduced { get; set; }
    [JsonRequired]
    public long ResourcesConsumed { get; set; }
    [JsonRequired]
    public long ResourcesLost { get; set; }
}

public sealed class WorldBattle
{
    [JsonRequired]
    public List<CombatUnit> Units { get; set; } = [];
    [JsonRequired]
    public List<long> ActorIds { get; set; } = [];
    [JsonRequired]
    public List<int> Order { get; set; } = [];
    [JsonRequired]
    public int TurnIndex { get; set; }
    [JsonRequired]
    public int Round { get; set; } = 1;
    [JsonIgnore] public CombatUnit Active => Units[Order[TurnIndex]];
}

public interface IGameSession
{
    int Seed { get; }
    bool IsInCombat { get; }
    void Save(string path);
}
public sealed class LegacyCampaignSession(Campaign campaign) : IGameSession
{
    public Campaign Campaign { get; } = campaign;
    public int Seed => Campaign.Seed;
    public bool IsInCombat => Campaign.Battle != null;
    public void Save(string path) => Campaign.Save(path);
}
