using System.Text.Json;
using Ithica.Core.Colony;
using Field = Ithica.Core.Expeditions.Campaign;

namespace Ithica.Core;

public sealed partial class Campaign
{
    public static string DefaultSave => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ithica", "campaign.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private sealed class SiteData
    {
        public string Id { get; set; } = "";
        public int Seed { get; set; }
        public int GeneratorVersion { get; set; } = 2;
        public int MapCell { get; set; }
        public int? ContactId { get; set; }
        public bool Structure { get; set; }
        public bool Secured { get; set; }
        public JsonElement? Field { get; set; }
    }
    private sealed class SaveData
    {
        public string Format { get; set; } = "ithica";
        public int Version { get; set; } = 1;
        public JsonElement Home { get; set; }
        public CampaignClock Clock { get; set; } = new();
        public CampaignPhase Phase { get; set; }
        public string? SelectedSiteId { get; set; }
        public List<int> PartyIds { get; set; } = [];
        public ExpeditionPack Pack { get; set; } = new();
        public Stock Cargo { get; set; }
        public string ArrivalReport { get; set; } = "";
        public List<SiteData> Sites { get; set; } = [];
    }
    private SaveData Snapshot() => new()
    {
        Home = JsonSerializer.Deserialize<JsonElement>(Home.StateJson()), Clock = Clock, Phase = Phase, SelectedSiteId = SelectedSiteId,
        PartyIds = PartyIds, Pack = Pack, Cargo = Cargo, ArrivalReport = ArrivalReport,
        Sites = Sites.Select(s => new SiteData { Id = s.Id, Seed = s.Seed, GeneratorVersion = s.GeneratorVersion, MapCell = s.MapCell, ContactId = s.ContactId,
            Structure = s.Structure, Secured = s.Secured, Field = s.Session is null ? null : JsonSerializer.Deserialize<JsonElement>(s.Session.StateJson()) }).ToList()
    };

    public string StateJson() => JsonSerializer.Serialize(Snapshot(), JsonOptions);
    public void Save(string? path = null)
    {
        string target = Path.GetFullPath(path ?? DefaultSave);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, Snapshot(), JsonOptions);
                stream.Flush(true);
            }
            File.Move(temporary, target, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static Campaign Load(string? path = null) => FromJson(File.ReadAllText(path ?? DefaultSave));
    public static Campaign ImportColony(string path)
    {
        var home = Simulation.Load(path);
        if (home.Crew.Any(c => c.Location != ResidentLocation.Home || c.Expedition is not null))
            throw new InvalidDataException("Finish automated legacy rescues before importing this colony.");
        foreach (var c in home.Crew) c.FieldRole = c.Role switch { "Miner" => 1, "Botanist" => 2, "Medic" => 3, _ => 0 };
        return new(home);
    }

    public static Campaign FromJson(string json)
    {
        var d = JsonSerializer.Deserialize<SaveData>(json, JsonOptions) ?? throw new InvalidDataException("Empty Ithica save.");
        if (d.Format != "ithica" || d.Version != 1 || d.Home.ValueKind != JsonValueKind.Object || !Enum.IsDefined(d.Phase)
            || d.Clock is null || d.Clock.Speed is not (1 or 3 or 8) || !double.IsFinite(d.Clock.Accumulator) || d.Clock.Accumulator is < 0 or > 8
            || d.Phase != CampaignPhase.Colony && d.Clock.Accumulator != 0 || d.Pack is null || !d.Pack.Valid || !d.Cargo.Valid
            || d.PartyIds is null || d.PartyIds.Count > 4 || d.PartyIds.Distinct().Count() != d.PartyIds.Count || d.Sites is null
            || d.Sites.Count > 10000 || d.Sites.Any(s => s is null || string.IsNullOrWhiteSpace(s.Id)) || d.Sites.Select(s => s.Id).Distinct().Count() != d.Sites.Count)
            throw new InvalidDataException("Invalid Ithica campaign state.");
        var home = Simulation.FromJson(d.Home.GetRawText());
        var c = new Campaign(home) { Clock = d.Clock, Phase = d.Phase, SelectedSiteId = d.SelectedSiteId,
            PartyIds = d.PartyIds, Pack = d.Pack, Cargo = d.Cargo, ArrivalReport = d.ArrivalReport };
        c.Sites.Clear();
        foreach (var s in d.Sites)
        {
            if (s.GeneratorVersion is not (2 or 3) || !home.World.Valid(s.MapCell) || s.ContactId is int id && ((uint)id >= home.Survivors.Count || s.Structure))
                throw new InvalidDataException("Invalid site contact or position.");
            var field = s.Field is { ValueKind: JsonValueKind.Object } value ? Field.FromJson(value.GetRawText()) : null;
            if (field is not null && (!field.FieldMode || field.ZoneId != s.Id || field.Seed != s.Seed || field.Zone.Generation.Version != s.GeneratorVersion))
                throw new InvalidDataException("Invalid site session.");
            c.Sites.Add(new() { Id = s.Id, Seed = s.Seed, GeneratorVersion = s.GeneratorVersion, MapCell = s.MapCell, ContactId = s.ContactId, Structure = s.Structure, Secured = s.Secured, Session = field });
        }
        bool away = c.Phase == CampaignPhase.Expedition;
        if (away != (c.PartyIds.Count > 0) || c.PartyIds.Any(id => home.Crew.All(r => r.Id != id || !r.Alive))
            || c.Phase != CampaignPhase.Colony && c.SelectedSite?.Session is null
            || c.Phase == CampaignPhase.Colony && c.SelectedSiteId is not null)
            throw new InvalidDataException("Invalid phase or expedition roster.");
        foreach (var resident in home.Crew)
            if ((resident.Location == ResidentLocation.Field) != c.PartyIds.Contains(resident.Id)
                || resident.Location == ResidentLocation.Field && resident.SiteId != c.SelectedSiteId)
                throw new InvalidDataException("Resident location disagrees with expedition membership.");
        foreach (var s in c.Sites.Where(s => s.Session is not null))
        {
            var f = s.Session!;
            if (away && s == c.SelectedSite)
            {
                if (!f.Party.Select(h => h.Id).SequenceEqual(c.PartyIds) || f.Potions != c.Pack.Medicine)
                    throw new InvalidDataException("Invalid field projection.");
                foreach (var resident in c.Party)
                {
                    var h = f.Party.Single(h => h.Id == resident.Id);
                    var u = f.Battle?.Units.SingleOrDefault(u => u.HeroId == resident.Id);
                    if (h.Health != (int)Math.Ceiling(resident.Health) || h.Mana != resident.Charge || h.Level != resident.Level
                        || u is not null && (u.Health != h.Health || u.Mana != h.Mana))
                        throw new InvalidDataException("Resident and encounter projection disagree.");
                }
            }
            else if (f.Party.Count != 0 || f.Battle is not null) throw new InvalidDataException("Inactive site contains a party.");
        }
        return c;
    }

    public object Summary() => new { name = "Ithica", Home.Seed, CampaignTick, Phase, Objective, residents = Home.Crew.Count,
        rescued = Home.RescuedCount, Home.Resources, SupportUpgrade, sites = Sites.Select(s => new { s.Id, s.Name, s.Seed, s.GeneratorVersion, s.Structure, s.Secured, instantiated = s.Session is not null }),
        party = PartyIds, Pack, Cargo, home = Home.Summary() };
}
