using System.Text.Json;

namespace Ithica.Core.Expeditions;

public sealed partial class Campaign
{
    public static string DefaultSave => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".regolith", "lantern.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private sealed class SaveData
    {
        public string Format { get; set; } = "lantern-rpg";
        public int Version { get; set; } = 2;
        public bool RetreatRequested { get; set; }
        public int Seed
        {
            get; set;
        }
        public uint RandomState
        {
            get; set;
        }
        public string ZoneId { get; set; } = "";
        public Cell Position
        {
            get; set;
        }
        public Cell PreviousPosition
        {
            get; set;
        }
        public int Gold
        {
            get; set;
        }
        public int Potions
        {
            get; set;
        }
        public int Torches
        {
            get; set;
        }
        public int Rations
        {
            get; set;
        }
        public int Light
        {
            get; set;
        }
        public int Steps
        {
            get; set;
        }
        public bool HasKey
        {
            get; set;
        }
        public bool HasRelic
        {
            get; set;
        }
        public bool Victory
        {
            get; set;
        }
        public int Sequence
        {
            get; set;
        }
        public List<Hero> Party { get; set; } = [];
        public HashSet<string> Completed { get; set; } = [];
        public List<bool[]> Seen { get; set; } = [];
        public List<ZoneData>? Zones { get; set; }
        public Battle? Battle
        {
            get; set;
        }
        public List<JournalEntry> Journal { get; set; } = [];
        public List<CombatEvent> CombatEvents { get; set; } = [];
    }
    private sealed class ZoneData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public Ground[] Tiles { get; set; } = [];
        public string?[] GroundSprites { get; set; } = [];
        public bool[] Safe { get; set; } = [];
        public List<WorldObject> Objects { get; set; } = [];
        public GenerationReport Generation { get; set; } = new();
        public HazardKind[] Hazards { get; set; } = [];
        public HazardKind[] Sources { get; set; } = [];
        public byte[] Age { get; set; } = [];
        public byte[] Fuel { get; set; } = [];
        public byte[] Suppressed { get; set; } = [];
        public int Turns { get; set; }
        public int Ticks { get; set; }
        public static ZoneData From(Zone z) => new()
        {
            Id = z.Id, Name = z.Name, Subtitle = z.Subtitle, Width = z.Width, Height = z.Height,
            Tiles = z.Tiles, GroundSprites = z.GroundSprites, Safe = z.Safe, Objects = z.Objects, Generation = z.Generation,
            Hazards = z.Environment.Cells, Sources = z.Environment.Sources, Age = z.Environment.Age,
            Fuel = z.Environment.Fuel, Suppressed = z.Environment.Suppressed, Turns = z.Environment.Turns, Ticks = z.Environment.Ticks
        };
        public Zone Restore()
        {
            long count = Width * (long)Height;
            var allowedSprites = GenerationRules.Default.SpriteAliases.ToHashSet(StringComparer.Ordinal);
            if (Width is < 8 or > 256 || Height is < 8 or > 256 || Tiles is null || Tiles.Length != count
                || GroundSprites is null || GroundSprites.Length != count || Safe is null || Safe.Length != count
                || Hazards is null || Hazards.Length != count || Sources is null || Sources.Length != count
                || Age is null || Age.Length != count || Fuel is null || Fuel.Length != count || Suppressed is null || Suppressed.Length != count
                || Objects is null || Objects.Count > count || Objects.Any(o => o is null || !Enum.IsDefined(o.Kind)) || Generation is null
                || Tiles.Any(g => !Enum.IsDefined(g)) || Hazards.Any(h => !Enum.IsDefined(h)) || Sources.Any(h => !Enum.IsDefined(h))
                || GroundSprites.Any(s => s is not null && !allowedSprites.Contains(s))
                || Turns < 0 || Ticks < 0 || Ticks > Turns / GenerationRules.Default.Hazards.StepsPerTick)
                throw new InvalidDataException("Invalid generated map or environment data.");
            var z = new Zone(Id, Name, Subtitle, Width, Height, Ground.Void) { Generation = Generation };
            Array.Copy(Tiles, z.Tiles, Tiles.Length);
            Array.Copy(GroundSprites, z.GroundSprites, Tiles.Length);
            Array.Copy(Safe, z.Safe, Tiles.Length);
            z.Objects.AddRange(Objects);
            Array.Copy(Hazards, z.Environment.Cells, Tiles.Length);
            Array.Copy(Sources, z.Environment.Sources, Tiles.Length);
            Array.Copy(Age, z.Environment.Age, Tiles.Length);
            Array.Copy(Fuel, z.Environment.Fuel, Tiles.Length);
            Array.Copy(Suppressed, z.Environment.Suppressed, Tiles.Length);
            z.Environment.Turns = Turns;
            z.Environment.Ticks = Ticks;
            if (Enumerable.Range(0, Tiles.Length).Any(i => Safe[i] && (Hazards[i] != HazardKind.None || Sources[i] != HazardKind.None)))
                throw new InvalidDataException("Hazard on a protected route.");
            return z;
        }
    }
    private SaveData Snapshot() => new() { Format = FieldMode ? "ithica-field" : "lantern-rpg", Version = FieldMode ? 1 : 2, RetreatRequested = RetreatRequested, Seed = Seed, RandomState = Random.State, ZoneId = ZoneId, Position = Position, PreviousPosition = PreviousPosition, Gold = Gold, Potions = Potions, Torches = Torches, Rations = Rations, Light = Light, Steps = Steps, HasKey = HasKey, HasRelic = HasRelic, Victory = Victory, Sequence = _sequence, Party = Party, Completed = Completed, Seen = Zones.Select(z => z.Seen).ToList(), Zones = Zones.Select(ZoneData.From).ToList(), Battle = Battle, Journal = Journal, CombatEvents = CombatEvents };
    public string StateJson() => JsonSerializer.Serialize(Snapshot(), JsonOptions);
    public void Save(string? path = null)
    {
        string destination = Path.GetFullPath(path ?? DefaultSave);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write))
            {
                JsonSerializer.Serialize(stream, Snapshot(), JsonOptions);
                stream.Flush(true);
            }
            File.Move(temp, destination, true);
        }
        finally
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
    }
    public static Campaign Load(string? path = null) => FromJson(File.ReadAllText(path ?? DefaultSave));
    public static Campaign FromJson(string json)
    {
        var d = JsonSerializer.Deserialize<SaveData>(json, JsonOptions) ?? throw new InvalidDataException("Empty RPG save.");
        bool field = d.Format == "ithica-field" && d.Version == 1;
        if ((!field && (d.Format != "lantern-rpg" || d.Version is not (1 or 2))) || d.Party is null || (field ? d.Party.Count > 4 : d.Party.Count != 4) || d.Completed is null || d.Seen is null || (field ? d.Seen.Count != 1 : d.Seen.Count != 3) || d.Gold < 0 || d.Potions < 0 || d.Torches < 0 || d.Rations < 0 || d.Light is < 0 or > 100 || d.Steps < 0 || d.RandomState == 0 || d.Journal is null || d.CombatEvents is null)
        {
            throw new InvalidDataException("Invalid RPG save. Colony saves use a different format.");
        }

        var c = new Campaign(d.Seed, !field && d.Version == 1, restoreMaps: field || d.Version == 2) { FieldMode = field, RetreatRequested = d.RetreatRequested };
        if (field || d.Version == 2)
        {
            if (d.Zones is null || (field ? d.Zones.Count != 1 : d.Zones.Count != 3) || d.Zones.Any(z => z is null)
                || !field && !d.Zones.Select(z => z.Id).SequenceEqual(new[] { "hearth", "wilds", "crypt" }))
                throw new InvalidDataException("Missing generated maps.");
            c.Zones.Clear();
            c.Zones.AddRange(d.Zones.Select(z => z.Restore()));
            var validation = field ? Generation.LunarGenerator.Validate(c.Zones[0]) : WorldBuilder.Validate(c.Zones);
            if (!validation.Valid) throw new InvalidDataException("Invalid saved quest: " + string.Join("; ", validation.Errors));
        }
        if (!c.Zones.Any(z => z.Id == d.ZoneId))
        {
            throw new InvalidDataException("Unknown RPG area.");
        }

        for (int i = 0; i < d.Party.Count; i++)
        {
            var h = d.Party[i];
            if (h is null || (field ? !h.Resident || h.Id < 0 || h.Role is < 0 or > 3 : h.Id != i || h.Role != i || h.Resident) || h.Level is < 1 or > 100 || h.Experience < 0 || h.Health < 0 || h.Health > h.MaxHealth || h.Mana < 0 || h.Mana > h.MaxMana || h.Stress is < 0 or > 100)
            {
                throw new InvalidDataException("Invalid party member.");
            }
        }
        if (d.Party.Select(h => h.Id).Distinct().Count() != d.Party.Count) throw new InvalidDataException("Duplicate resident identity.");
        for (int i = 0; i < d.Seen.Count; i++)
        {
            if (d.Seen[i] is null || d.Seen[i].Length != c.Zones[i].Tiles.Length)
            {
                throw new InvalidDataException("Invalid exploration data.");
            }

            c.Zones[i].Seen = d.Seen[i];
        }
        c.ZoneId = d.ZoneId;
        if (!c.Zone.Walkable(d.Position) || !c.Zone.Walkable(d.PreviousPosition))
        {
            throw new InvalidDataException("Invalid party position.");
        }

        if (d.Battle is { } b)
        {
            if (b.Units is null || b.Units.Count < d.Party.Count + 1 || b.Units.Count > 12 || b.Order is null || b.Order.Count == 0 || b.TurnIndex < 0 || b.TurnIndex >= b.Order.Count || b.Order.Distinct().Count() != b.Order.Count || b.Order.Any(i => i < 0 || i >= b.Units.Count) || b.Round < 1 || !c.Zone.Objects.Any(o => o.Id == b.EncounterId && o.Kind == ObjectKind.Encounter))
            {
                throw new InvalidDataException("Invalid encounter.");
            }

            for (int i = 0; i < b.Units.Count; i++)
            {
                var u = b.Units[i];
                if (u is null || u.Id != i || !c.Zone.Walkable(u.Position) || u.Health < 0 || u.MaxHealth <= 0 || u.Health > u.MaxHealth || u.Mana < 0 || u.Mana > u.MaxMana || (u.HeroId < -1 || (u.Friendly && !d.Party.Any(h => h.Id == u.HeroId))) || u.Stress is < 0 or > 100 || u.IntentTarget < 0 || u.IntentTarget >= b.Units.Count)
                {
                    throw new InvalidDataException("Invalid battle unit.");
                }
            }
            if (b.Units.Take(d.Party.Count).Where((u, i) => u.HeroId != d.Party[i].Id).Any() || b.Units.Skip(d.Party.Count).Any(u => u.Friendly) || !b.Active.Alive)
            {
                throw new InvalidDataException("Invalid battle turn.");
            }
        }
        c.Random.State = d.RandomState;
        c.Position = d.Position;
        c.PreviousPosition = d.PreviousPosition;
        c.Gold = d.Gold;
        c.Potions = d.Potions;
        c.Torches = d.Torches;
        c.Rations = d.Rations;
        c.Light = d.Light;
        c.Steps = d.Steps;
        c.HasKey = d.HasKey;
        c.HasRelic = d.HasRelic;
        c.Victory = d.Victory;
        c._sequence = d.Sequence;
        c.Party = d.Party;
        c.Completed = d.Completed;
        c.Battle = d.Battle;
        c.Journal = d.Journal;
        c.CombatEvents = d.CombatEvents;
        c.Reveal();
        return c;
    }
    public object GenerationSummary() => new { Seed, Validation = FieldMode ? Generation.LunarGenerator.Validate(Zone) : WorldBuilder.Validate(Zones), Zones = Zones.Select(z => new { z.Id, z.Width, z.Height, z.Generation, Terrain = z.Tiles.GroupBy(g => g).ToDictionary(g => g.Key.ToString(), g => g.Count()), HazardTiles = z.Environment.Cells.Count(h => h != HazardKind.None), z.Environment.Turns, z.Environment.Ticks }) };
    public object Summary() => new { Seed, Zone = ZoneId, Position, Gold, Potions, Torches, Rations, Light, Steps, HasKey, HasRelic, Victory, Party, Completed = Completed.Order().ToArray(), Encounter = Battle?.Name, Objective, Generation = GenerationSummary(), Journal = Journal.TakeLast(5) };
    public ActionResult TravelTo(string objectId)
    {
        var o = Zone.Objects.FirstOrDefault(o => o.Id == objectId && Present(o));
        if (o is null)
        {
            return new(false, "No such object in this area.");
        }

        var route = Route(o.Position);
        if (route.Count == 0 && Position.Distance(o.Position) > 1)
        {
            return new(false, "No route to " + o.Name);
        }

        string zone = ZoneId;
        foreach (var step in route)
        {
            var result = Move(step.X - Position.X, step.Y - Position.Y);
            if (!result.Success)
            {
                return result;
            }

            if (Battle is not null || ZoneId != zone)
            {
                return result;
            }
        }
        return o.Kind == ObjectKind.Encounter && Completed.Contains(o.Id) ? new(true, "Encounter cleared.") : Interact(objectId);
    }
    public void RunDemoExpedition()
    {
        void Go(string id)
        {
            if (Light < 65 && Torches > 0) UseTorch();
            foreach (var h in Party.Where(h => h.Health < h.MaxHealth / 2))
                if (Potions > 0) UsePotion(h.Id);
            var result = TravelTo(id);
            if (!result.Success)
            {
                throw new InvalidOperationException(id + ": " + result.Message);
            }

            if (Battle is not null)
            {
                AutoBattle();
            }
        }
        Go("to-wilds");
        Go("wilds-hounds");
        Go("iron-cache");
        Go("forest-shrine");
        Go("wilds-raiders");
        Go("wood-camp");
        Go("to-crypt");
        Go("abbey-hollows");
        Go("abbey-chest");
        Go("crypt-camp");
        Go("abbey-slimes");
        Go("crypt-camp");
        Go("iron-gate");
        Go("abbey-abbot");
        Go("relic");
        Go("to-forest");
        Go("to-town");
        Go("elder");
        if (!Victory)
        {
            throw new InvalidOperationException("Demo did not return the ember to Hearth.");
        }
    }
}
