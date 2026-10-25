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
    public const int GenerationVersion = 4;
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
        for (int i = 0; i < 24; i++)
        {
            WorldPosition p = i switch { 0 => new(220 + r.Next(280), 240 + r.Next(300)), 1 => new(900 + r.Next(280), 240 + r.Next(400)), _ => default };
            if (i > 1)
            {
                int attempt = 0;
                do { p = new(120 + r.Next(Size - 240), 120 + r.Next(Size - 240)); }
                while (plan.Towns.Any(t => p.Distance(t.Center) < 430) && ++attempt < 10000);
                if (attempt == 10000) throw new InvalidDataException("Cannot place separated settlements.");
            }
            plan.Towns.Add(new(i, starts[r.Next(starts.Length)] + ends[r.Next(ends.Length)] + $" {i + 1}", i % 6, i % 6, p));
        }
        AddRoad(plan.Towns[0], plan.Towns[1]);
        var linked = new HashSet<int> { 0, 1 };
        while (linked.Count < plan.Towns.Count)
        {
            var edge = (from a in plan.Towns where linked.Contains(a.Id) from b in plan.Towns where !linked.Contains(b.Id)
                orderby a.Center.Distance(b.Center), a.Id, b.Id select (a, b)).First();
            AddRoad(edge.a, edge.b); linked.Add(edge.b.Id);
        }
        // A geographic ring supplies alternate routes around every trunk-road blockade.
        var ring = plan.Towns.OrderBy(t => Math.Atan2(t.Center.Y - Size / 2.0, t.Center.X - Size / 2.0)).ToArray();
        for (int i = 0; i < ring.Length; i++) AddRoad(ring[i], ring[(i + 1) % ring.Length]);
        for (int i = 0; i < 48; i++)
        {
            WorldPosition p; int attempt = 0;
            do { p = new(90 + r.Next(Size - 180), 90 + r.Next(Size - 180)); }
            while ((plan.Towns.Any(t => Math.Abs(t.Center.X - p.X) < 100 && Math.Abs(t.Center.Y - p.Y) < 100)
                || plan.Sites.Any(t => p.Distance(t.Center) < 230)) && ++attempt < 10000);
            if (attempt == 10000) throw new InvalidDataException("Cannot place separated adventure sites.");
            string purpose = new[] { "ruin", "mine", "shrine", "fort" }[i % 4];
            plan.Sites.Add(new(i, $"{starts[r.Next(8)]} {purpose} {i + 1}", i % 6, purpose, p));
        }
        foreach (var t in plan.Towns) plan.History.Add($"{t.Name} was founded by the {SampleLibrary.Default.Cultures[t.Culture].Name} people near the old trade roads.");
        return plan;
        void AddRoad(TownPlan a, TownPlan b)
        {
            if (a.Id == b.Id || plan.Roads.Any(r => r.FromTown == a.Id && r.ToTown == b.Id || r.FromTown == b.Id && r.ToTown == a.Id)) return;
            plan.Roads.Add(new(plan.Roads.Count, a.Id, b.Id, a.Center, b.Center));
        }
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

