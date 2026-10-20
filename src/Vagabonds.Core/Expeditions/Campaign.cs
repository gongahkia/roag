namespace Vagabonds.Core.Expeditions;

public sealed partial class Campaign
{
    public bool FieldMode { get; private set; }
    public bool RetreatRequested { get; private set; }
    public int Seed
    {
        get; private set;
    }
    public List<Zone> Zones
    {
        get;
    }
    public string ZoneId { get; private set; } = "hearth";
    public Zone Zone => Zones.Single(z => z.Id == ZoneId);
    public Cell Position { get; private set; } = new(18, 16);
    public Cell PreviousPosition { get; private set; } = new(18, 17);
    public List<Hero> Party { get; private set; } = [];
    public HashSet<string> Completed { get; private set; } = [];
    public int Gold { get; private set; } = 36;
    public int Potions { get; private set; } = 5;
    public int Torches { get; private set; } = 4;
    public int Rations { get; private set; } = 6;
    public int Light { get; private set; } = 100;
    public int Steps
    {
        get; private set;
    }
    public bool HasKey
    {
        get; private set;
    }
    public bool HasRelic
    {
        get; private set;
    }
    public bool Victory
    {
        get; private set;
    }
    public Battle? Battle
    {
        get; private set;
    }
    public RandomStream Random
    {
        get;
    }
    public List<JournalEntry> Journal { get; private set; } = [];
    public List<CombatEvent> CombatEvents { get; private set; } = [];
    private int _sequence;
    public string Objective => Victory ? "The ember has a home. Explore, recover, and inspect the art." : HasRelic ? "Bring the ember relic to Warden Elowen in Hearth." : Completed.Contains("iron-gate") ? "Defeat the Hollow Abbot and recover the ember relic." : HasKey ? "Take the iron key to the sealed chamber beneath the abbey." : "Find the iron key in Ashwood's abandoned chest.";
    public Campaign(int seed = 42) : this(seed, false) { }
    private Campaign(int seed, bool legacy, bool restoreMaps = false)
    {
        Seed = seed;
        Random = new(unchecked((uint)seed));
        Zones = restoreMaps ? [] : legacy ? LegacyWorldBuilder.Create(seed) : WorldBuilder.Create(seed);
        if (legacy)
            foreach (var zone in Zones)
            {
                zone.Generation = new() { Version = 1, Algorithm = "Authored version-one map" };
                Array.Fill(zone.Safe, true);
            }
        string[] names = ["Rowan", "Vesper", "Iris", "Sable"];
        for (int i = 0; i < 4; i++)
        {
            var hero = new Hero { Id = i, Name = names[i], Role = i };
            hero.Health = hero.MaxHealth;
            hero.Mana = hero.MaxMana;
            Party.Add(hero);
        }
        Note("Four strangers share one lantern. Find the ember beneath the abbey, and bring it home.");
        if (!restoreMaps) Reveal();
    }
    private void Note(string text)
    {
        Journal.Add(new(++_sequence, text));
        if (Journal.Count > 100)
        {
            Journal.RemoveAt(0);
        }
    }
    private ActionResult Result(bool ok, string text, string? title = null)
    {
        if (ok)
        {
            Note(text);
        }

        return new(ok, text, title);
    }
    public bool Present(WorldObject o) => o.Kind switch { ObjectKind.Encounter or ObjectKind.Chest or ObjectKind.Relic or ObjectKind.Rescue or ObjectKind.Salvage => !Completed.Contains(o.Id), _ => true };
    public WorldObject? ObjectAt(Cell cell) => Zone.Objects.FirstOrDefault(o => o.Position == cell && Present(o));
    public bool Walkable(Cell p) => Zone.Walkable(p) && !(ObjectAt(p) is { } o && o.Blocks && !(o.Kind == ObjectKind.Gate && Completed.Contains(o.Id)));
    public void Reveal() => Zone.Reveal(Position, ZoneId == "hearth" ? 60 : ZoneId == "wilds" ? 10 : Light > 60 ? 10 : Light > 20 ? 8 : 5);
    public ActionResult Move(int dx, int dy)
    {
        if (Battle is not null)
        {
            return new(false, "Choose a battle action first.");
        }

        if (Math.Abs(dx) + Math.Abs(dy) != 1)
        {
            return new(false, "Move one tile at a time.");
        }

        var next = Position + new Cell(dx, dy);
        if (!Walkable(next))
        {
            if (ObjectAt(next) is { } blocked && blocked.Kind != ObjectKind.Scenery)
            {
                return Interact(blocked.Id);
            }

            return new(false, "The way is blocked.");
        }
        PreviousPosition = Position;
        Position = next;
        AdvanceExploration();
        Reveal();
        if (ObjectAt(next) is { } o)
        {
            if (o.Kind == ObjectKind.Encounter)
            {
                StartEncounter(o);
                return new(true, "Ambush / " + o.Name);
            }
            if (o.Kind == ObjectKind.Portal)
            {
                return Interact(o.Id);
            }
        }
        return new(true, "");
    }
    private void AdvanceExploration()
    {
        Steps++;
        if (FieldMode) return;
        if (ZoneId != "hearth")
        {
            if (Steps % 4 == 0)
            {
                Light = Math.Max(0, Light - 1);
            }

            if (Light < 25 && Steps % 12 == 0)
            {
                foreach (var h in Party)
                {
                    h.Stress = Math.Min(100, h.Stress + 2);
                }
            }
        }
        else
        {
            Light = 100;
        }

        if (ZoneId != "hearth" && Zone.Generation.Version > 1)
        {
            Zone.Environment.Advance(Zone, Seed);
            var hazard = Zone.Environment.Cells[Zone.Index(Position)];
            if (hazard != HazardKind.None)
            {
                foreach (var h in Party.Where(h => h.Health > 0))
                {
                    h.Stress = Math.Min(100, h.Stress + (hazard == HazardKind.Corruption ? 3 : 1));
                    if (hazard != HazardKind.Corruption) h.Health = Math.Max(1, h.Health - 1);
                }
                Note(hazard switch
                {
                    HazardKind.Fire => "Scorching ground: each conscious companion loses 1 health and gains 1 stress. T clears nearby hazards.",
                    HazardKind.Spores => "Bitter spores: each conscious companion loses 1 health and gains 1 stress. T clears nearby hazards.",
                    _ => "The blight whispers: each conscious companion gains 3 stress. T clears nearby hazards."
                });
            }
        }
    }
    public ActionResult Wait()
    {
        if (Battle is not null) return new(false, "Choose a battle action first.");
        AdvanceExploration();
        Reveal();
        return new(true, "You wait and watch the shadows.");
    }
    public List<Cell> Route(Cell destination) => FindRoute(destination, discoveredOnly: true);
    private List<Cell> FindRoute(Cell destination, bool discoveredOnly)
    {
        if (!Zone.Inside(destination) || discoveredOnly && !Zone.Seen[Zone.Index(destination)]) return [];
        var o = ObjectAt(destination);
        IEnumerable<Cell> goals = o is { Blocks: true } && !Walkable(destination) ? Zone.Neighbors(destination).Where(Walkable) : [destination];
        bool Allowed(Cell p) => (!discoveredOnly || Zone.Seen[Zone.Index(p)]) && Walkable(p)
            && (o?.Position == p || ObjectAt(p)?.Kind is not (ObjectKind.Encounter or ObjectKind.Portal));
        var path = Zone.FindPath(Position, goals, p => Allowed(p) && (p == destination
            || discoveredOnly && !Zone.Visible[Zone.Index(p)] || Zone.Environment.Cells[Zone.Index(p)] == HazardKind.None));
        return path.Count > 0 ? path : Zone.FindPath(Position, goals, Allowed);
    }
    public ActionResult Interact(string? id = null)
    {
        if (Battle is not null)
        {
            return new(false, "Finish the encounter first.");
        }

        var o = id is null ? Zone.Objects.Where(Present).Where(o => o.Kind != ObjectKind.Scenery && o.Position.Distance(Position) <= 1).OrderBy(o => o.Position.Distance(Position)).FirstOrDefault() : Zone.Objects.FirstOrDefault(o => o.Id == id && Present(o));
        if (o is null || o.Position.Distance(Position) > 1)
        {
            return new(false, "Walk next to an object, then press E or click it.");
        }

        switch (o.Kind)
        {
            case ObjectKind.Portal:
                ZoneId = o.Destination;
                Position = PreviousPosition = o.Arrival;
                Reveal();
                return Result(true, "Entered " + Zone.Name + ".");
            case ObjectKind.Encounter:
                StartEncounter(o);
                return new(true, "Encounter / " + o.Name);
            case ObjectKind.Gate:
                if (Completed.Contains(o.Id))
                {
                    return new(true, "The seal is open.");
                }

                if (!HasKey)
                {
                    return new(false, "An iron key is needed. Search the chest in Ashwood.", "THE IRON SEAL");
                }

                Completed.Add(o.Id);
                HasKey = false;
                return Result(true, "The iron key turns. Beyond the seal, a bell begins to toll.", "THE SEAL OPENS");
            case ObjectKind.Inn:
                if (Gold < 12)
                {
                    return new(false, "A room costs 12 gold. You can still camp with rations.", "THE COPPER KETTLE");
                }

                Gold -= 12;
                foreach (var h in Party)
                {
                    h.Health = h.MaxHealth;
                    h.Mana = h.MaxMana;
                    h.Stress = 0;
                }
                Light = 100;
                return Result(true, "Warm beds, clean bandages, and a quiet night. The party is fully restored.", "THE COPPER KETTLE");
            case ObjectKind.Merchant:
                if (Gold < 10)
                {
                    return new(false, "An expedition kit costs 10 gold.", "THE PROVISIONER");
                }

                Gold -= 10;
                Potions += 2;
                Torches += 2;
                Rations += 2;
                return Result(true, "Bought 2 potions, 2 torches, and 2 rations for 10 gold.", "READY FOR THE ROAD");
            case ObjectKind.Talk:
                if (o.Id == "elder" && HasRelic && !Victory)
                {
                    Victory = true;
                    Gold += 80;
                    return Result(true, "Elowen cups the ember in both hands. For the first time in years, Hearth's bell rings above ground. +80 gold. The expedition is complete; you may keep exploring.", "A LIGHT BROUGHT HOME");
                }
                return new(true, o.Detail, o.Name.ToUpperInvariant());
            case ObjectKind.Chest:
                Completed.Add(o.Id);
                if (o.Id == "iron-cache")
                {
                    HasKey = true;
                    Gold += 18;
                    Potions++;
                }
                else
                {
                    Gold += 24;
                    Potions += 2;
                    Torches++;
                }
                return Result(true, o.Detail, "A CACHE IN THE DARK");
            case ObjectKind.Shrine:
                if (!Completed.Add(o.Id))
                {
                    return new(false, "The shrine has given all it can.");
                }

                foreach (var h in Party)
                {
                    h.Mana = Math.Min(h.MaxMana, h.Mana + 8);
                    h.Stress = Math.Max(0, h.Stress - 15);
                }
                return Result(true, "The Wayfarer's light restores 8 mana and 15 composure to each companion.", "A MOMENT OF GRACE");
            case ObjectKind.Camp:
                return Camp();
            case ObjectKind.Relic:
                if (!Completed.Contains("abbey-abbot"))
                {
                    return new(false, "The Hollow Abbot still guards this ember.");
                }

                HasRelic = true;
                Completed.Add(o.Id);
                return Result(true, "A living ember rests in your lantern. Bring it to Warden Elowen in Hearth.", "THE EMBER RELIC");
            default:
                return new(true, o.Name);
        }
    }
    public ActionResult Camp()
    {
        if (Battle is not null)
        {
            return new(false, "You cannot camp during combat.");
        }

        if (ZoneId != "hearth" && !Zone.Objects.Any(o => o.Kind == ObjectKind.Camp && o.Position.Distance(Position) <= 1))
        {
            return new(false, "Find a marked campsite before resting.");
        }

        if (Rations < 2)
        {
            return new(false, "Camping needs 2 shared rations.");
        }

        Rations -= 2;
        Light = Math.Min(100, Light + 20);
        foreach (var h in Party)
        {
            h.Health = Math.Min(h.MaxHealth, h.Health + 18);
            h.Mana = Math.Min(h.MaxMana, h.Mana + 6);
            h.Stress = Math.Max(0, h.Stress - 12);
        }
        Reveal();
        return Result(true, "Two rations shared. Everyone recovers 18 health, 6 mana, and 12 composure.", "BENEATH ONE LANTERN");
    }
    public ActionResult UseTorch()
    {
        if (Battle is not null)
        {
            return new(false, "Relight the lantern outside combat.");
        }

        var rules = GenerationRules.Default.Hazards;
        bool hazardNearby = Zone.Environment.Nearby(Zone, Position, rules.TorchRadius);
        if (Torches <= 0 || Light == 100 && !hazardNearby)
        {
            return new(false, Torches <= 0 ? "No torches left." : "The lantern is already bright.");
        }

        Torches--;
        int cleared = Zone.Environment.ClearNear(Zone, Position, rules.TorchRadius);
        Light = Math.Min(100, Light + 45);
        Reveal();
        return Result(true, cleared > 0 ? $"A fresh torch drives back {cleared} hazard tiles and restores 45 light." : "A fresh torch. The shadows recede.");
    }
    public ActionResult UsePotion(int heroId)
    {
        if (Battle is not null)
        {
            return new(false, "Use the potion action during battle.");
        }

        if ((uint)heroId >= Party.Count || Potions <= 0)
        {
            return new(false, "No potion available.");
        }

        var h = Party[heroId];
        if (h.Health == h.MaxHealth)
        {
            return new(false, h.Name + " is already healthy.");
        }

        Potions--;
        h.Health = Math.Min(h.MaxHealth, h.Health + 24);
        return Result(true, h.Name + " drinks a potion. +24 health.");
    }
}
