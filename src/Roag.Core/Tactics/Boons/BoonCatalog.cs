using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roag.Core.Tactics;

public sealed record BoonWeaponFormDefinition(ClassKind Class, string AbilityId);

/// <summary>Immutable native catalog and subscription index in the original authored order.</summary>
public sealed class BoonCatalog
{
    private sealed record Document(int SchemaVersion, string SourceVersion, ImmutableArray<string> StarterPool,
        ImmutableArray<string> NormalPool, ImmutableArray<BoonDefinition> Definitions);
    public static ImmutableArray<BoonRarity> Rarities { get; } = [BoonRarity.Common, BoonRarity.Uncommon, BoonRarity.Rare, BoonRarity.Legendary];
    public static JsonSerializerOptions Json { get; } = CreateJsonOptions();
    private static readonly Lazy<BoonCatalog> BuiltIn = new(LoadBuiltIn);
    public static BoonCatalog Default => BuiltIn.Value;
    public ImmutableArray<BoonDefinition> Definitions { get; }
    public ImmutableArray<string> Ids { get; }
    public ImmutableArray<string> StarterPool { get; }
    public ImmutableArray<string> NormalPool { get; }
    private readonly FrozenDictionary<string, BoonDefinition> _definitions;
    private readonly FrozenDictionary<BoonTrigger, ImmutableArray<BoonDefinition>> _subscriptions;

    public BoonCatalog(IEnumerable<BoonDefinition> definitions, IEnumerable<string> starterPool, IEnumerable<string> normalPool)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        Definitions = definitions.ToImmutableArray();
        if (Definitions.IsEmpty || Definitions.Any(d => d is null)) throw new InvalidDataException("Missing boon definitions.");
        foreach (var definition in Definitions) BoonDefinitionValidation.Validate(definition);
        Ids = Definitions.Select(d => d.Id).ToImmutableArray();
        if (Ids.Distinct(StringComparer.Ordinal).Count() != Ids.Length) throw new InvalidDataException("Duplicate boon identities.");
        _definitions = Definitions.ToFrozenDictionary(d => d.Id, StringComparer.Ordinal);
        StarterPool = ValidatePool(starterPool); NormalPool = ValidatePool(normalPool);
        foreach (var definition in Definitions)
            if (definition.Incompatible.Any(id => id == definition.Id || !_definitions.ContainsKey(id)))
                throw new InvalidDataException($"Invalid boon incompatibility: {definition.Id}.");
        _subscriptions = Enum.GetValues<BoonTrigger>().ToFrozenDictionary(trigger => trigger,
            trigger => Definitions.Where(d => d.Triggers.Contains(trigger)).ToImmutableArray());
    }

    private ImmutableArray<string> ValidatePool(IEnumerable<string> pool)
    {
        ArgumentNullException.ThrowIfNull(pool);
        var entries = pool.ToImmutableArray();
        if (entries.Distinct(StringComparer.Ordinal).Count() != entries.Length || entries.Any(id => id is null || !_definitions.ContainsKey(id)))
            throw new InvalidDataException("Unknown or duplicate boon pool entry.");
        return entries;
    }

    public BoonDefinition Get(string id) => _definitions.TryGetValue(id, out var definition) ? definition
        : throw new ArgumentException("Unknown boon: " + id, nameof(id));
    public bool Contains(string id) => id is not null && _definitions.ContainsKey(id);
    public ImmutableArray<BoonDefinition> Subscribers(BoonTrigger trigger) => _subscriptions[trigger];
    public bool Conflicts(string first, string second) => first != second
        && (Get(first).Incompatible.Contains(second) || Get(second).Incompatible.Contains(first));

    public static BoonCatalog Load(string json)
    {
        try
        {
            var document = JsonSerializer.Deserialize<Document>(json, Json) ?? throw new InvalidDataException("Missing boon catalog.");
            if (document.SchemaVersion != 1 || string.IsNullOrWhiteSpace(document.SourceVersion)
                || document.Definitions.IsDefault || document.StarterPool.IsDefault || document.NormalPool.IsDefault)
                throw new InvalidDataException("Invalid boon catalog header.");
            return new(document.Definitions, document.StarterPool, document.NormalPool);
        }
        catch (JsonException e) { throw new InvalidDataException("Invalid native boon content.", e); }
    }

    private static BoonCatalog LoadBuiltIn()
    {
        using var stream = typeof(BoonCatalog).Assembly.GetManifestResourceStream("Roag.Tactics.Boons.json")
            ?? throw new InvalidDataException("Missing embedded boon catalog.");
        using var reader = new StreamReader(stream);
        return Load(reader.ReadToEnd());
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true, MaxDepth = 64
        };
        // Authored event names are PascalCase; operation names and rarities are snake_case.
        options.Converters.Add(new JsonStringEnumConverter<BoonTrigger>(allowIntegerValues: false));
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    public static BoonWeaponFormDefinition WeaponForm(BoonWeaponForm form)
    {
        var (kind, special) = form switch
        {
            BoonWeaponForm.PommelReturn => (ClassKind.Adventurer, false),
            BoonWeaponForm.NeedleFlight => (ClassKind.Ranger, false),
            BoonWeaponForm.HollowSigil => (ClassKind.Arcanist, true),
            BoonWeaponForm.Backswing => (ClassKind.Berserker, true),
            BoonWeaponForm.DistantBastion => (ClassKind.Sentinel, true),
            BoonWeaponForm.VolatileVial => (ClassKind.Alchemist, false),
            BoonWeaponForm.SplitThrust => (ClassKind.Duelist, false),
            _ => throw new ArgumentOutOfRangeException(nameof(form))
        };
        var definition = ClassCatalog.Get(kind);
        return new(kind, special ? definition.SpecialAbilityId : definition.BasicAbilityId);
    }
}
