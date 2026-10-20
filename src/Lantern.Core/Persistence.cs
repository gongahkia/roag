using System.Text.Json;

namespace Lantern.Core;

public sealed partial class Campaign
{
    public static string DefaultSave => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".regolith", "lantern.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private sealed class SaveData
    {
        public string Format { get; set; } = "lantern-rpg";
        public int Version { get; set; } = 1;
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
        public Battle? Battle
        {
            get; set;
        }
        public List<JournalEntry> Journal { get; set; } = [];
        public List<CombatEvent> CombatEvents { get; set; } = [];
    }
    private SaveData Snapshot() => new() { Seed = Seed, RandomState = Random.State, ZoneId = ZoneId, Position = Position, PreviousPosition = PreviousPosition, Gold = Gold, Potions = Potions, Torches = Torches, Rations = Rations, Light = Light, Steps = Steps, HasKey = HasKey, HasRelic = HasRelic, Victory = Victory, Sequence = _sequence, Party = Party, Completed = Completed, Seen = Zones.Select(z => z.Seen).ToList(), Battle = Battle, Journal = Journal, CombatEvents = CombatEvents };
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
        if (d.Format != "lantern-rpg" || d.Version != 1 || d.Party is null || d.Party.Count != 4 || d.Completed is null || d.Seen is null || d.Seen.Count != 3 || d.Gold < 0 || d.Potions < 0 || d.Torches < 0 || d.Rations < 0 || d.Light is < 0 or > 100 || d.Steps < 0 || d.RandomState == 0 || d.Journal is null || d.CombatEvents is null)
        {
            throw new InvalidDataException("Invalid RPG save. Colony saves use a different format.");
        }

        var c = new Campaign(d.Seed);
        if (!c.Zones.Any(z => z.Id == d.ZoneId))
        {
            throw new InvalidDataException("Unknown RPG area.");
        }

        for (int i = 0; i < 4; i++)
        {
            var h = d.Party[i];
            if (h is null || h.Id != i || h.Role != i || h.Level is < 1 or > 100 || h.Experience < 0 || h.Health < 0 || h.Health > h.MaxHealth || h.Mana < 0 || h.Mana > h.MaxMana || h.Stress is < 0 or > 100)
            {
                throw new InvalidDataException("Invalid party member.");
            }
        }
        for (int i = 0; i < 3; i++)
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
            if (b.Units is null || b.Units.Count is < 5 or > 12 || b.Order is null || b.Order.Count == 0 || b.TurnIndex < 0 || b.TurnIndex >= b.Order.Count || b.Order.Distinct().Count() != b.Order.Count || b.Order.Any(i => i < 0 || i >= b.Units.Count) || b.Round < 1 || !c.Zone.Objects.Any(o => o.Id == b.EncounterId && o.Kind == ObjectKind.Encounter))
            {
                throw new InvalidDataException("Invalid encounter.");
            }

            for (int i = 0; i < b.Units.Count; i++)
            {
                var u = b.Units[i];
                if (u is null || u.Id != i || !c.Zone.Walkable(u.Position) || u.Health < 0 || u.MaxHealth <= 0 || u.Health > u.MaxHealth || u.Mana < 0 || u.Mana > u.MaxMana || u.HeroId is < -1 or > 3 || u.Stress is < 0 or > 100 || u.IntentTarget < 0 || u.IntentTarget >= b.Units.Count)
                {
                    throw new InvalidDataException("Invalid battle unit.");
                }
            }
            if (b.Units.Take(4).Where((u, i) => u.HeroId != i).Any() || b.Units.Skip(4).Any(u => u.Friendly) || !b.Active.Alive)
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
    public object Summary() => new { Seed, Zone = ZoneId, Position, Gold, Potions, Torches, Rations, Light, Steps, HasKey, HasRelic, Victory, Party, Completed = Completed.Order().ToArray(), Encounter = Battle?.Name, Objective, Journal = Journal.TakeLast(5) };
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
