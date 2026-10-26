namespace Roag.Core.Expeditions;

// A field session owns site/encounter state. Hero health is a command-boundary
// projection; the integrated campaign commits every change to its resident roster.
public sealed partial class Campaign
{
    public static Campaign CreateField(int seed, Zone zone)
    {
        var field = new Campaign(seed, false, true) { FieldMode = true, Gold = 0, Potions = 0, Torches = 0, Rations = 0 };
        field.Party.Clear();
        field.Journal.Clear();
        field.Zones.Add(zone);
        field.ZoneId = zone.Id;
        field.Position = field.PreviousPosition = zone.Objects.Single(o => o.Kind == ObjectKind.Return).Position;
        field.Reveal();
        return field;
    }

    public void EnterField(IEnumerable<Hero> party)
    {
        if (!FieldMode || Battle is not null) throw new InvalidOperationException("Site is not available.");
        Party = party.ToList();
        if (Party.Count is < 1 or > 4 || Party.Select(h => h.Id).Distinct().Count() != Party.Count)
            throw new ArgumentException("Choose one to four distinct residents.");
        Position = PreviousPosition = Zone.Objects.Single(o => o.Kind == ObjectKind.Return).Position;
        RetreatRequested = false;
        Reveal();
    }

    public void LeaveField()
    {
        Battle = null;
        Party.Clear();
        Potions = Rations = Torches = 0;
        RetreatRequested = false;
    }

    public void SetFieldSupplies(int medicine) => Potions = medicine;

    private (string Name, string Sprite, int Health, int Power, int Speed, int Armor)[] FieldEnemies(string id) =>
        id.EndsWith("-heavy", StringComparison.Ordinal)
            ? [("A still shape", "bandit", 48, 8, 5, 2), ("Interference", "wisp", 18, 4, 6, 0)]
            : Party.Count > 2
                ? [("Movement", "bandit", 30, 6, 6, 1), ("Interference", "wisp", 18, 4, 6, 0)]
                : [("Movement", "bandit", 28, 6, 6, 1)];
}
