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
    public static readonly string[] Actions = ["recruit", "trade", "heal", "converse", "share-meal"];
    public static readonly string[] Kinds = ["ability", "boss", "character", "class", "game", "generation", "incident", "interaction", "item", "names", "recipe", "simulation", "trait", "variant"];
    public static readonly string[] Layouts = ["overlapping-wfc", "caverns", "bsp-workings", "bsp-fort", "ring-sanctuary"];
    private readonly Dictionary<string, ContentDefinition> _entries = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _sources = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, ContentDefinition> Entries => _entries;
    public List<ContentIssue> Issues { get; } = [];
    public List<ContentReference> References { get; } = [];
    public string Hash { get; private set; } = "";
    public ClassDefinition[] Classes => All<ClassDefinition>().OrderBy(c => c.Role).ToArray();
    public GameDefinition Game => Get<GameDefinition>("game.vagabonds");
    public IEnumerable<T> All<T>() where T : ContentDefinition => _entries.Values.OfType<T>().OrderBy(e => e.Id, StringComparer.Ordinal);
    public T Get<T>(string id) where T : ContentDefinition => _entries.TryGetValue(id, out var entry) && entry is T typed ? typed
        : throw new InvalidDataException($"Missing {typeof(T).Name}: {id}.");
    public string Source(string id) => _sources.GetValueOrDefault(id, "");
    public static bool ValidId(string? id) => !string.IsNullOrWhiteSpace(id) && Regex.IsMatch(id, "^[a-z][a-z0-9-]*\\.[a-z0-9][a-z0-9.-]*$");
    public static string Kind(ContentDefinition d) => d switch
    {
        AbilityDefinition => "ability", BossDefinition => "boss", CharacterDefinition => "character", ClassDefinition => "class",
        InteractionDefinition => "interaction", ItemDefinition => "item", NamesDefinition => "names", RecipeDefinition => "recipe",
        TraitDefinition => "trait", VariantDefinition => "variant", GameDefinition => "game", GenerationDefinition => "generation", SimulationDefinition => "simulation", IncidentDefinition => "incident", _ => throw new ArgumentException("Unknown content type.")
    };
    public static ContentCatalog LoadDirectory(string root)
    {
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
        return Load(Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories).Where(p => Path.GetFileName(p) != "schema.json")
            .Order(StringComparer.Ordinal).Select(p => (File: Path.GetRelativePath(root, p), Text: File.ReadAllText(p))));
    }
    public static ContentCatalog Load(IEnumerable<(string File, string Text)> documents)
    {
        var catalog = new ContentCatalog(); var canonical = new StringBuilder();
        foreach (var (file, text) in documents.OrderBy(d => d.File, StringComparer.Ordinal))
        {
            try
            {
                var entry = JsonSerializer.Deserialize<ContentDefinition>(text, Json) ?? throw new JsonException("Empty content definition.");
                if (!ValidId(entry.Id))
                { catalog.Issues.Add(new(file, "id", "Use a stable kind.name identifier in lowercase.")); continue; }
                if (!entry.Id.StartsWith(Kind(entry) + ".", StringComparison.Ordinal)) catalog.Issues.Add(new(file, "id", "Identifier prefix must match kind."));
                if (!catalog._entries.TryAdd(entry.Id, entry)) catalog.Issues.Add(new(file, "id", "Duplicate identifier: " + entry.Id));
                else { catalog._sources[entry.Id] = file; canonical.Append(JsonSerializer.Serialize(entry, Json)); }
            }
            catch (Exception e) when (e is JsonException or NotSupportedException or InvalidOperationException)
            { catalog.Issues.Add(new(file, "$", e.Message)); }
        }
        catalog.Hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
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
                    if (r.Ability != null && _entries.TryGetValue(r.Ability, out var entry) && entry is AbilityDefinition ability && ability.Effect is "flee" or "potion") Error(field, "Enemy rules cannot use party-only retreat or inventory medicine.");
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
                    Ref<GenerationDefinition>("generation", g.Generation);
                    Ref<SimulationDefinition>("simulation", g.Simulation);
                    if (g.Incidents == null) Error("incidents", "Provide an incident pool.");
                    else foreach (var id in g.Incidents) Ref<IncidentDefinition>("incidents", id);
                    Ref<AbilityDefinition>("enemyFallback", g.EnemyFallback);
                    if (g.EnemyFallback != null && _entries.TryGetValue(g.EnemyFallback, out var fallback) && fallback is AbilityDefinition f && (f.Mana != 0 || f.Target != Expeditions.TargetKind.Enemy || f.Effect != "damage")) Error("enemyFallback", "Fallback must be a free, single-target damaging attack.");
                    foreach (var id in new[] { g.Traveller, g.Villager, g.Healer, g.Beast, g.Raider, g.Caravan }) Ref<CharacterDefinition>("characters", id);
                    if (g.Classes == null || g.CommonAbilities == null || g.Bosses == null || g.Recipes == null) { Error("classes", "Game pools cannot be null."); break; }
                    if (g.Classes.Length != 4 || g.CommonAbilities.Length != 3 || g.Bosses.Length == 0) Error("classes", "Provide four classes, three common actions and at least one boss.");
                    foreach (var id in g.Classes) Ref<ClassDefinition>("classes", id);
                    foreach (var id in g.CommonAbilities) Ref<AbilityDefinition>("commonAbilities", id);
                    foreach (var id in g.Bosses) Ref<BossDefinition>("bosses", id);
                    foreach (var id in g.Recipes) Ref<RecipeDefinition>("recipes", id);
                    Cost("inventory", g.Inventory); break;
                case GenerationDefinition g:
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
                case SimulationDefinition m:
                    if (new[] { m.HungerPerTick, m.FatiguePerTick, m.RestPerTick, m.StructureWearPerTick }.Any(v => !double.IsFinite(v) || v < 0 || v > 1)) Error("hungerPerTick", "Rates must be finite in 0..1.");
                    Range("mealAt", m.MealAt, 10, 60); Range("mealReduction", m.MealReduction, 10, 60); Range("starveAt", m.StarveAt, 70, 100);
                    Range("memoryTicks", m.MemoryTicks, 300, 7200); Range("forageWork", m.ForageWork, 5, 300); break;
                case IncidentDefinition e:
                    if (e.Condition is not ("hunger" or "exhaustion" or "injury" or "craft" or "trust")) Error("condition", "Unknown incident condition.");
                    if (e.Effect is not ("memory" or "rest" or "aid")) Error("effect", "Unknown incident handler.");
                    Range("mood", e.Mood, -20, 20); Range("cooldown", e.Cooldown, 300, 7200); break;
                case AbilityDefinition a:
                    Range("mana", a.Mana, 0, 100); Range("power", a.Power, 0, 200); Range("duration", a.Duration, 1, 12);
                    if (!Effects.Contains(a.Effect)) Error("effect", "Unknown registered effect: " + a.Effect);
                    break;
                case ClassDefinition c:
                    Range("role", c.Role, 0, 3); Range("health", c.Health, 1, 200); Range("mana", c.Mana, 0, 100);
                    Range("armor", c.Armor, 0, 20); Range("speed", c.Speed, 1, 30);
                    if (c.Sprite == null) Error("sprite", "A class needs a sprite.");
                    if (c.Abilities == null || c.Abilities.Length != 3) Error("abilities", "Provide three class abilities.");
                    else foreach (var id in c.Abilities) Ref<AbilityDefinition>("abilities", id);
                    break;
                case CharacterDefinition c:
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
                case InteractionDefinition i:
                    if (!Actions.Contains(i.Action)) Error("action", "Unknown registered action: " + i.Action);
                    Range("distance", i.Distance, 1, 3); Range("amount", i.Amount, 0, 100); Cost("cost", i.Cost); break;
                case ItemDefinition i:
                    if (string.IsNullOrWhiteSpace(i.Stack) || !Regex.IsMatch(i.Stack, "^[a-z][a-z0-9.-]*$")) Error("stack", "Use a stable inventory key.");
                    if (i.Slot is not ("none" or "weapon" or "armor" or "charm")) Error("slot", "Unknown equipment slot.");
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
                    foreach (var id in b.Minions) Ref<CharacterDefinition>("minions", id);
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
