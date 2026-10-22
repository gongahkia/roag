using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Expeditions;
using Vagabonds.Core.Content;

namespace Vagabonds.Core.World;

public static class SessionStore
{
    public static string Format(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (doc.RootElement.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Save must be an object.");
        return doc.RootElement.TryGetProperty("format", out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
    }
    public static IGameSession Load(string path) => Format(path) == "vagabonds-world" ? WorldSession.Load(path) : new LegacyCampaignSession(Campaign.LoadRpg(path));
}

public sealed partial class WorldSession
{
    private sealed record ChunkReference(ChunkKey Key, string Hash);
    private sealed record Manifest(string Format, int Version, string ContentHash, WorldPlan Plan, JsonObject State, List<ChunkReference> Chunks);
    private static readonly JsonSerializerOptions SaveOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = false, RespectRequiredConstructorParameters = true };
    public void Save(string path)
    {
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string directory = path + ".chunks"; Directory.CreateDirectory(directory);
        var refs = new List<ChunkReference>();
        foreach (var delta in State.Chunks)
        {
            byte[] data = JsonSerializer.SerializeToUtf8Bytes(delta, SaveOptions); string hash = Convert.ToHexString(SHA256.HashData(data));
            string file = Path.Combine(directory, hash + ".json");
            if (!File.Exists(file)) WriteAtomic(file, data);
            refs.Add(new(delta.Key, hash));
        }
        var state = JsonSerializer.SerializeToNode(State, SaveOptions)!.AsObject(); state.Remove("chunks");
        WriteAtomic(path, JsonSerializer.SerializeToUtf8Bytes(new Manifest("vagabonds-world", 3, Content.Hash, Plan, state, refs), SaveOptions));
        SavePath = path; DeathPending = false;
    }
    private static void WriteAtomic(string path, byte[] data)
    {
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { stream.Write(data); stream.Flush(true); }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static WorldSession Load(string path)
    {
        var save = JsonSerializer.Deserialize<Manifest>(File.ReadAllBytes(path), SaveOptions) ?? throw new InvalidDataException("Empty world save.");
        if (save.Format != "vagabonds-world" || save.Version != 3 || save.Plan == null || save.State == null || save.Chunks == null) throw new InvalidDataException("Unsupported world save.");
        if (save.ContentHash != ContentCatalog.Default.Hash) throw new InvalidDataException("World content differs from this build. Start an explicit new run; the selected save is preserved.");
        var plan = save.Plan;
        if (plan.Version != WorldPlan.GenerationVersion || plan.SampleHash != SampleLibrary.Default.Hash) throw new InvalidDataException("World generation version/sample manifest differs from this build.");
        if (plan.Validate().Count != 0 || plan.Towns.Select(t => t.Id).Where((id, index) => id != index).Any() || plan.Sites.Select(t => t.Id).Where((id, index) => id != index).Any() || plan.Roads.Select(t => t.Id).Where((id, index) => id != index).Any() || plan.Roads.Any(r => r.FromTown is < 0 or >= 24 || r.ToTown is < 0 or >= 24)) throw new InvalidDataException("Invalid world plan.");
        var state = save.State.Deserialize<WorldState>(SaveOptions) ?? throw new InvalidDataException("Missing world state.");
        if (save.Chunks.Count > 16384 || save.Chunks.Any(c => c == null) || save.Chunks.Select(c => c.Key).Distinct().Count() != save.Chunks.Count) throw new InvalidDataException("Invalid chunk references.");
        state.Chunks = [];
        foreach (var reference in save.Chunks)
        {
            if (reference.Hash == null || reference.Hash.Length != 64 || reference.Hash.Any(c => !char.IsAsciiHexDigit(c))) throw new InvalidDataException("Invalid chunk hash.");
            byte[] data = File.ReadAllBytes(Path.Combine(Path.GetFullPath(path) + ".chunks", reference.Hash + ".json"));
            if (Convert.ToHexString(SHA256.HashData(data)) != reference.Hash) throw new InvalidDataException("Chunk snapshot checksum mismatch.");
            var delta = JsonSerializer.Deserialize<ChunkDelta>(data, SaveOptions) ?? throw new InvalidDataException("Empty chunk snapshot.");
            if (delta.Key != reference.Key || !plan.Contains(new(delta.Key.X * 64, delta.Key.Y * 64, delta.Key.Floor)) || delta.Edits == null || delta.Discovered == null || delta.Storage == null || delta.Edits.Count > 4096 || delta.Discovered.Any(i => i is < 0 or >= 4096) || delta.Edits.Any(e => e.Key is < 0 or >= 4096 || !Enum.IsDefined(e.Value)) || delta.Storage.Any(s => s.Key is < 0 or >= 4096 || s.Value == null || s.Value.Any(e => e.Value < 0))) throw new InvalidDataException("Invalid chunk snapshot.");
            state.Chunks.Add(delta);
        }
        ValidateState(plan, state);
        var session = new WorldSession(plan, state) { SavePath = Path.GetFullPath(path), Paused = true };
        session.RestorePartyFormation();
        session.PrepareLocalCollision(force: true); session.Reveal();
        return session;
    }
    private static void ValidateState(WorldPlan plan, WorldState s)
    {
        if (s.Tick is < 0 or > 1000000000000 || !plan.Contains(s.Position) || s.Actors == null || s.Party == null || s.Inventory == null || s.Towns == null || s.Factions == null || s.Journal == null || s.Arc == null || s.VisitedSites == null || s.Tasks == null) throw new InvalidDataException("Invalid world state.");
        if (s.Actors.Count is < 1 or > 10000 || s.Actors.Any(a => a == null) || s.Actors.Select(a => a.Id).Distinct().Count() != s.Actors.Count || s.NextActorId <= s.Actors.Max(a => a.Id) || s.Party.Count is < 1 or > 4 || s.Party.Distinct().Count() != s.Party.Count || s.CombatRandom == 0) throw new InvalidDataException("Invalid actor identities.");
        foreach (var a in s.Actors)
            if (a.Id <= 0 || string.IsNullOrWhiteSpace(a.Name) || !plan.Contains(a.Position) || a.Role is < 0 or > 3 || a.Level is < 1 or > 100 || a.HomeTown is < 0 or >= 24 || a.FactionId is < 0 or >= 6 || a.Culture is < 0 or >= 6 || a.Body == null || a.Memories == null || a.Trust == null || a.Equipment == null || a.Equipment.Any(e => e.Value == null) || a.Health < 0 || a.Health > a.MaxHealth || a.Mana < 0 || a.Mana > a.MaxMana || !Enum.IsDefined(a.Kind) || !Enum.IsDefined(a.Life) || a.Cargo == null || a.Cargo.Any(e => e.Value < 0) || a.Route == null || a.Route.Any(r => r < 0 || r >= plan.Roads.Count) || a.DestinationTown is < 0 or >= 24) throw new InvalidDataException("Invalid actor.");
        foreach (var actor in s.Actors)
            if (actor.Archetype == null || !ContentCatalog.Default.Entries.TryGetValue(actor.Archetype, out var definition) || definition is not (CharacterDefinition or BossDefinition)
                || actor.BaseHealth is < 1 or > 3000 || actor.BaseMana is < 0 or > 500 || actor.BaseArmor is < 0 or > 30 || actor.BasePower is < 0 or > 100 || actor.BaseSpeed is < 1 or > 30
                || string.IsNullOrWhiteSpace(actor.SpriteId) || actor.TraitId == null || actor.TraitId.Length > 0 && !ContentCatalog.Default.Entries.ContainsKey(actor.TraitId))
                throw new InvalidDataException("Invalid content-backed actor state.");
        var actorIds = s.Actors.Select(a => a.Id).ToHashSet();
        var stacks = ContentCatalog.Default.All<ItemDefinition>().Select(i => i.Stack).ToHashSet();
        foreach (var actor in s.Actors)
        {
            var body = actor.Body;
            if (new[] { body.Hunger, body.Fatigue }.Any(v => !double.IsFinite(v) || v < 0 || v > 100) || body.Wounds is < 0 or > 10
                || body.RestUntil < 0 || body.ForageAfter < 0 || !double.IsFinite(body.ForageProgress) || body.ForageProgress is < 0 or > 300 || !double.IsFinite(body.WorkDone) || body.WorkDone < 0
                || actor.Stress is < 0 or > 100 || actor.Memories.Count > 12 || actor.Memories.Any(m => m == null || string.IsNullOrWhiteSpace(m.Key) || m.Text == null || m.Text.Length > 512
                    || m.Tick < 0 || m.Tick > s.Tick || m.Until < m.Tick || m.Mood is < -20 or > 20 || m.Other != 0 && !actorIds.Contains(m.Other))
                || actor.Trust.Any(t => !actorIds.Contains(t.Key) || t.Value is < -100 or > 100)) throw new InvalidDataException("Invalid actor needs or memories.");
        }
        bool StackValid(Dictionary<string, int>? values) => values != null && values.All(p => stacks.Contains(p.Key) && p.Value is >= 0 and <= 9999);
        if (s.WorkRally is { } rally && !plan.Contains(rally) || s.Work == null || s.Structures == null || s.NextWorkId < 1 || s.IncidentRandom == 0 || s.IncidentAfter == null || s.Work.Count > 64
            || s.Work.Select(w => w?.Id).Distinct().Count() != s.Work.Count
            || s.Work.Any(w => w == null || w.Id < 1 || w.Id >= s.NextWorkId || !Enum.IsDefined(w.Kind) || !plan.Contains(w.Target)
                || w.Before is Material.StairsUp or Material.StairsDown || !Enum.IsDefined(w.Before) || !Enum.IsDefined(w.After)
                || w.Kind == WorkKind.Build && w.After is not (Material.Wall or Material.Floor or Material.Door or Material.Bridge or Material.Storage or Material.Camp)
                || !double.IsFinite(w.Required) || w.Required is < 1 or > 1000 || !double.IsFinite(w.Progress) || w.Progress < 0 || w.Progress >= w.Required
                || w.Worker != 0 && !actorIds.Contains(w.Worker) || !StackValid(w.Reserved) || !StackValid(w.Yield) || w.Status == null || w.Priority is < 0 or > 2
                || w.Kind is WorkKind.Build or WorkKind.Craft && (!ContentCatalog.Default.Entries.TryGetValue(w.Definition, out var recipe) || recipe is not RecipeDefinition))
            || s.Work.Where(w => w.Worker != 0).GroupBy(w => w.Worker).Any(g => g.Count() > 1)
            || s.Structures.Any(b => b == null || !plan.Contains(b.Position) || !Enum.IsDefined(b.Material) || b.Material is Material.StairsUp or Material.StairsDown || !Enum.IsDefined(b.Foundation)
                || !StackValid(b.Cost) || !double.IsFinite(b.Condition) || b.Condition is < 0 or > 100)
            || s.Structures.Select(b => b.Position).Distinct().Count() != s.Structures.Count
            || s.IncidentAfter.Any(p => !ContentCatalog.Default.Entries.TryGetValue(p.Key, out var entry) || entry is not IncidentDefinition || p.Value < 0)) throw new InvalidDataException("Invalid work or incident state.");
        foreach (var actor in s.Actors)
        {
            if (actor.VariantId == null || actor.VariantId.Length > 0 && (!ContentCatalog.Default.Entries.TryGetValue(actor.VariantId, out var variant) || variant is not VariantDefinition)
                || actor.EncounterLeader != 0 && !s.Actors.Any(a => a.Id == actor.EncounterLeader && ContentCatalog.Default.Entries[a.Archetype] is BossDefinition)
                    && !(s.Discoveries?.Any(d => d?.Enemies is { Length: > 0 } && d.Enemies[0] == actor.EncounterLeader && d.Enemies.Contains(actor.Id)) ?? false))
                throw new InvalidDataException("Invalid encounter identity.");
            foreach (var (slot, gear) in actor.Equipment)
            {
                var item = ContentCatalog.Default.Get<ItemDefinition>(gear.Item);
                if (item.Slot != slot || slot == "none" || gear != new EquipmentState(item.Id, item.Health, item.Power, item.Armor, item.Speed)) throw new InvalidDataException("Invalid equipped item.");
            }
        }
        if (s.Endgame == null || s.Endgame.SiteId is < 0 or >= 48 || !s.Actors.Any(a => a.Id == s.Endgame.ActorId && ContentCatalog.Default.Entries[a.Archetype] is BossDefinition)
            || s.Endgame.Victory && (s.Battle != null || s.Actors.Single(a => a.Id == s.Endgame.ActorId).Life != LifeState.Dead)
            || s.Loot == null || s.Loot.Count > 2048 || s.Loot.Any(c => c == null || c.Id == null || !plan.Contains(c.Position) || c.Contents == null || c.Contents.Any(p => p.Value < 0 || !ContentCatalog.Default.All<ItemDefinition>().Any(i => i.Stack == p.Key))
                || c.Choices == null || c.Choices.Length is not (0 or 3) || c.Choices.Distinct().Count() != c.Choices.Length
                || c.Choices.Any(id => id == null || !ContentCatalog.Default.Entries.TryGetValue(id, out var entry) || entry is not ItemDefinition item || item.Slot == "none")
                || (c.Choices.Length > 0 && c.Taken ? c.Selected == null || !c.Choices.Contains(c.Selected) : c.Selected != null))
            || s.Loot.Select(c => c.Id).Distinct().Count() != s.Loot.Count) throw new InvalidDataException("Invalid adventure state.");
        ValidateDiscoveries(plan, s);
        foreach (var caravan in s.Actors.Where(a => a.Kind == ActorKind.Caravan))
        {
            if (caravan.DestinationTown == null || caravan.DestinationTown == caravan.HomeTown || caravan.Traveling && caravan.Route.Count == 0) throw new InvalidDataException("Invalid caravan destination/route.");
            int current = caravan.HomeTown;
            foreach (int id in caravan.Route)
            {
                var road = plan.Roads[id];
                if (road.FromTown != current && road.ToTown != current) throw new InvalidDataException("Disconnected caravan route.");
                current = road.FromTown == current ? road.ToTown : road.FromTown;
            }
            if (caravan.Traveling && current != caravan.DestinationTown) throw new InvalidDataException("Caravan route misses destination.");
        }
        if (s.Party.Any(id => !s.Actors.Any(a => a.Id == id && a.Recruited && a.Kind == ActorKind.Person)) || s.Towns.Count != 24 || s.Towns.Where((t, i) => t == null || t.Id != i || t.Stocks == null || new[] { "food", "wood", "stone", "medicine" }.Any(k => !t.Stocks.ContainsKey(k)) || t.Stocks.Any(e => e.Value < 0)).Any() || s.Factions.Count != 6 || s.Factions.Any(f => f == null || f.Relations == null || f.Relations.Length != 6) || s.Inventory.Any(e => e.Value < 0) || s.Arc.TownId is < 0 or >= 24 || s.Arc.SiteId is < 0 or >= 48 || !s.Actors.Any(a => a.Id == s.Arc.PatronId) || !Enum.IsDefined(s.Arc.Kind) || !Enum.IsDefined(s.Arc.Status) || s.Tasks.Any(t => t == null || t.TownId is < 0 or >= 24 || t.RoadId < 0 || t.RoadId >= plan.Roads.Count)) throw new InvalidDataException("Invalid party, economy or arc.");
        if (s.Towns.Any(t => t.Stocks.Count != 4) || s.Factions.Where((f, i) => f.Id != i || f.Relations.Any(r => r is < -100 or > 100)).Any() || s.Journal.Any(j => j == null) || s.VisitedSites.Any(id => id is < 0 or >= 48)) throw new InvalidDataException("Invalid economy/history.");
        var party = s.Party.Select(id => s.Actors.Single(a => a.Id == id));
        if (s.AwaitingContinuation ? party.Any(a => a.Life != LifeState.Dead) : !party.Any(a => a.Life == LifeState.Alive)) throw new InvalidDataException("Invalid party life state.");
        if (s.Battle is { } b)
        {
            if (s.AwaitingContinuation || b.Plans == null || b.Units == null || b.ActorIds == null || b.Order == null || b.Units.Count is < 2 or > 8 || b.Units.Any(u => u == null) || b.Units.Count != b.ActorIds.Count || b.ActorIds.Distinct().Count() != b.ActorIds.Count || b.ActorIds.Any(id => !s.Actors.Any(a => a.Id == id)) || b.Units.Where((u, i) => u.Id != i || u.Health < 0 || u.Health > u.MaxHealth || u.Mana < 0 || u.Mana > u.MaxMana || u.Friendly != s.Party.Contains(b.ActorIds[i])).Any() || b.Order.Count == 0 || b.Order.Distinct().Count() != b.Order.Count || b.Order.Any(i => i < 0 || i >= b.Units.Count) || b.TurnIndex < 0 || b.TurnIndex >= b.Order.Count || !b.Active.Alive || !b.Units.Any(u => u.Friendly && u.Alive) || !b.Units.Any(u => !u.Friendly && u.Alive)) throw new InvalidDataException("Invalid battle.");
            if (b.Plans.Count > 4 || b.Plans.Any(p => p.Key < 0 || p.Key >= b.Units.Count || b.Units[p.Key].Friendly || p.Value == null || p.Value.Target < 0 || p.Value.Target >= b.Units.Count
                || !ContentCatalog.Default.Entries.TryGetValue(p.Value.Ability, out var d) || d is not AbilityDefinition ability || ability.Effect is "flee" or "potion")
                || b.Units.Any(u => !u.Friendly && u.Alive && !b.Plans.ContainsKey(u.Id))) throw new InvalidDataException("Invalid enemy intentions.");
            foreach (var unit in b.Units)
            {
                var actor = s.Actors.Single(a => a.Id == b.ActorIds[unit.Id]);
                if (unit.Health != actor.Health || unit.Mana != actor.Mana || unit.Stress != actor.Stress || unit.MaxHealth != actor.MaxHealth || unit.MaxMana != actor.MaxMana)
                    throw new InvalidDataException("Combat and actor state disagree.");
            }
            foreach (var (unit, intent) in b.Plans)
            {
                var ability = ContentCatalog.Default.Get<AbilityDefinition>(intent.Ability);
                bool valid = ability.Target switch
                {
                    TargetKind.Self => intent.Target == unit,
                    TargetKind.Ally or TargetKind.AllAllies => !b.Units[intent.Target].Friendly,
                    _ => b.Units[intent.Target].Friendly
                };
                if (!valid) throw new InvalidDataException("Invalid intention target.");
            }
            if (b.Round is < 1 or > 100000 || b.Units.Any(u => u.MaxHealth is < 1 or > 10000 || u.MaxMana is < 0 or > 10000 || u.Stress is < 0 or > 100 || u.Armor is < 0 or > 1000 || u.Speed is < 0 or > 1000 || u.Power is < 0 or > 1000 || u.Burn is < 0 or > 1000 || u.Bleed is < 0 or > 1000 || u.Mark is < 0 or > 1000 || u.Stun is < 0 or > 1000 || !Enum.IsDefined(u.Intent))) throw new InvalidDataException("Invalid combat statistics.");
        }
    }
}
