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
    public int Seed { get; init; }
    public int Version { get; init; } = GenerationVersion;
    public string SampleHash { get; init; } = SampleLibrary.Default.Hash;
    public string Id { get; init; } = "";
    public List<TownPlan> Towns { get; init; } = [];
    public List<SitePlan> Sites { get; init; } = [];
    public List<RoadPlan> Roads { get; init; } = [];
    public List<BiomeRegion> BiomeRegions { get; init; } = [];
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
        if (Towns.Any(t => !Contains(t.Center)) || Sites.Any(t => !Contains(t.Center))) errors.Add("Settlement/site outside world.");
        if (Towns.Any(t => t.Culture is < 0 or >= 6 || t.FactionId is < 0 or >= 6 || t.Name == null) || Sites.Any(s => s.Culture is < 0 or >= 6 || s.Name == null || s.Purpose is not ("ruin" or "mine" or "shrine" or "fort"))) errors.Add("Invalid culture/site purpose.");
        if (Roads.Count > 100 || Roads.Any(r => r.FromTown is < 0 or >= 24 || r.ToTown is < 0 or >= 24)) errors.Add("Invalid roads.");
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
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public ActorKind Kind { get; set; }
    public WorldPosition Position { get; set; }
    public int HomeTown { get; set; }
    public int FactionId { get; set; }
    public int Culture { get; set; }
    public string Background { get; set; } = "";
    public string Trait { get; set; } = "";
    public string Motivation { get; set; } = "";
    public string Job { get; set; } = "";
    public long FriendId { get; set; }
    public int Role { get; set; }
    public int Level { get; set; } = 1;
    public int Experience { get; set; }
    public int Health { get; set; }
    public int Mana { get; set; }
    public int Stress { get; set; }
    public LifeState Life { get; set; }
    public bool Recruited { get; set; }
    public Dictionary<string, int> Cargo { get; set; } = [];
    public int? DestinationTown { get; set; }
    public long NextTravelTick { get; set; }
    public long DepartureTick { get; set; }
    public bool Traveling { get; set; }
    public List<int> Route { get; set; } = [];
    public int MaxHealth => Kind == ActorKind.Person ? new Hero { Role = Role, Level = Level }.MaxHealth : Kind == ActorKind.Beast ? 18 : 30;
    public int MaxMana => new Hero { Role = Role, Level = Level }.MaxMana;
    public string Sprite => Kind switch { ActorKind.Beast => "wolf", ActorKind.Raider => "bandit", ActorKind.Caravan => "merchant", _ => new Hero { Role = Role }.Sprite };
}

public sealed class TownState
{
    public int Id { get; set; }
    public Dictionary<string, int> Stocks { get; set; } = new() { ["food"] = 100, ["wood"] = 100, ["stone"] = 100, ["medicine"] = 25 };
    public int Vacancies { get; set; }
    public int Shortage { get; set; }
    public int RaidLosses { get; set; }
    public int Price(string item) => 2 + Math.Max(0, 100 - Stocks.GetValueOrDefault(item)) / 10;
}
public sealed class FactionState
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int[] Relations { get; set; } = new int[6];
}
public sealed class WorldArc
{
    public ArcKind Kind { get; set; }
    public ArcStatus Status { get; set; }
    public int TownId { get; set; }
    public int SiteId { get; set; }
    public long PatronId { get; set; }
    public int Progress { get; set; }
    public string Objective { get; set; } = "";
    public string Outcome { get; set; } = "";
}
public sealed class RegionalTask
{
    public string Id { get; set; } = "";
    public int TownId { get; set; }
    public int? RoadId { get; set; }
    public string Description { get; set; } = "";
    public bool Complete { get; set; }
}
public sealed class ChunkDelta
{
    public ChunkKey Key { get; set; }
    public Dictionary<int, Material> Edits { get; set; } = [];
    public HashSet<int> Discovered { get; set; } = [];
    public Dictionary<int, Dictionary<string, int>> Storage { get; set; } = [];
}
public sealed class WorldState
{
    public long Tick { get; set; }
    public uint CombatRandom { get; set; }
    public int PartyOrdinal { get; set; }
    public long NextActorId { get; set; } = 100000;
    public List<Actor> Actors { get; set; } = [];
    public List<long> Party { get; set; } = [];
    public WorldPosition Position { get; set; }
    public Dictionary<string, int> Inventory { get; set; } = new() { ["food"] = 8, ["wood"] = 20, ["stone"] = 12, ["medicine"] = 3, ["gold"] = 25 };
    public List<TownState> Towns { get; set; } = [];
    public List<FactionState> Factions { get; set; } = [];
    public List<ChunkDelta> Chunks { get; set; } = [];
    public List<string> Journal { get; set; } = [];
    public HashSet<int> VisitedSites { get; set; } = [];
    public WorldArc Arc { get; set; } = new();
    public List<RegionalTask> Tasks { get; set; } = [];
    public WorldBattle? Battle { get; set; }
    public bool AwaitingContinuation { get; set; }
    public long NavigationRevision { get; set; }
    public long ResourcesProduced { get; set; }
    public long ResourcesConsumed { get; set; }
    public long ResourcesLost { get; set; }
}

public sealed class WorldBattle
{
    public List<CombatUnit> Units { get; set; } = [];
    public List<long> ActorIds { get; set; } = [];
    public List<int> Order { get; set; } = [];
    public int TurnIndex { get; set; }
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
