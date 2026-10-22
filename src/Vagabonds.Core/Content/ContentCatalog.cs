using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Vagabonds.Core.Content;

public sealed record ContentIssue(string File, string Field, string Message);
public sealed record ContentReference(string Source, string Field, string Target);

public sealed class ContentCatalog
{
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, AllowOutOfOrderMetadataProperties = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };
    private static readonly Lazy<ContentCatalog> EmbeddedInstance = new(Embedded);
    public static ContentCatalog Default => EmbeddedInstance.Value;
    public static readonly string[] Effects = ["damage", "burn", "bleed", "mark", "stun", "heal", "calm", "guard", "rally", "potion", "flee", "dread", "drain", "cleanse"];
    public static readonly string[] Actions = ["recruit", "trade", "heal", "converse", "share-meal", "rumour"];
    public static readonly string[] Kinds = ["ability", "affix", "boss", "camp", "character", "class", "dialogue", "discovery", "game", "generation", "incident", "interaction", "item", "names", "recipe", "resource", "simulation", "trait", "variant"];
    public static readonly string[] Layouts = ["overlapping-wfc", "caverns", "bsp-workings", "bsp-fort", "ring-sanctuary"];
    private readonly Dictionary<string, ContentDefinition> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _sources = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, ContentDefinition> Entries => _entries;
    public List<ContentIssue> Issues { get; } = [];
    public List<ContentReference> References { get; } = [];
    public string Hash { get; private set; } = "";
    public string PresentationHash { get; private set; } = "";
    public ClassDefinition[] Classes => All<ClassDefinition>().OrderBy(c => c.Role).ToArray();
    public GameDefinition Game => Get<GameDefinition>("game.vagabonds");
    public IEnumerable<T> All<T>() where T : ContentDefinition => _entries.Values.OfType<T>().OrderBy(e => e.Id, StringComparer.Ordinal);
    public T Get<T>(string id) where T : ContentDefinition => _entries.TryGetValue(id, out var entry) && entry is T typed ? typed
        : throw new InvalidDataException($"Missing {typeof(T).Name}: {id}.");
    public string Source(string id) => _sources.GetValueOrDefault(id, "");
    public static bool ValidId(string? id) => !string.IsNullOrWhiteSpace(id) && Regex.IsMatch(id, "^[a-z][a-z0-9-]*\\.[a-z0-9][a-z0-9.-]*$");
    public static string Kind(ContentDefinition d) => d switch
    {
        AbilityDefinition => "ability", AffixDefinition => "affix", BossDefinition => "boss", CampDefinition => "camp", CharacterDefinition => "character", ClassDefinition => "class",
        DialogueDefinition => "dialogue", InteractionDefinition => "interaction", ItemDefinition => "item", NamesDefinition => "names", RecipeDefinition => "recipe",
        TraitDefinition => "trait", VariantDefinition => "variant", GameDefinition => "game", GenerationDefinition => "generation", SimulationDefinition => "simulation", IncidentDefinition => "incident", DiscoveryDefinition => "discovery", ResourceDefinition => "resource", _ => throw new ArgumentException("Unknown content type.")
    };
    public static ContentCatalog LoadDirectory(string root)
    {
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        return Load(Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories).Where(p => Path.GetFileName(p) != "schema.json")
            .Order(StringComparer.Ordinal).Select(p => (File: Path.GetRelativePath(root, p), Text: File.ReadAllText(p))));
    }
    public static ContentCatalog Load(IEnumerable<(string File, string Text)> documents)
    {
        var catalog = new ContentCatalog(); var canonical = new StringBuilder(); var presentation = new StringBuilder();
        foreach (var (file, text) in documents.OrderBy(d => d.File, StringComparer.Ordinal))
        {
            try
            {
                var entry = JsonSerializer.Deserialize<ContentDefinition>(text, Json) ?? throw new JsonException("Empty content definition.");
                if (!ValidId(entry.Id))
                { catalog.Issues.Add(new(file, "id", "Use a stable kind.name identifier in lowercase.")); continue; }
                if (!entry.Id.StartsWith(Kind(entry) + ".", StringComparison.Ordinal)) catalog.Issues.Add(new(file, "id", "Identifier prefix must match kind."));
                if (!catalog._entries.TryAdd(entry.Id, entry)) catalog.Issues.Add(new(file, "id", "Duplicate identifier: " + entry.Id));
                else
                {
                    catalog._sources[entry.Id] = file;
                    // Dialogue is side-effect-free presentation. Editing speech must not
                    // invalidate terrain, actors or reserved work in existing saves.
                    (entry is DialogueDefinition ? presentation : canonical).Append(JsonSerializer.Serialize(entry, Json));
                }
            }
            catch (Exception e) when (e is JsonException or NotSupportedException or InvalidOperationException)
            { catalog.Issues.Add(new(file, "$", e.Message)); }
        }
        catalog.Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
        catalog.PresentationHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(presentation.ToString())));
        catalog.Validate(); return catalog;
    }
    private static ContentCatalog Embedded()
    {
        var assembly = typeof(ContentCatalog).Assembly;
        var documents = assembly.GetManifestResourceNames().Where(n => n.StartsWith("Vagabonds.Content.", StringComparison.Ordinal))
            .Select(n => { using var reader = new StreamReader(assembly.GetManifestResourceStream(n)!); return (File: n, Text: reader.ReadToEnd()); }).ToArray();
        var catalog = Load(documents);
        if (catalog.Issues.Count > 0) throw new InvalidDataException(string.Join("\n", catalog.Issues.Select(i => $"{i.File}:{i.Field}: {i.Message}")));
        return catalog;
    }
    private void Validate()
    {
        using var sprites = JsonDocument.Parse(typeof(ContentCatalog).Assembly.GetManifestResourceStream("Vagabonds.ContentSprites.json")!);
        var bindings = sprites.RootElement.GetProperty("bindings").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var d in _entries.Values)
        {
            string file = Source(d.Id);
            void Error(string field, string message) => Issues.Add(new(file, field, message));
            void Range(string field, int value, int minimum, int maximum) { if (value < minimum || value > maximum) Error(field, $"Expected {minimum}..{maximum}."); }
            void Ref<T>(string field, string? id, bool optional = false) where T : ContentDefinition
            {
                if (optional && id == null) return;
                if (id == null || !_entries.TryGetValue(id, out var target) || target is not T) Error(field, $"Unknown {typeof(T).Name}: {id}.");
                if (id != null) References.Add(new(d.Id, field, id));
            }
            void Cost(string field, Dictionary<string, int>? cost)
            {
                if (cost == null) { Error(field, "Costs cannot be null."); return; }
                foreach (var (item, count) in cost) { Ref<ItemDefinition>(field, item); Range(field, count, 1, 999); }
            }
            void Behaviors(string field, BehaviorRule[]? rules, bool required = false)
            {
                if (rules == null || required && rules.Length == 0) { Error(field, "Provide behaviour rules."); return; }
                if (rules.Length > 16) Error(field, "At most sixteen rules per phase.");
                foreach (var r in rules)
                {
                    if (r == null) { Error(field, "A rule cannot be null."); continue; }
                    Ref<AbilityDefinition>(field, r.Ability); Range(field, r.Weight, 1, 100); Range(field, r.Every, 1, 20);
                    if (r.Ability != null && _entries.TryGetValue(r.Ability, out var entry) && entry is AbilityDefinition ability && (ability.Effect is "flee" or "potion" || ability.Cooperation != null)) Error(field, "Enemy rules cannot use party-only retreat, medicine or cooperative abilities.");
                    Range(field, r.FromRound, 1, 100); Range(field, r.BelowHealthPercent, 1, 100);
                    if (r.Target is not ("random" or "weakest" or "strongest" or "stressed")) Error(field, "Unknown targeting rule.");
                }
            }
            if (string.IsNullOrWhiteSpace(d.Name) || d.Name.Length > 120) Error("name", "A readable name of at most 120 characters is required.");
            if (d.Description == null || d.Tags == null || d.Tags.Any(string.IsNullOrWhiteSpace)) Error("tags", "Description/tags cannot be null or blank.");
            if (d.Sprite != null && !bindings.Contains(d.Sprite)) Error("sprite", "Unknown Vagabonds sprite binding: " + d.Sprite);
            switch (d)
            {
                case GameDefinition g:
                    if (g.CooperativeAbilities == null || g.CooperativeAbilities.Length > 2 || g.CooperativeAbilities.Distinct().Count() != g.CooperativeAbilities.Length) Error("cooperativeAbilities", "Provide up to two distinct cooperative abilities.");
                    else foreach (var id in g.CooperativeAbilities)
                    {
                        Ref<AbilityDefinition>("cooperativeAbilities", id);
                        if (id != null && _entries.TryGetValue(id, out var cooperative) && cooperative is AbilityDefinition { Cooperation: null }) Error("cooperativeAbilities", "Cooperative abilities need partner costs.");
                    }
                    if (g.Wildlife == null || g.Wildlife.Length > 32 || g.Wildlife.Distinct().Count() != g.Wildlife.Length) Error("wildlife", "Provide up to 32 distinct wildlife characters.");
                    else foreach (var id in g.Wildlife)
                    {
                        Ref<CharacterDefinition>("wildlife", id);
                        if (id != null && _entries.TryGetValue(id, out var entry) && entry is CharacterDefinition wildlife && wildlife.Ecology == null) Error("wildlife", "Wildlife needs ecology rules.");
                    }
                    if (g.CampActivities == null || g.CampActivities.Length == 0) Error("campActivities", "Provide camp activities.");
                    else foreach (var id in g.CampActivities) Ref<CampDefinition>("campActivities", id);
                    if (g.Resources == null || g.Resources.Length == 0) Error("resources", "Provide a resource pool.");
                    else foreach (var id in g.Resources) Ref<ResourceDefinition>("resources", id);
                    if (g.Discoveries == null || g.Discoveries.Length == 0) Error("discoveries", "Provide a discovery pool.");
                    else foreach (var id in g.Discoveries) Ref<DiscoveryDefinition>("discoveries", id);
                    Ref<GenerationDefinition>("generation", g.Generation);
                    Ref<SimulationDefinition>("simulation", g.Simulation);
                    if (g.Incidents == null) Error("incidents", "Provide an incident pool.");
                    else foreach (var id in g.Incidents) Ref<IncidentDefinition>("incidents", id);
                    Ref<AbilityDefinition>("enemyFallback", g.EnemyFallback);
                    if (g.EnemyFallback != null && _entries.TryGetValue(g.EnemyFallback, out var fallback) && fallback is AbilityDefinition f && (f.Mana != 0 || f.Target != Expeditions.TargetKind.Enemy || f.Effect != "damage")) Error("enemyFallback", "Fallback must be a free, single-target damaging attack.");
                    foreach (var id in new[] { g.Traveller, g.Villager, g.Healer, g.Beast, g.Raider, g.Caravan }) Ref<CharacterDefinition>("characters", id);
                    foreach (var id in new[] { g.Traveller, g.Villager, g.Healer, g.Beast, g.Raider, g.Caravan })
                        if (id != null && _entries.TryGetValue(id, out var actorEntry) && actorEntry is CharacterDefinition { Ecology: not null }) Error("characters", "Ecology characters spawn through the wildlife pool, not town/site roles.");
                    if (g.Classes == null || g.CommonAbilities == null || g.Bosses == null || g.Recipes == null) { Error("classes", "Game pools cannot be null."); break; }
                    if (g.Classes.Length != 4 || g.CommonAbilities.Length != 3 || g.Bosses.Length == 0) Error("classes", "Provide four classes, three common actions and at least one boss.");
                    foreach (var id in g.Classes) Ref<ClassDefinition>("classes", id);
                    foreach (var id in g.CommonAbilities) Ref<AbilityDefinition>("commonAbilities", id);
                    foreach (var id in g.CommonAbilities.Append(g.EnemyFallback))
                        if (id != null && _entries.TryGetValue(id, out var common) && common is AbilityDefinition { Cooperation: not null }) Error("commonAbilities", "Put cooperative abilities in their own pool, not common actions or enemy fallback.");
                    foreach (var id in g.Bosses) Ref<BossDefinition>("bosses", id);
                    foreach (var id in g.Recipes) Ref<RecipeDefinition>("recipes", id);
                    Cost("inventory", g.Inventory); break;
                case GenerationDefinition g:
                    Range("settlementFinds", g.SettlementFinds, 1, 8); Range("roadDiscoverySpacing", g.RoadDiscoverySpacing, 120, 1000); Range("wildernessFinds", g.WildernessFinds, 0, 128);
                    if (g.Layouts == null || g.Biomes == null) { Error("layouts", "Layout and biome profiles are required."); break; }
                    foreach (string purpose in new[] { "ruin", "mine", "fort", "shrine" })
                        if (!g.Layouts.TryGetValue(purpose, out var layouts) || layouts == null || layouts.Length != World.WorldPlan.Floors || layouts.Any(l => !Layouts.Contains(l)))
                            Error("layouts." + purpose, "Provide a registered layout for each of the four floors.");
                    foreach (var biome in Enum.GetValues<World.Biome>())
                    {
                        if (!g.Biomes.TryGetValue(biome, out var profile) || profile == null) { Error("biomes", "Missing biome: " + biome); continue; }
                        Range("treesPerThousand", profile.TreesPerThousand, 0, 800);
                        if (new[] { profile.RockThreshold, profile.WaterThreshold, profile.WaterElevation }.Any(v => !double.IsFinite(v) || v < -1 || v > 1)) Error("biomes", "Thresholds must be finite in -1..1.");
                    }
                    break;
                case DiscoveryDefinition dsc:
                    if (dsc.Situation is { } situation)
                    {
                        if (situation.Kind is not ("trap" or "spring" or "crossing") || dsc.Action != "encounter") Error("situation.kind", "Use a trap, spring or crossing encounter.");
                        if (!bindings.Contains(situation.ResolvedSprite ?? "")) Error("situation.resolvedSprite", "Provide a bound resolved appearance.");
                        if (situation.Approaches == null || situation.Approaches.Length is < 1 or > 3 || situation.Approaches.Any(a => a == null)
                            || situation.Approaches.Select(a => a.Action).Distinct().Count() != situation.Approaches.Length) Error("situation.approaches", "Provide one to three distinct approaches.");
                        else foreach (var approach in situation.Approaches)
                        {
                            if (approach.Action is not ("disarm" or "repair" or "negotiate") || approach.Action == "disarm" && situation.Kind != "trap"
                                || approach.Action == "negotiate" && situation.Kind != "crossing" || approach.Action == "repair" && situation.Kind == "trap") Error("situation.approaches.action", "Use a registered approach suited to this situation.");
                            if (string.IsNullOrWhiteSpace(approach.Name) || approach.Name.Length > 80 || approach.Description == null || approach.Description.Length > 200 || !bindings.Contains(approach.Sprite ?? "")) Error("situation.approaches.name", "Provide a short readable name, description and sprite.");
                            Range("situation.approaches.work", approach.Work, approach.Action == "negotiate" ? 0 : 10, approach.Action == "negotiate" ? 0 : 300);
                            Cost("situation.approaches.cost", approach.Cost);
                        }
                        void FeatureCells(string field, Expeditions.Cell[]? cells, char terrain, bool required)
                        {
                            if (cells == null || cells.Length > 4 || required && cells.Length < 2 || cells.Distinct().Count() != cells.Length) { Error(field, "Provide two to four distinct feature offsets, or an empty optional list."); return; }
                            int radius = (dsc.Layout?.Length ?? 0) / 2;
                            foreach (var cell in cells)
                            {
                                int x = cell.X + radius, y = cell.Y + radius;
                                if (cell.X == 0 && cell.Y == 0 || dsc.Layout == null || y < 0 || y >= dsc.Layout.Length || dsc.Layout[y] == null || x < 0 || x >= dsc.Layout[y].Length || dsc.Layout[y][x] != terrain) Error(field, "Feature offset must match the indicated terrain in the layout.");
                            }
                            if (cells.Length > 0 && !cells.Any(c => cells.Contains(new(-c.X, -c.Y)))) Error(field, "Include opposite feature offsets so the approach trail preserves one.");
                        }
                        FeatureCells("situation.tunnelWalls", situation.TunnelWalls, '#', true);
                        FeatureCells("situation.bridgeCells", situation.BridgeCells, '~', situation.Kind == "crossing");
                        if (situation.Kind != "crossing" && situation.BridgeCells?.Length > 0) Error("situation.bridgeCells", "Only crossings repair bridges.");
                    }
                    if (dsc.Action is not ("cache" or "camp" or "clue" or "encounter")) Error("action", "Use cache, camp, clue or encounter.");
                    if (string.IsNullOrWhiteSpace(dsc.Text) || dsc.Text.Length > 800 || dsc.Sprite == null) Error("text", "A discovery needs appearance and text of at most 800 characters.");
                    foreach (Match token in Regex.Matches(dsc.Text ?? "", "\\{([^{}]+)\\}"))
                        if (token.Groups[1].Value is not ("town" or "faction" or "witness" or "site" or "guardian" or "direction")) Error("text", "Unknown world reference: " + token.Value);
                    if (dsc.Layout == null || dsc.Layout.Length is < 3 or > 9 || dsc.Layout.Length % 2 == 0
                        || dsc.Layout.Any(r => r == null || r.Length != dsc.Layout.Length || r.Any(c => !".#C~".Contains(c)))
                        || dsc.Layout != null && dsc.Layout.Length > 0 && dsc.Layout[dsc.Layout.Length / 2] is { } middle && middle.Length == dsc.Layout.Length && middle[middle.Length / 2] == '#') Error("layout", "Use an odd square 3..9 of . ground, # wall, ~ water, C camp; keep its centre walkable.");
                    Cost("supplies", dsc.Supplies);
                    if (dsc.Rewards == null || dsc.Enemies == null) { Error("rewards", "Reward/enemy pools cannot be null."); break; }
                    foreach (var id in dsc.Rewards)
                    { Ref<ItemDefinition>("rewards", id); if (id != null && _entries.TryGetValue(id, out var gear) && gear is ItemDefinition item && item.Slot == "none") Error("rewards", "Reward choices must be equipment."); }
                    if (dsc.Rewards.Length is > 0 and < 3) Error("rewards", "Offer at least three equipment alternatives, or no equipment.");
                    if (dsc.Rewards.Distinct().Count() != dsc.Rewards.Length) Error("rewards", "Reward IDs must be distinct.");
                    if (dsc.Enemies.Length > 3 || dsc.Action == "encounter" && dsc.Enemies.Length == 0 || dsc.Action != "encounter" && dsc.Enemies.Length != 0) Error("enemies", "Only encounters have one to three enemies.");
                    foreach (var id in dsc.Enemies)
                    { Ref<CharacterDefinition>("enemies", id); if (id != null && _entries.TryGetValue(id, out var foe) && foe is CharacterDefinition character && (character.ActorKind is not (World.ActorKind.Beast or World.ActorKind.Raider) || character.Ecology != null)) Error("enemies", "Use hostile characters without independent habitat rules."); }
                    break;
                case CampDefinition camp:
                    if (camp.Role is not ("cook" or "treat" or "watch" or "stories")) Error("role", "Use cook, treat, watch or stories.");
                    if (camp.Role == "cook" ? camp.Effect is not ("armor" or "burn-resist" or "bleed-resist" or "focus") : camp.Effect != camp.Role) Error("effect", "Choose a registered meal preparation, or the effect matching the camp role.");
                    if (camp.Sprite == null) Error("sprite", "Camp activities need an icon.");
                    Range("power", camp.Power, 1, 4); Range("work", camp.Work, 10, 300); Range("duration", camp.Duration, 300, 7200);
                    Cost("cost", camp.Cost); break;
                case ResourceDefinition resource:
                    if (resource.Sprite == null || !bindings.Contains(resource.DepletedSprite ?? "")) Error("depletedSprite", "Resources need full and depleted sprite bindings.");
                    if (resource.Biomes == null || resource.Biomes.Length == 0 || resource.Biomes.Any(b => !Enum.IsDefined(b)) || resource.Biomes.Distinct().Count() != resource.Biomes.Length) Error("biomes", "Provide distinct known biomes.");
                    if (resource.Terrain is not (World.Material.Tree or World.Material.Water or World.Material.Rock or World.Material.Grass or World.Material.Floor)) Error("terrain", "Use natural tree, water, rock, grass or cave floor.");
                    Range("chancePerThousand", resource.ChancePerThousand, 1, 400); Range("capacity", resource.Capacity, 1, 12);
                    Range("work", resource.Work, 5, 300); Range("regrowTicks", resource.RegrowTicks, 0, 72000);
                    if (resource.Activity is not ("harvest" or "fish" or "mine")) Error("activity", "Use harvest, fish or mine.");
                    if (resource.Yield == null || resource.Yield.Count == 0) Error("yield", "Provide a resource yield.");
                    Cost("yield", resource.Yield); break;
                case SimulationDefinition m:
                    if (new[] { m.HungerPerTick, m.FatiguePerTick, m.RestPerTick, m.StructureWearPerTick }.Any(v => !double.IsFinite(v) || v < 0 || v > 1)) Error("hungerPerTick", "Rates must be finite in 0..1.");
                    Range("mealAt", m.MealAt, 10, 60); Range("mealReduction", m.MealReduction, 10, 60); Range("starveAt", m.StarveAt, 70, 100);
                    Range("memoryTicks", m.MemoryTicks, 300, 7200); Range("forageWork", m.ForageWork, 5, 300); break;
                case IncidentDefinition e:
                    if (e.Condition is not ("hunger" or "exhaustion" or "injury" or "craft" or "trust")) Error("condition", "Unknown incident condition.");
                    if (e.Effect is not ("memory" or "rest" or "aid")) Error("effect", "Unknown incident handler.");
                    Range("mood", e.Mood, -20, 20); Range("cooldown", e.Cooldown, 300, 7200); break;
                case AbilityDefinition a:
                    if (a.Cooperation is { } cooperation)
                    {
                        Range("cooperation.partnerMana", cooperation.PartnerMana, 1, 20); Range("cooperation.partnerPowerPercent", cooperation.PartnerPowerPercent, 0, 100);
                        Range("mana", a.Mana, 1, 20);
                        if (a.Effect == "damage" ? a.Target != Expeditions.TargetKind.Enemy : a.Effect != "cleanse" || a.Target != Expeditions.TargetKind.AllAllies) Error("cooperation", "Use a single-enemy strike or an all-ally cleanse with partner costs.");
                    }
                    Range("mana", a.Mana, 0, 100); Range("power", a.Power, 0, 200); Range("duration", a.Duration, 1, 12);
                    if (!Effects.Contains(a.Effect)) Error("effect", "Unknown registered effect: " + a.Effect);
                    break;
                case ClassDefinition c:
                    Range("role", c.Role, 0, 3); Range("health", c.Health, 1, 200); Range("mana", c.Mana, 0, 100);
                    Range("armor", c.Armor, 0, 20); Range("speed", c.Speed, 1, 30);
                    if (c.Sprite == null) Error("sprite", "A class needs a sprite.");
                    if (c.Abilities == null || c.Abilities.Length != 3) Error("abilities", "Provide three class abilities.");
                    else foreach (var id in c.Abilities) Ref<AbilityDefinition>("abilities", id);
                    if (c.Abilities != null) foreach (var id in c.Abilities)
                        if (id != null && _entries.TryGetValue(id, out var abilityEntry) && abilityEntry is AbilityDefinition { Cooperation: not null }) Error("abilities", "Cooperative actions belong in the game pool, leaving three independent class actions.");
                    break;
                case CharacterDefinition c:
                    if (c.Ecology is { } ecology)
                    {
                        if (c.ActorKind != World.ActorKind.Beast || c.Names != null || c.Classes?.Length != 0 || c.Interactions?.Length != 0) Error("ecology", "Ecology belongs to beasts without human classes, names or interactions.");
                        if (ecology.Behavior is not ("flee" or "defend" or "roost")) Error("ecology.behavior", "Use flee, defend or roost.");
                        if (ecology.Biomes == null || ecology.Biomes.Length == 0 || ecology.Biomes.Any(b => !Enum.IsDefined(b)) || ecology.Biomes.Distinct().Count() != ecology.Biomes.Length) Error("ecology.biomes", "Provide distinct known biomes.");
                        Range("ecology.notice", ecology.Notice, 1, 8); Range("ecology.leash", ecology.Leash, ecology.Notice + 1, 16);
                        Ref<ResourceDefinition>("ecology.resource", ecology.Resource, optional: ecology.Behavior != "defend");
                        if (ecology.Resource != null && _entries.TryGetValue(ecology.Resource, out var resourceEntry) && resourceEntry is ResourceDefinition { Biomes: not null } habitat
                            && (habitat.Underground != ecology.Underground || ecology.Biomes != null && !ecology.Biomes.Any(habitat.Biomes.Contains))) Error("ecology.resource", "The resource must share the species' floor and biome habitat.");
                        if (ecology.Drops == null || ecology.Drops.Count is < 1 or > 4) Error("ecology.drops", "Provide one to four ingredient drops.");
                        Cost("ecology.drops", ecology.Drops);
                        if (ecology.Drops != null) foreach (int amount in ecology.Drops.Values) Range("ecology.drops", amount, 1, 8);
                        if (ecology.Drops != null) foreach (var id in ecology.Drops.Keys)
                            if (_entries.TryGetValue(id, out var drop) && drop is ItemDefinition item && item.Slot != "none") Error("ecology.drops", "Wildlife drops are ingredients, not equipment.");
                    }
                    Ref<NamesDefinition>("names", c.Names, true);
                    Range("health", c.Health, 1, 2000); Range("mana", c.Mana, 0, 500); Range("power", c.Power, 0, 100);
                    Range("armor", c.Armor, 0, 30); Range("speed", c.Speed, 1, 30);
                    if (c.Classes == null || c.Interactions == null) { Error("classes", "Lists cannot be null."); break; }
                    foreach (var id in c.Classes) Ref<ClassDefinition>("classes", id);
                    foreach (var id in c.Interactions) Ref<InteractionDefinition>("interactions", id);
                    Behaviors("behaviors", c.Behaviors);
                    if (c.Classes.Length == 0 && c.Sprite == null) Error("sprite", "Provide a sprite or a class pool.");
                    break;
                case NamesDefinition n:
                    if (new[] { n.First, n.Last, n.Backgrounds, n.Motivations, n.Traits }.Any(a => a == null || a.Length == 0 || a.Any(string.IsNullOrWhiteSpace)))
                    { Error("first", "All name/profile pools must be nonempty."); break; }
                    foreach (var id in n.Traits) Ref<TraitDefinition>("traits", id); break;
                case TraitDefinition t:
                    if (t.Value is not ("craft" or "discovery" or "fellowship" or "security") || t.Aversion is not ("overwork" or "confinement" or "conflict" or "danger")) Error("value", "Unknown disposition.");
                    Range("workPercent", t.WorkPercent, 50, 150); Range("fatiguePercent", t.FatiguePercent, 50, 150); break;
                case DialogueDefinition dialogue:
                    Ref<InteractionDefinition>("interaction", dialogue.Interaction, optional: true);
                    if (_entries.Values.OfType<DialogueDefinition>().Count(d => d.Interaction == dialogue.Interaction) != 1) Error("interaction", "One dialogue per interaction, plus one greeting without an interaction.");
                    if (dialogue.Variants == null || dialogue.Variants.Length is < 1 or > 16) { Error("variants", "Provide 1..16 variants."); break; }
                    if (!dialogue.Variants.Any(v => v?.When == "always")) Error("variants", "Include an always fallback.");
                    foreach (var v in dialogue.Variants)
                    {
                        if (v == null || v.Player == null || v.Replies == null || v.Replies.Length is < 1 or > 6) { Error("variants", "Provide a player line and 1..6 replies."); continue; }
                        if (v.When is not ("always" or "trusted" or "injured" or "hungry" or "craft" or "security" or "discovery" or "fellowship")) Error("variants.when", "Unknown dialogue condition.");
                        foreach (string line in v.Replies.Prepend(v.Player))
                        {
                            if (line == null || line.Length > 400) { Error("variants", "Lines must be non-null and at most 400 characters."); continue; }
                            foreach (Match token in Regex.Matches(line, "\\{([^{}]+)\\}"))
                                if (!World.WorldDialogue.Tokens.Contains(token.Groups[1].Value)) Error("variants", "Unknown dialogue token: " + token.Value);
                        }
                        if (v.Replies.Any(string.IsNullOrWhiteSpace)) Error("variants.replies", "Replies cannot be blank.");
                    }
                    break;
                case InteractionDefinition i:
                    if (!Actions.Contains(i.Action)) Error("action", "Unknown registered action: " + i.Action);
                    Range("distance", i.Distance, 1, 3); Range("amount", i.Amount, 0, 100); Cost("cost", i.Cost); break;
                case AffixDefinition affix:
                    if (affix.Effect is not ("protect" or "burn-power" or "healing" or "resolve" or "dig" or "mine" or "fish")) Error("effect", "Choose a registered equipment effect.");
                    Range("power", affix.Power, 1, affix.Effect is "dig" or "mine" or "fish" ? 100 : 8);
                    if (affix.Slots == null || affix.Slots.Length == 0 || affix.Slots.Any(s => s is not ("weapon" or "armor" or "charm" or "tool")) || affix.Slots.Distinct().Count() != affix.Slots.Length) Error("slots", "List distinct equipment slots for this effect.");
                    break;
                case ItemDefinition i:
                    if (i.Effects == null || i.AffixPool == null || i.Effects.Length > 2 || i.AffixPool.Length > 16) Error("effects", "Provide up to two fixed effects and sixteen possible affixes.");
                    else
                    {
                        if (i.Effects.Distinct().Count() != i.Effects.Length || i.AffixPool.Distinct().Count() != i.AffixPool.Length) Error("effects", "Effect IDs must be distinct in each pool.");
                        foreach (var pool in new[] { (Field: "effects", Values: i.Effects), (Field: "affixPool", Values: i.AffixPool) })
                            foreach (string id in pool.Values)
                            { Ref<AffixDefinition>(pool.Field, id); if (id != null && _entries.TryGetValue(id, out var e) && e is AffixDefinition a && !(a.Slots?.Contains(i.Slot) ?? false)) Error(pool.Field, "This effect is not allowed on the item's slot."); }
                    }
                    if (string.IsNullOrWhiteSpace(i.Stack) || !Regex.IsMatch(i.Stack, "^[a-z][a-z0-9.-]*$")) Error("stack", "Use a stable inventory key.");
                    if (i.Slot is not ("none" or "weapon" or "armor" or "charm" or "tool")) Error("slot", "Unknown equipment slot.");
                    if (i.WorkBonuses == null) Error("workBonuses", "Work bonuses cannot be null.");
                    else foreach (var (activity, bonus) in i.WorkBonuses)
                    { if (activity is not ("dig" or "build" or "repair" or "craft" or "harvest" or "fish" or "mine")) Error("workBonuses", "Unknown work activity."); Range("workBonuses", bonus, 1, 100); }
                    if (i.Slot == "none" && i.WorkBonuses?.Count > 0) Error("workBonuses", "Only equipped items grant bonuses.");
                    Range("health", i.Health, 0, 100); Range("power", i.Power, 0, 30); Range("armor", i.Armor, 0, 15); Range("speed", i.Speed, 0, 15); Range("value", i.Value, 0, 1000); break;
                case RecipeDefinition r:
                    if (r.Material is not (World.Material.Wall or World.Material.Floor or World.Material.Door or World.Material.Bridge or World.Material.Storage or World.Material.Camp)) Error("material", "Choose a buildable material.");
                    Range("work", r.Work, 1, 1000); Cost("cost", r.Cost);
                    Ref<ItemDefinition>("output", r.Output, true); Range("amount", r.Amount, 1, 20); break;
                case BossDefinition b:
                    Range("health", b.Health, 60, 2000); Range("mana", b.Mana, 0, 500); Range("power", b.Power, 1, 50);
                    Range("armor", b.Armor, 0, 15); Range("speed", b.Speed, 1, 25); Range("phasePercent", b.PhasePercent, 20, 80);
                    if (string.IsNullOrWhiteSpace(b.Family) || string.IsNullOrWhiteSpace(b.Weakness) || string.IsNullOrWhiteSpace(b.Tactics) || b.Sprite == null) Error("tactics", "Bosses need appearance, family, weakness and readable tactics.");
                    Behaviors("behaviors", b.Behaviors, true); Behaviors("enraged", b.Enraged, true);
                    if (b.Minions == null || b.Variants == null || b.Variants.Length == 0) { Error("variants", "Provide a variant pool and minion list."); break; }
                    if (b.Minions.Length > 3) Error("minions", "At most three minions preserve four-member enemy parties.");
                    foreach (var id in b.Minions)
                    { Ref<CharacterDefinition>("minions", id); if (id != null && _entries.TryGetValue(id, out var minion) && minion is CharacterDefinition { Ecology: not null }) Error("minions", "Ecology characters spawn through habitats, not boss groups."); }
                    foreach (var id in b.Variants) Ref<VariantDefinition>("variants", id); break;
                case VariantDefinition v:
                    Range("healthPercent", v.HealthPercent, 70, 150); Range("power", v.Power, -5, 5); Range("armor", v.Armor, -3, 3); Range("speed", v.Speed, -3, 3);
                    Behaviors("behaviors", v.Behaviors); break;
            }
        }
        foreach (var group in All<ClassDefinition>().GroupBy(c => c.Role).Where(g => g.Count() > 1)) Issues.Add(new("$catalog", "role", "Duplicate role: " + group.Key));
        foreach (var group in All<ItemDefinition>().GroupBy(c => c.Stack).Where(g => g.Count() > 1)) Issues.Add(new("$catalog", "stack", "Duplicate inventory key: " + group.Key));
        if (!_entries.TryGetValue("game.vagabonds", out var game) || game is not GameDefinition)
            Issues.Add(new("$catalog", "game", "The runtime entry point game.vagabonds is required."));
        References.Add(new("$runtime", "entryPoint", "game.vagabonds"));
    }
}
