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
    public static IGameSession Load(string path, Action<WorldDiagnosticEvent>? diagnostics = null) => Format(path) == "vagabonds-world" ? WorldSession.Load(path, diagnostics: diagnostics) : new LegacyCampaignSession(Campaign.LoadRpg(path));
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
        Diagnose("save", new { path, chunks = refs.Count });
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
    public static WorldSession Load(string path, ContentCatalog? content = null, Action<WorldDiagnosticEvent>? diagnostics = null)
    {
        var catalog = content ?? ContentCatalog.Default;
        if (catalog.Issues.Count > 0) throw new InvalidDataException("Cannot load with invalid content.");
        var save = JsonSerializer.Deserialize<Manifest>(File.ReadAllBytes(path), SaveOptions) ?? throw new InvalidDataException("Empty world save.");
        if (save.Format != "vagabonds-world" || save.Version != 3 || save.Plan == null || save.State == null || save.Chunks == null) throw new InvalidDataException("Unsupported world save.");
        if (save.ContentHash != catalog.Hash) throw new InvalidDataException("World content differs from this build. Start an explicit new run; the selected save is preserved.");
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
            if (delta.Resources == null || delta.Resources.Count > 4096 || delta.Resources.Any(r => r.Key is < 0 or >= 4096 || r.Value == null
                || !catalog.Entries.TryGetValue(r.Value.Definition, out var entry) || entry is not ResourceDefinition resource
                || r.Value.Remaining < 0 || r.Value.Remaining >= resource.Capacity || r.Value.RecoverAt < 0
                || r.Value.Remaining > 0 && r.Value.RecoverAt != 0 || resource.RegrowTicks == 0 && r.Value.RecoverAt != 0
                || r.Value.Remaining == 0 && resource.RegrowTicks > 0 && (r.Value.RecoverAt == 0 || r.Value.RecoverAt > state.Tick + resource.RegrowTicks)))
                throw new InvalidDataException("Invalid resource stock.");
            state.Chunks.Add(delta);
        }
        ValidateState(plan, state, catalog);
        var session = new WorldSession(plan, state, catalog, diagnostics, observe: false) { SavePath = Path.GetFullPath(path), Paused = true };
        foreach (var chunk in state.Chunks)
            foreach (var (index, stock) in chunk.Resources)
            {
                var p = new WorldPosition(chunk.Key.X * 64 + index % 64, chunk.Key.Y * 64 + index / 64, chunk.Key.Floor);
                if (!session.Chunks.IsEdited(p) && session.ResourceAt(p)?.Definition.Id != stock.Definition) throw new InvalidDataException("Resource stock does not match its world location.");
            }
        foreach (var order in state.Work.Where(w => w.Kind == WorkKind.Harvest))
        {
            if (!catalog.Entries.TryGetValue(order.Definition, out var entry) || entry is not ResourceDefinition resource
                || session.ResourceAt(order.Target) is not { Available: > 0 } node || node.Definition.Id != resource.Id
                || order.Required != resource.Work || order.Reserved.Count != 0 || order.Before != order.After
                || order.Yield.Count != resource.Yield.Count || resource.Yield.Any(y => order.Yield.GetValueOrDefault(catalog.Get<ItemDefinition>(y.Key).Stack) != y.Value))
                throw new InvalidDataException("Invalid resource work order.");
        }
        if (state.Work.Any(session.SupplySourceChanged)) throw new InvalidDataException("Reserved storage no longer exists.");
        foreach (var order in state.Work.Where(w => w.Kind == WorkKind.Camp))
        {
            if (order.Station is not { } station || !session.ServiceableCamp(station) || order.Target.Distance(station) > 2
                || !catalog.Entries.TryGetValue(order.Definition, out var entry) || entry is not CampDefinition duty
                || order.Required != duty.Work || order.Before != order.After || order.Yield.Count != 0 || order.Reserved.Count != duty.Cost.Count
                || duty.Cost.Any(y => order.Reserved.GetValueOrDefault(catalog.Get<ItemDefinition>(y.Key).Stack) != y.Value))
                throw new InvalidDataException("Invalid camp duty.");
        }
        session.RestorePartyFormation();
        session.PrepareLocalCollision(force: true); session.RefreshVisibility(remember: false);
        return session;
    }
    private static void ValidateState(WorldPlan plan, WorldState s, ContentCatalog catalog)
    {
        if (s.Tick is < 0 or > 1000000000000 || !plan.Contains(s.Position) || s.Actors == null || s.Party == null || s.Inventory == null || s.Towns == null || s.Factions == null || s.Journal == null || s.Arc == null || s.VisitedSites == null || s.Tasks == null) throw new InvalidDataException("Invalid world state.");
        if (s.Actors.Count is < 1 or > 10000 || s.Actors.Any(a => a == null) || s.Actors.Select(a => a.Id).Distinct().Count() != s.Actors.Count || s.NextActorId <= s.Actors.Max(a => a.Id) || s.Party.Count is < 1 or > 4 || s.Party.Distinct().Count() != s.Party.Count || s.CombatRandom == 0) throw new InvalidDataException("Invalid actor identities.");
        foreach (var a in s.Actors)
            if (a.Id <= 0 || string.IsNullOrWhiteSpace(a.Name) || !plan.Contains(a.Position) || a.Role is < 0 or > 3 || a.Level is < 1 or > 100 || a.HomeTown is < 0 or >= 24 || a.FactionId is < 0 or >= 6 || a.Culture is < 0 or >= 6 || a.Body == null || a.Memories == null || a.Trust == null || a.Equipment == null || a.Equipment.Any(e => e.Value == null) || a.Health < 0 || a.Health > a.MaxHealth || a.Mana < 0 || a.Mana > a.MaxMana || !Enum.IsDefined(a.Kind) || !Enum.IsDefined(a.Life) || a.Cargo == null || a.Cargo.Any(e => e.Value < 0) || a.Route == null || a.Route.Any(r => r < 0 || r >= plan.Roads.Count) || a.DestinationTown is < 0 or >= 24) throw new InvalidDataException("Invalid actor.");
        foreach (var actor in s.Actors)
            if (actor.Archetype == null || !catalog.Entries.TryGetValue(actor.Archetype, out var definition) || definition is not (CharacterDefinition or BossDefinition)
                || actor.BaseHealth is < 1 or > 3000 || actor.BaseMana is < 0 or > 500 || actor.BaseArmor is < 0 or > 30 || actor.BasePower is < 0 or > 100 || actor.BaseSpeed is < 1 or > 30
                || string.IsNullOrWhiteSpace(actor.SpriteId) || actor.TraitId == null || actor.TraitId.Length > 0 && !catalog.Entries.ContainsKey(actor.TraitId))
                throw new InvalidDataException("Invalid content-backed actor state.");
        var actorIds = s.Actors.Select(a => a.Id).ToHashSet();
        foreach (var actor in s.Actors)
            if (actor.Routine is { } routine && (actor.Kind != ActorKind.Person || !Enum.IsDefined(routine.Kind) || !plan.Contains(routine.Target)
                || routine.Since < 0 || routine.Since > s.Tick || string.IsNullOrWhiteSpace(routine.Reason) || routine.Reason.Length > 256
                || routine.Other != 0 && !actorIds.Contains(routine.Other))) throw new InvalidDataException("Invalid resident routine.");
        if (s.EcologyChunks == null || s.EcologyChunks.Count > 16384 || s.EcologyChunks.Any(k => k.X is < 0 or >= 64 || k.Y is < 0 or >= 64 || k.Floor is < 0 or >= WorldPlan.Floors)) throw new InvalidDataException("Invalid observed ecology chunks.");
        foreach (var actor in s.Actors)
        {
            var ecology = (catalog.Entries[actor.Archetype] as CharacterDefinition)?.Ecology;
            if (ecology == null ? actor.Habitat != null || actor.ProvokedUntil != 0
                : actor.Habitat is not { } home || !plan.Contains(home) || !s.EcologyChunks.Contains(home.Chunk) || actor.Kind != ActorKind.Beast
                    || ecology.Underground != (home.Floor > 0) || actor.Position.Floor != home.Floor || actor.Recruited
                    || actor.ProvokedUntil < 0 || actor.ProvokedUntil > s.Tick + 75)
                throw new InvalidDataException("Invalid wildlife habitat or provocation.");
        }
        if (s.Actors.Where(a => a.Habitat != null).GroupBy(a => a.Habitat!.Value.Chunk).Any(g => g.Count() > 1)) throw new InvalidDataException("Duplicate wildlife habitat in a chunk.");
        if (s.GeneratedEquipment == null || s.GeneratedEquipment.Count > 6144) throw new InvalidDataException("Invalid generated equipment registry.");
        foreach (var (id, gear) in s.GeneratedEquipment)
            if (!System.Text.RegularExpressions.Regex.IsMatch(id, "^gear\\.[0-9]+\\.[0-9]+\\.[0-9]+$") || gear == null || gear.Base == null
                || !catalog.Entries.TryGetValue(gear.Base, out var entry) || entry is not ItemDefinition basis || basis.Slot == "none"
                || gear.Affixes == null || gear.Affixes.Length is < 1 or > 2 || gear.Affixes.Distinct().Count() != gear.Affixes.Length
                || gear.Affixes.Any(a => a == null || !basis.AffixPool.Contains(a) || basis.Effects.Contains(a))) throw new InvalidDataException("Invalid generated equipment effects.");
        ItemDefinition? SavedItem(string id) => catalog.Entries.GetValueOrDefault(id) as ItemDefinition
            ?? (s.GeneratedEquipment.TryGetValue(id, out var generated) ? ResolveEquipment(catalog, id, generated) : null);
        var stacks = catalog.All<ItemDefinition>().Select(i => i.Stack).Concat(s.GeneratedEquipment.Keys).ToHashSet();
        foreach (var actor in s.Actors)
        {
            if (actor.Preparations == null || actor.Preparations.Count > 5 || actor.Preparations.Select(p => p?.Effect).Distinct().Count() != actor.Preparations.Count
                || actor.Preparations.Any(p => p == null || !catalog.Entries.TryGetValue(p.Definition, out var entry) || entry is not CampDefinition camp
                    || camp.Role is not ("cook" or "watch") || p.Effect != camp.Effect || p.Power != camp.Power || p.Until < 1 || p.Until > s.Tick + camp.Duration))
                throw new InvalidDataException("Invalid camp preparation.");
            var body = actor.Body;
            if (new[] { body.Hunger, body.Fatigue }.Any(v => !double.IsFinite(v) || v < 0 || v > 100) || body.Wounds is < 0 or > 10
                || body.RestUntil < 0 || body.ForageAfter < 0 || !double.IsFinite(body.ForageProgress) || body.ForageProgress is < 0 or > 300 || !double.IsFinite(body.WorkDone) || body.WorkDone < 0
                || actor.Stress is < 0 or > 100 || actor.Memories.Count > 12 || actor.Memories.Any(m => m == null || string.IsNullOrWhiteSpace(m.Key) || m.Text == null || m.Text.Length > 512
                    || m.Tick < 0 || m.Tick > s.Tick || m.Until < m.Tick || m.Mood is < -20 or > 20 || m.Other != 0 && !actorIds.Contains(m.Other))
                || actor.Trust.Any(t => !actorIds.Contains(t.Key) || t.Value is < -100 or > 100)) throw new InvalidDataException("Invalid actor needs or memories.");
        }
        if (s.LastCampStory is { } story && (!actorIds.Contains(story.Speaker) || story.Text == null || story.Text.Length > 512 || story.Tick < 0 || story.Tick > s.Tick || !plan.Contains(story.Camp))) throw new InvalidDataException("Invalid camp story.");
        bool StackValid(Dictionary<string, int>? values) => values != null && values.All(p => stacks.Contains(p.Key) && p.Value is >= 0 and <= 9999);
        if (!StackValid(s.Inventory) || s.Chunks.Any(c => c.Storage.Values.Any(v => !StackValid(v)))) throw new InvalidDataException("Invalid inventory stacks.");
        var uniqueItems = s.GeneratedEquipment.Keys.ToDictionary(id => id, id => s.Inventory.GetValueOrDefault(id));
        foreach (var gear in s.Actors.SelectMany(a => a.Equipment.Values)) if (uniqueItems.ContainsKey(gear.Item)) uniqueItems[gear.Item]++;
        foreach (var entry in s.Chunks.SelectMany(c => c.Storage.Values).SelectMany(v => v)) if (uniqueItems.ContainsKey(entry.Key)) uniqueItems[entry.Key] += entry.Value;
        if (uniqueItems.Values.Any(count => count > 1)) throw new InvalidDataException("A generated item is duplicated.");
        if (s.WorkRally is { } rally && !plan.Contains(rally) || s.Work == null || s.Structures == null || s.NextWorkId < 1 || s.IncidentRandom == 0 || s.IncidentAfter == null || s.Work.Count > 64
            || s.Work.Select(w => w?.Id).Distinct().Count() != s.Work.Count
            || s.Work.Any(w => w == null || w.Id < 1 || w.Id >= s.NextWorkId || !Enum.IsDefined(w.Kind) || !plan.Contains(w.Target)
                || w.Before is Material.StairsUp or Material.StairsDown || !Enum.IsDefined(w.Before) || !Enum.IsDefined(w.After)
                || w.Kind == WorkKind.Build && w.After is not (Material.Wall or Material.Floor or Material.Door or Material.Bridge or Material.Storage or Material.Camp)
                || w.Method == null || !double.IsFinite(w.Required) || w.Required is < 1 or > 1000 || !double.IsFinite(w.Progress) || w.Progress < 0 || w.Progress >= w.Required
                || w.Worker != 0 && !actorIds.Contains(w.Worker) || !StackValid(w.Reserved) || !StackValid(w.Yield) || w.Status == null || w.Priority is < 0 or > 2
                || w.Kind == WorkKind.Camp && (!catalog.Entries.TryGetValue(w.Definition, out var campDuty) || campDuty is not CampDefinition || w.Station == null || !plan.Contains(w.Station.Value))
                || w.Kind == WorkKind.Discovery && (!catalog.Entries.TryGetValue(w.Definition, out var discoveryDefinition) || discoveryDefinition is not DiscoveryDefinition { Situation: not null } sd
                    || !sd.Situation.Approaches.Any(a => a.Action == w.Method && a.Work > 0 && a.Work == w.Required
                        && a.Cost.Count == w.Reserved.Count && a.Cost.All(p => w.Reserved.GetValueOrDefault(catalog.Get<ItemDefinition>(p.Key).Stack) == p.Value))
                    || w.Yield.Count != 0 || w.Before != w.After || !s.Discoveries.Any(d => d.Position == w.Target && d.Definition == w.Definition))
                || w.Kind is WorkKind.Build or WorkKind.Craft && (!catalog.Entries.TryGetValue(w.Definition, out var recipe) || recipe is not RecipeDefinition))
            || s.Work.Where(w => w.Worker != 0).GroupBy(w => w.Worker).Any(g => g.Count() > 1)
            || s.Structures.Any(b => b == null || !plan.Contains(b.Position) || !Enum.IsDefined(b.Material) || b.Material is Material.StairsUp or Material.StairsDown || !Enum.IsDefined(b.Foundation)
                || !StackValid(b.Cost) || !double.IsFinite(b.Condition) || b.Condition is < 0 or > 100)
            || s.Structures.Select(b => b.Position).Distinct().Count() != s.Structures.Count
            || s.IncidentAfter.Any(p => !catalog.Entries.TryGetValue(p.Key, out var entry) || entry is not IncidentDefinition || p.Value < 0)) throw new InvalidDataException("Invalid work or incident state.");
        ValidateWorkSupplies(plan, s, catalog);
        foreach (var actor in s.Actors)
        {
            if (actor.VariantId == null || actor.VariantId.Length > 0 && (!catalog.Entries.TryGetValue(actor.VariantId, out var variant) || variant is not VariantDefinition)
                || actor.EncounterLeader != 0 && !s.Actors.Any(a => a.Id == actor.EncounterLeader && catalog.Entries[a.Archetype] is BossDefinition)
                    && !(actor.EncounterId?.Length > 0 && s.Actors.Any(a => a.Id == actor.EncounterLeader && a.EncounterId == actor.EncounterId))
                    && !(s.Discoveries?.Any(d => d?.Enemies is { Length: > 0 } && d.Enemies[0] == actor.EncounterLeader && d.Enemies.Contains(actor.Id)) ?? false))
                throw new InvalidDataException("Invalid encounter identity.");
            foreach (var (slot, gear) in actor.Equipment)
            {
                var item = gear.Item == null ? null : SavedItem(gear.Item);
                if (item == null || item.Slot != slot || slot == "none" || gear != new EquipmentState(item.Id, item.Health, item.Power, item.Armor, item.Speed)) throw new InvalidDataException("Invalid equipped item.");
            }
        }
        if (s.Endgame == null || s.Endgame.SiteId is < 0 or >= 48 || !s.Actors.Any(a => a.Id == s.Endgame.ActorId && catalog.Entries[a.Archetype] is BossDefinition)
            || s.Endgame.Victory && (s.Battle != null || s.Actors.Single(a => a.Id == s.Endgame.ActorId).Life != LifeState.Dead)
            || s.Loot == null || s.Loot.Count > 10000 || s.Loot.Any(c => c == null || c.Id == null || !plan.Contains(c.Position) || c.Contents == null || c.Contents.Any(p => p.Value < 0 || !stacks.Contains(p.Key))
                || c.Choices == null || c.Choices.Length is not (0 or 3) || c.Choices.Distinct().Count() != c.Choices.Length
                || c.Choices.Any(id => id == null || SavedItem(id) is not { Slot: not "none" })
                || (c.Choices.Length > 0 && c.Taken ? c.Selected == null || !c.Choices.Contains(c.Selected) : c.Selected != null))
            || s.Loot.Select(c => c.Id).Distinct().Count() != s.Loot.Count) throw new InvalidDataException("Invalid adventure state.");
        if (s.Loot.Any(c => IsWorkSupplyCache(c) && (c.Choices.Length != 0 || c.Selected != null))) throw new InvalidDataException("Invalid returned supply bundle.");
        foreach (var cache in s.Loot.Where(c => c.SourceActor != 0))
        {
            var actor = s.Actors.FirstOrDefault(a => a.Id == cache.SourceActor);
            var ecology = actor == null ? null : (catalog.Entries[actor.Archetype] as CharacterDefinition)?.Ecology;
            if (actor == null || ecology == null || actor.Life != LifeState.Dead || cache.Position != actor.Position || cache.Id != "wildlife-" + actor.Id
                || cache.Choices.Length != 0 || cache.Contents.Count != ecology.Drops.Count
                || ecology.Drops.Any(p => cache.Contents.GetValueOrDefault(catalog.Get<ItemDefinition>(p.Key).Stack) != p.Value)) throw new InvalidDataException("Invalid wildlife remains.");
        }
        if (s.Actors.Any(a => a.Habitat != null && a.Life == LifeState.Dead && !s.Loot.Any(c => c.SourceActor == a.Id))) throw new InvalidDataException("Missing wildlife remains.");
        ValidateProgression(s, catalog);
        ValidateDiscoveries(plan, s, catalog);
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
            if (b.Assisting == null || b.Assisting.Count > 3 || b.Assisting.Any(id => id < 0 || id >= b.Units.Count || !b.Units[id].Friendly || b.Order.IndexOf(id) <= b.TurnIndex)) throw new InvalidDataException("Invalid committed cooperative actions.");
            if (b.Plans.Count > 4 || b.Plans.Any(p => p.Key < 0 || p.Key >= b.Units.Count || b.Units[p.Key].Friendly || p.Value == null || p.Value.Target < 0 || p.Value.Target >= b.Units.Count
                || !catalog.Entries.TryGetValue(p.Value.Ability, out var d) || d is not AbilityDefinition ability || ability.Effect is "flee" or "potion" || ability.Cooperation != null)
                || b.Units.Any(u => !u.Friendly && u.Alive && !b.Plans.ContainsKey(u.Id))) throw new InvalidDataException("Invalid enemy intentions.");
            foreach (var unit in b.Units)
            {
                var actor = s.Actors.Single(a => a.Id == b.ActorIds[unit.Id]);
                if (unit.Health != actor.Health || unit.Mana != actor.Mana || unit.Stress != actor.Stress || unit.MaxHealth != actor.MaxHealth || unit.MaxMana != actor.MaxMana)
                    throw new InvalidDataException("Combat and actor state disagree.");
            }
            foreach (var (unit, intent) in b.Plans)
            {
                var ability = catalog.Get<AbilityDefinition>(intent.Ability);
                bool valid = ability.Target switch
                {
                    TargetKind.Self => intent.Target == unit,
                    TargetKind.Ally or TargetKind.AllAllies => !b.Units[intent.Target].Friendly,
                    _ => b.Units[intent.Target].Friendly
                };
                if (!valid) throw new InvalidDataException("Invalid intention target.");
            }
            if (b.ShieldUsed == null || b.ShieldUsed.Any(i => i < 0 || i >= b.Units.Count)) throw new InvalidDataException("Invalid shield usage.");
            if (b.Round is < 1 or > 100000 || b.Units.Any(u => u.MaxHealth is < 1 or > 10000 || u.MaxMana is < 0 or > 10000 || u.Stress is < 0 or > 100 || u.Armor is < 0 or > 1000 || u.Speed is < 0 or > 1000 || u.Power is < 0 or > 1000 || u.Burn is < 0 or > 1000 || u.Bleed is < 0 or > 1000 || u.Mark is < 0 or > 1000 || u.Stun is < 0 or > 1000 || !Enum.IsDefined(u.Intent))) throw new InvalidDataException("Invalid combat statistics.");
        }
        ValidateEncounterState(s, catalog);
        ValidateEncounterPopulations(plan, s, catalog);
    }
}
