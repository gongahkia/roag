using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using Vagabonds.Core.Content;

namespace Vagabonds.Client;

/// <summary>Client choreography only. No profile changes a skill, a save, or either simulation RNG.</summary>
internal sealed record CombatVisualProfile(string Id, string Family, string Sprite, Color Tint,
    double Anticipation, double Travel, double Impact, double Recovery, string Sound)
{
    public double Duration => Anticipation + Travel + Impact + Recovery;
}

internal sealed class CombatVisualProfiles
{
    private sealed class Document
    {
        public int Version { get; set; }
        public string Notes { get; set; } = "";
        public string DefaultProfile { get; set; } = "";
        public string CooperativeProfile { get; set; } = "";
        public ProfileData[] Profiles { get; set; } = [];
        public Dictionary<string, string> Abilities { get; set; } = [];
        public Dictionary<string, string> Effects { get; set; } = [];
    }
    private sealed class ProfileData
    {
        public string Id { get; set; } = "";
        public string Family { get; set; } = "";
        public string Sprite { get; set; } = "";
        public string Color { get; set; } = "";
        public double Anticipation { get; set; }
        public double Travel { get; set; }
        public double Impact { get; set; }
        public double Recovery { get; set; }
        public string Sound { get; set; } = "";
    }
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    private static readonly Lazy<CombatVisualProfiles> Embedded = new(() =>
    {
        using var resource = typeof(CombatVisualProfiles).Assembly.GetManifestResourceStream("Vagabonds.CombatPresentation.json")
            ?? throw new InvalidDataException("Missing embedded combat presentation profiles.");
        using var reader = new StreamReader(resource);
        return Load(reader.ReadToEnd());
    });
    private static readonly HashSet<string> Families = new(StringComparer.Ordinal)
        { "melee", "projectile", "spell", "heal", "guard", "dread", "cooperative", "retreat" };
    private readonly Dictionary<string, CombatVisualProfile> _profiles;
    private readonly Dictionary<string, string> _abilities, _effects;
    private readonly string _defaultProfile, _cooperativeProfile;
    public static CombatVisualProfiles Default => Embedded.Value;
    public IReadOnlyCollection<CombatVisualProfile> Profiles => _profiles.Values;
    public IEnumerable<string> AbilityIds => _abilities.Keys;
    public string Hash { get; }

    private CombatVisualProfiles(Document document, Dictionary<string, CombatVisualProfile> profiles, string hash)
    {
        _profiles = profiles; _abilities = document.Abilities; _effects = document.Effects;
        _defaultProfile = document.DefaultProfile; _cooperativeProfile = document.CooperativeProfile; Hash = hash;
    }

    public CombatVisualProfile Resolve(string? abilityId, string? effect = null, bool cooperative = false)
    {
        if (abilityId != null && _abilities.TryGetValue(abilityId, out string? specific)) return _profiles[specific];
        // Unknown/new abilities remain playable. Healing keeps its visual meaning even when assisted.
        if (cooperative && effect is not ("heal" or "potion" or "cleanse" or "calm")) return _profiles[_cooperativeProfile];
        return _profiles[effect != null && _effects.TryGetValue(effect, out string? fallback) ? fallback : _defaultProfile];
    }