public sealed record Preparation(string Definition, string Effect, int Power, long Until);
public sealed record CampStory(long Speaker, string Text, long Tick, WorldPosition Camp);
public enum RoutineKind { Danger, Food, Rest, AssignedWork, Occupation, Social, Home }
public sealed record ResidentRoutine(RoutineKind Kind, WorldPosition Target, long Since, string Reason, long Other = 0, bool Blocked = false);
public sealed class ResidentJourney
{
    [JsonRequired] public WorldPosition Origin { get; set; }
    [JsonRequired] public WorldPosition Target { get; set; }
    [JsonRequired] public List<WorldPosition> Steps { get; set; } = [];
    [JsonRequired] public int Next { get; set; }
    [JsonRequired] public long RetryAt { get; set; }
    [JsonRequired] public long Revision { get; set; }
}
public enum OccupationKind { Gather, Fell, Quarry, Craft, BuildWorkplace, RepairWorkplace }
public enum OccupationPhase { Collecting, Working, Delivering, Returning }
public sealed class OccupationJob
{
    [JsonRequired] public long Id { get; set; }
    [JsonRequired] public OccupationKind Kind { get; set; }
    [JsonRequired] public OccupationPhase Phase { get; set; }
    [JsonRequired] public WorldPosition Target { get; set; }
    [JsonRequired] public string Definition { get; set; } = "";
    [JsonRequired] public Material Before { get; set; }
    [JsonRequired] public double Required { get; set; }
    [JsonRequired] public double Progress { get; set; }
    [JsonRequired] public Dictionary<string, int> Inputs { get; set; } = [];
    [JsonRequired] public Dictionary<string, int> Yield { get; set; } = [];
    [JsonRequired] public Dictionary<string, int> Carried { get; set; } = [];
    [JsonRequired] public Dictionary<string, int> Delivered { get; set; } = [];
    [JsonRequired] public Dictionary<string, int> Consumed { get; set; } = [];
    [JsonRequired] public long LastProgressTick { get; set; }
    [JsonRequired] public WorldPosition LastPosition { get; set; }
}
public sealed class Actor
{
    private WorldPosition _position;
    private long _id;
    internal event Action<Actor, WorldPosition>? IndexChanged;
    internal event Action<Actor>? NeedsChanged;
    private BodyState _body = new();
    private StateList<Preparation> _preparations = [];
    private StateList<ActorMemory> _memories = [];
    private string _traitId = "";
    private int _level = 1;
    private LifeState _life;
    private bool _recruited;
    private ResidentRoutine? _routine;
    private OccupationJob? _occupation;
    private WildlifeMigration? _migration;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public WorldPosition? NatalHabitat { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] public long WildlifeAfter { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public WildlifeMigration? Migration
    {
        get => _migration;
        set { if (_migration == value) return; if (IndexChanged != null && Journey is { } journey && journey.Target != value?.Target) Journey = null; _migration = value; NotifyNeedsChanged(); }
    }
    public int OccupationVersion { get; set; }
    public long OccupationAfter { get; set; }
    public WorldPosition? OccupationWorkplace { get; set; }
    public WorldPosition? OccupationAvoid { get; set; }
    public long OccupationAvoidUntil { get; set; }
    public OccupationJob? Occupation { get => _occupation; set { if (_occupation == value) return; _occupation = value; NotifyNeedsChanged(); } }
    public ResidentJourney? Journey { get; set; }
    internal ResidentJourney? GroundJourney;
    internal ResidentGround[]? JourneyGround;
    internal (WorldPosition Position, ResidentGround Ground)? AnchorGround;
    public Actor()
    {
        _body.Changed += NotifyNeedsChanged; _preparations.Changed += NotifyNeedsChanged; _memories.Changed += NotifyNeedsChanged;
    }
    private void NotifyNeedsChanged() => NeedsChanged?.Invoke(this);
    public ResidentRoutine? Routine
    {
        get => _routine;
        set { if (_routine == value) return; if (IndexChanged != null && Journey is { } journey && journey.Target != value?.Target) Journey = null; _routine = value; NotifyNeedsChanged(); }
    }
    [JsonRequired] public WorldPosition? Habitat { get; set; }
    [JsonRequired] public long ProvokedUntil { get; set; }
    [JsonRequired] public BodyState Body
    {
        get => _body;
        set { if (_body != null) _body.Changed -= NotifyNeedsChanged; _body = value; if (_body != null) _body.Changed += NotifyNeedsChanged; NotifyNeedsChanged(); }
    }
    public StateList<Preparation> Preparations
    {
        get => _preparations;
        set { if (_preparations != null) _preparations.Changed -= NotifyNeedsChanged; _preparations = value; if (_preparations != null) _preparations.Changed += NotifyNeedsChanged; NotifyNeedsChanged(); }
    }
    [JsonRequired] public StateList<ActorMemory> Memories
    {
        get => _memories;
        set { if (_memories != null) _memories.Changed -= NotifyNeedsChanged; _memories = value; if (_memories != null) _memories.Changed += NotifyNeedsChanged; NotifyNeedsChanged(); }
    }
    [JsonRequired] public Dictionary<long, int> Trust { get; set; } = [];
    public string VariantId { get; set; } = "";
    public long EncounterLeader { get; set; }
    [JsonRequired] public Dictionary<string, EquipmentState> Equipment { get; set; } = [];
    public string Archetype { get; set; } = "";
    public string TraitId { get => _traitId; set { if (_traitId == value) return; _traitId = value; NotifyNeedsChanged(); } }
    public int BaseHealth { get; set; }
    public int BaseMana { get; set; }
    public int BaseArmor { get; set; } = 1;
    public int BasePower { get; set; } = 6;
    public int BaseSpeed { get; set; } = 6;
    public string SpriteId { get; set; } = "";
    [JsonRequired]
    public long Id { get => _id; set { if (_id == value) return; _id = value; IndexChanged?.Invoke(this, _position); } }
    [JsonRequired]
    public string Name { get; set; } = "";
    [JsonRequired]
    public ActorKind Kind { get; set; }
    [JsonRequired]
    public WorldPosition Position
    {
        get => _position;
        set
        {
            if (_position == value) return; var before = _position; _position = value;
            if (IndexChanged != null && before.Distance(value) > 1 && _migration != null) { Migration = null; WildlifeAfter = 0; }
            if (IndexChanged != null) Journey = null;
            IndexChanged?.Invoke(this, before);
            if (_routine?.Kind is RoutineKind.Food or RoutineKind.Rest || _migration != null) NotifyNeedsChanged();
        }
    }
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
    public int Level { get => _level; set { if (_level == value) return; _level = value; NotifyNeedsChanged(); } }
    public string CombatTrait { get; set; } = "";
    public List<string> Talents { get; set; } = [];
    public string EncounterId { get; set; } = "";
    [JsonRequired]
    public int Experience { get; set; }
    [JsonRequired]
    public int Health { get; set; }
    [JsonRequired]
    public int Mana { get; set; }
    [JsonRequired]
    public int Stress { get; set; }
    [JsonRequired]
    public LifeState Life { get => _life; set { if (_life == value) return; _life = value; NotifyNeedsChanged(); } }
    [JsonRequired]
    public bool Recruited { get => _recruited; set { if (_recruited == value) return; _recruited = value; if (value && IndexChanged != null) Journey = null; NotifyNeedsChanged(); } }
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
    public int MaxHealth => (BaseHealth > 0 ? BaseHealth + (Level - 1) * 6 : Kind == ActorKind.Person ? new Hero { Role = Role, Level = Level }.MaxHealth : Kind == ActorKind.Beast ? 18 : 30) + Equipment.Values.Sum(e => e.Health);
    public int MaxMana => BaseHealth > 0 ? BaseMana + (Level - 1) * 3 : new Hero { Role = Role, Level = Level }.MaxMana;
    public string Sprite => SpriteId.Length > 0 ? SpriteId : Kind switch { ActorKind.Beast => "wolf", ActorKind.Raider => "bandit", ActorKind.Caravan => "merchant", _ => new Hero { Role = Role }.Sprite };
    public int Armor => BaseArmor + Equipment.Values.Sum(e => e.Armor);
    public int Power => Math.Max(1, BasePower + Equipment.Values.Sum(e => e.Power) - Body.Wounds / 2 - (Body.Fatigue >= 80 ? 2 : 0));
    public int Speed => Math.Max(1, BaseSpeed + Equipment.Values.Sum(e => e.Speed) - Body.Wounds / 2 - (Body.Fatigue >= 80 ? 2 : 0));
}
public sealed class BodyState
{
    private NeedTimeline _hunger, _fatigue;
    private Func<long>? _clock;
    private long _restUntil, _forageAfter;
    private double _forageProgress;
    internal event Action? Changed;
    private long Now => _clock?.Invoke() ?? 0;
    [JsonRequired] public double Hunger { get => _hunger.At(Now); set { _hunger.Set(Now, value); Changed?.Invoke(); } }
    [JsonRequired] public double Fatigue { get => _fatigue.At(Now); set { _fatigue.Set(Now, value); Changed?.Invoke(); } }
    [JsonRequired] public int Wounds { get; set; }
    [JsonRequired] public long RestUntil { get => _restUntil; set { if (_restUntil == value) return; _restUntil = value; Changed?.Invoke(); } }
    [JsonRequired] public long ForageAfter { get => _forageAfter; set { if (_forageAfter == value) return; _forageAfter = value; Changed?.Invoke(); } }
    [JsonRequired] public double ForageProgress { get => _forageProgress; set { if (_forageProgress == value) return; _forageProgress = value; Changed?.Invoke(); } }
    [JsonRequired] public double WorkDone { get; set; }
    internal void Bind(Func<long> clock, long tick)
    {
        double hunger = Hunger, fatigue = Fatigue;
        _clock = clock; _hunger.Set(tick, hunger); _fatigue.Set(tick, fatigue);
    }
    internal void Unbind() { _hunger.Freeze(Now); _fatigue.Freeze(Now); _clock = null; }
    internal void Reanchor(long before, long after) { _hunger.Reanchor(before, after); _fatigue.Reanchor(before, after); }
    internal double FatigueAt(long tick) => _fatigue.At(tick);
    internal void RatesAt(long tick, double hunger, double fatigue) { _hunger.RateAt(tick, hunger); _fatigue.RateAt(tick, fatigue); }
    internal int ThresholdIn(int limit, int mealAt, bool restThreshold = true)
    {
        double hunger = Hunger, fatigue = Fatigue;
        for (int i = 1; i < limit; i++)
        {
            hunger = Math.Clamp(hunger + _hunger.Rate, 0, 100); fatigue = Math.Clamp(fatigue + _fatigue.Rate, 0, 100);
            if (hunger >= mealAt || restThreshold && fatigue >= 75 && _fatigue.Rate > 0) return i;
        }
        return limit;
    }
}
public sealed record ActorMemory(string Key, string Text, long Tick, long Until, int Mood, long Other = 0);
public enum WorkKind { Build, Dig, Repair, Craft, Forage, Harvest, Camp, Discovery }
public enum SupplyPhase { Waiting, Carrying, Delivered }
public sealed class WorkSupply
{
    [JsonRequired] public string Stack { get; set; } = "";
    [JsonRequired] public int Count { get; set; }
    [JsonRequired] public WorldPosition? Storage { get; set; }
    [JsonRequired] public SupplyPhase Phase { get; set; }
    [JsonRequired] public long Carrier { get; set; }
}
public sealed class WorkOrder
{
    // Older orders grant their result directly; new ones retain real cargo until delivery.
    public int OutputVersion { get; set; }
    public WorkOutput? Output { get; set; }
    public WorldPosition? DeliveryStorage { get; set; }
    // Version zero is an older order whose reserved cost was already onsite.
    public int SupplyVersion { get; set; }
    public List<WorkSupply>? Supplies { get; set; }
    public string Method { get; set; } = "";
    [JsonRequired] public long Id { get; set; }
    [JsonRequired] public WorkKind Kind { get; set; }
    [JsonRequired] public string Definition { get; set; } = "";
    [JsonRequired] public WorldPosition Target { get; set; }
    [JsonRequired] public Material Before { get; set; }
    [JsonRequired] public Material After { get; set; }
    [JsonRequired] public double Required { get; set; }
    [JsonRequired] public double Progress { get; set; }
    [JsonRequired] public long Worker { get; set; }
    [JsonRequired] public Dictionary<string, int> Reserved { get; set; } = [];
    [JsonRequired] public Dictionary<string, int> Yield { get; set; } = [];
    [JsonRequired] public string Status { get; set; } = "Waiting";
    public bool PartyApproach { get; set; }
    public bool Suspended { get; set; }
    public WorldPosition? Station { get; set; }
    public bool PinnedWorker { get; set; }
    public int Priority { get; set; } = 1;
}
public sealed class WorkOutput
{
    [JsonRequired] public long Carrier { get; set; }
    [JsonRequired] public long ProducedTick { get; set; }
    [JsonRequired] public Dictionary<string, int> Remaining { get; set; } = [];
    [JsonRequired] public Dictionary<string, int> Delivered { get; set; } = [];
}
public sealed class BuiltStructure
{
    [JsonRequired] public WorldPosition Position { get; set; }
    [JsonRequired] public Material Material { get; set; }
    [JsonRequired] public Material Foundation { get; set; }
    [JsonRequired] public Dictionary<string, int> Cost { get; set; } = [];
    [JsonRequired] public double Condition { get; set; } = 100;
}

public sealed record GeneratedEquipment(string Base, string[] Affixes);
public sealed record EquipmentState(string Item, int Health, int Power, int Armor, int Speed);
public sealed class LootCache
{
    [JsonRequired] public long SourceActor { get; set; }
    public required string Id { get; set; }
    public required WorldPosition Position { get; set; }
    public required Dictionary<string, int> Contents { get; set; }
    public bool Taken { get; set; }
    public string[] Choices { get; set; } = [];
    public string? Selected { get; set; }
}
public sealed class WorldDiscovery
{
    [JsonRequired] public string Outcome { get; set; } = "";
    [JsonRequired] public bool Engaged { get; set; }
    [JsonRequired] public string Id { get; set; } = "";
    [JsonRequired] public string Definition { get; set; } = "";
    [JsonRequired] public string Name { get; set; } = "";
    [JsonRequired] public string Context { get; set; } = "";
    [JsonRequired] public WorldPosition Position { get; set; }
    [JsonRequired] public WorldPosition Anchor { get; set; }
    [JsonRequired] public int Rotation { get; set; }
    [JsonRequired] public int TownId { get; set; }
    [JsonRequired] public int SiteId { get; set; }
    [JsonRequired] public long WitnessId { get; set; }
    [JsonRequired] public bool Known { get; set; }
    [JsonRequired] public bool Visited { get; set; }
    [JsonRequired] public long[] Enemies { get; set; } = [];
}
public sealed record CombatantFeedbackState(int Health, int Mana, int Stress, bool Guarding, int Burn, int Bleed, int Mark, int Stun);
public sealed record CombatEvent(long Sequence, int Round, string Kind, long Source, long Target, string Label, int Amount, string Detail,
    CombatantFeedbackState? TargetState = null, string AbilityId = "");
public sealed record EncounterReward(int Gold, int Food, int Experience, string[] LevelUps)
{
    public string Theme { get; init; } = "";
    public EncounterRewardItem[] Items { get; init; } = [];
    public long[] Sources { get; init; } = [];
}
public sealed record EncounterRewardItem(string Item, int Count);
public sealed class EndgameState
{
    public required long ActorId { get; set; }
    public required int SiteId { get; set; }
    public bool Victory { get; set; }
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
public sealed record ResourceStock(string Definition, int Remaining, long RecoverAt);
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
    public Dictionary<int, ResourceStock> Resources { get; set; } = [];
}
public sealed class WorldState
{
    public Dictionary<int, HabitatState> Habitats { get; set; } = [];
    [JsonRequired] public List<EncounterPopulationRecord> EncounterPopulations { get; set; } = [];
    public HashSet<long> RewardedEncounterIds { get; set; } = [];
    [JsonRequired] public HashSet<ChunkKey> EcologyChunks { get; set; } = [];
    public CampStory? LastCampStory { get; set; }
    public Dictionary<string, GeneratedEquipment> GeneratedEquipment { get; set; } = [];
    [JsonRequired] public List<WorldDiscovery> Discoveries { get; set; } = [];
    [JsonRequired] public HashSet<int> KnownSites { get; set; } = [];
    [JsonRequired] public List<CombatEvent> CombatEvents { get; set; } = [];
    [JsonRequired] public long NextCombatEvent { get; set; } = 1;
    [JsonRequired] public EncounterReward? LastEncounterReward { get; set; }
    [JsonRequired] public List<WorkOrder> Work { get; set; } = [];
    [JsonRequired] public List<BuiltStructure> Structures { get; set; } = [];
    public WorldPosition? WorkRally { get; set; }
    [JsonRequired] public long NextWorkId { get; set; } = 1;
    [JsonRequired] public uint IncidentRandom { get; set; } = 1;
    [JsonRequired] public Dictionary<string, long> IncidentAfter { get; set; } = [];
    [JsonRequired] public EndgameState Endgame { get; set; } = null!;
    [JsonRequired] public List<LootCache> Loot { get; set; } = [];
    [JsonRequired]
    public long Tick { get; set; }
    [JsonRequired]
    public uint CombatRandom { get; set; }
    [JsonRequired]
    public int PartyOrdinal { get; set; }
    [JsonRequired]
    public long NextActorId { get; set; } = 100000;
    [JsonRequired]
    public ActorCollection Actors { get; set; } = [];
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
    public long ResourcesExported { get; set; }
    public long ResourcesImported { get; set; }
    public long NamedResourcesProduced { get; set; }
    public long RegionalResourcesProduced { get; set; }
}

public sealed class WorldBattle
{
    public Dictionary<int, PreparedAttack> Prepared { get; set; } = [];
    public Dictionary<int, string> LastEnemyActions { get; set; } = [];
    [JsonRequired] public HashSet<int> Assisting { get; set; } = [];
    public HashSet<int> ShieldUsed { get; set; } = [];
    [JsonRequired] public Dictionary<int, PlannedAbility> Plans { get; set; } = [];
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
public sealed record PlannedAbility(string Ability, int Target, bool Preparing = false, bool Interrupted = false);
public sealed record PreparedAttack(string Ability, int Target, int ReadyRound);

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