    /// <summary>Loads and validates authoring data without allocating textures or opening an audio device.</summary>
    public static CombatVisualProfiles Load(string text, ContentCatalog? content = null)
    {
        content ??= ContentCatalog.Default;
        Document document;
        try
        {
            using var syntax = JsonDocument.Parse(text);
            RejectDuplicateProperties(syntax.RootElement, "$", 0);
            document = JsonSerializer.Deserialize<Document>(text, Json) ?? throw new InvalidDataException("Empty combat presentation document.");
        }
        catch (JsonException e) { throw new InvalidDataException("Invalid combat presentation JSON: " + e.Message, e); }
        if (document.Version != 1) throw new InvalidDataException("combat presentation version must be 1.");
        if (document.Profiles == null || document.Profiles.Length is < 1 or > 128 || document.Abilities == null || document.Effects == null)
            throw new InvalidDataException("profiles, abilities and effects must be non-null; provide 1..128 profiles.");

        using var bindings = JsonDocument.Parse(typeof(ContentCatalog).Assembly.GetManifestResourceStream("Vagabonds.ContentSprites.json")!);
        var sprites = bindings.RootElement.GetProperty("bindings").EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        var profiles = new Dictionary<string, CombatVisualProfile>(StringComparer.Ordinal);
        foreach (var entry in document.Profiles)
        {
            if (entry == null || !ContentCatalog.ValidId(entry.Id) || !entry.Id.StartsWith("combat.", StringComparison.Ordinal))
                throw new InvalidDataException("Profile id must be a stable combat.name identifier.");
            if (entry.Family == null || !Families.Contains(entry.Family)) throw new InvalidDataException($"{entry.Id}: unknown family '{entry.Family}'.");
            if (entry.Sprite == null || !sprites.Contains(entry.Sprite)) throw new InvalidDataException($"{entry.Id}: unknown sprite '{entry.Sprite}'.");
            if (entry.Sound == null || !CombatSoundSynthesis.Cues.Contains(entry.Sound)) throw new InvalidDataException($"{entry.Id}: unknown sound '{entry.Sound}'.");
            if (entry.Color == null || entry.Color.Length != 7 || entry.Color[0] != '#' ||
                !uint.TryParse(entry.Color.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint rgb))
                throw new InvalidDataException($"{entry.Id}: color must be #RRGGBB.");
            double[] timings = [entry.Anticipation, entry.Travel, entry.Impact, entry.Recovery];
            if (timings.Any(value => !double.IsFinite(value) || value < .04 || value > .6) || timings.Sum() > 1.2)
                throw new InvalidDataException($"{entry.Id}: each beat must be 0.04..0.6 seconds; total must not exceed 1.2 seconds.");
            if (!profiles.TryAdd(entry.Id, new(entry.Id, entry.Family, entry.Sprite,
                    new Color((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb),
                    entry.Anticipation, entry.Travel, entry.Impact, entry.Recovery, entry.Sound)))
                throw new InvalidDataException("Duplicate profile: " + entry.Id);
        }
        void RequireProfile(string? id, string source)
        {
            if (id == null || !profiles.ContainsKey(id)) throw new InvalidDataException($"{source}: unknown combat profile '{id}'.");
        }
        RequireProfile(document.DefaultProfile, "defaultProfile"); RequireProfile(document.CooperativeProfile, "cooperativeProfile");
        foreach (var (id, profile) in document.Abilities)
        {
            if (!content.Entries.TryGetValue(id, out var ability) || ability is not AbilityDefinition)
                throw new InvalidDataException($"abilities.{id}: unknown ability.");
            RequireProfile(profile, "abilities." + id);
        }
        foreach (var (effect, profile) in document.Effects)
        {
            if (!ContentCatalog.Effects.Contains(effect)) throw new InvalidDataException($"effects.{effect}: unknown registered effect.");
            RequireProfile(profile, "effects." + effect);
        }
        foreach (string effect in ContentCatalog.Effects)
            if (!document.Effects.ContainsKey(effect)) throw new InvalidDataException($"effects.{effect}: missing fallback.");
        string canonical = JsonSerializer.Serialize(document, Json);
        return new(document, profiles, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))));
    }

    private static void RejectDuplicateProperties(JsonElement value, string path, int depth)
    {
        if (depth > 16) throw new InvalidDataException("Combat presentation nesting exceeds 16 levels.");
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new InvalidDataException($"{path}: duplicate property '{property.Name}'.");
                RejectDuplicateProperties(property.Value, path + "." + property.Name, depth + 1);
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) RejectDuplicateProperties(item, path + "[]", depth + 1);
    }
}
