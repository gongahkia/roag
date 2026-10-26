using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roag.Core.Tactics;

public enum EnemyBrainKind { Guard, Spitter, Skitter, Warden, Pursuer, SporeHerald, CanalLancer,
    CinderBellkeeper, HollowRegent, BiomeElite, ExpansionRegular, Squad, IdeaRegular }

public sealed record EnemyDefinition(string Id, string Name, EnemyBrainKind Ai, int Hp, int Damage, int Defense,
    int Speed, int Windup, int Recovery, int MoveCost, int Range, int XpReward, int CurrencyDrop,
    int MinRange = 1, bool Invulnerable = false, bool SpecialEncounter = false)
{
    [JsonIgnore] public bool Regular => !Invulnerable && !SpecialEncounter
        && Ai is not (EnemyBrainKind.Warden or EnemyBrainKind.Pursuer or EnemyBrainKind.HollowRegent or EnemyBrainKind.BiomeElite);
    [JsonIgnore] public bool PullImmune => Invulnerable || Ai is EnemyBrainKind.Warden or EnemyBrainKind.HollowRegent or EnemyBrainKind.BiomeElite;

    public ActorTactics CreateTactics(bool guaranteedHealthDrop = false, bool rewardExempt = false) => new()
    {
        DefinitionId = Id, InnateStats = new(Hp, Damage, Damage, Defense, Speed, 0, 150),
        MoveCost = MoveCost, Invulnerable = Invulnerable, RegularEnemy = Regular, ImmuneToPull = PullImmune,
        ExperienceReward = XpReward, CurrencyDrop = CurrencyDrop, HealthDrop = guaranteedHealthDrop,
        RewardExempt = rewardExempt, Enemy = new()
    };

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Id) || !Id.StartsWith("core:enemy/", StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(Name) || !Enum.IsDefined(Ai) || Hp <= 0 || Damage < 0 || Defense < 0 || Speed <= 0
            || Windup <= 0 || Recovery <= 0 || MoveCost <= 0 || Range <= 0 || MinRange < 1 || MinRange > Range
            || XpReward < 0 || CurrencyDrop < 0)
            throw new InvalidDataException("Invalid authored enemy definition.");
    }
}

/// <summary>Persistent tactical decision memory; no duplicate health, location or scheduled work.</summary>
public sealed class EnemyMemory
{
    public long Attacks { get; set; }
    public bool PhaseTwo { get; set; }
    public Facing? LastMovement { get; set; }
    public bool Alerted { get; set; }
    public bool Retaliate { get; set; }
    public int Summons { get; set; }
    public long SummonedBy { get; set; }

    internal void Validate()
    {
        if (Attacks < 0 || Summons is < 0 or > 3 || SummonedBy < 0
            || LastMovement is { } facing && (!Enum.IsDefined(facing) || !AttackGeometry.Cardinal(facing)))
            throw new InvalidDataException("Invalid enemy decision memory.");
    }
}

/// <summary>Authored content only; behavior is implemented by native enemy controllers.</summary>
public sealed class EnemyCatalog
{
    private sealed record Document(int SchemaVersion, string SourceVersion, ImmutableArray<EnemyDefinition> Definitions);
    private readonly FrozenDictionary<string, EnemyDefinition> _definitions;
    private static readonly Lazy<EnemyCatalog> BuiltIn = new(LoadBuiltIn);
    public static EnemyCatalog Default => BuiltIn.Value;
    public ImmutableArray<EnemyDefinition> Definitions { get; }

    public EnemyCatalog(IEnumerable<EnemyDefinition> definitions)
    {
        Definitions = definitions.ToImmutableArray();
        if (Definitions.IsEmpty || Definitions.Any(d => d is null) || Definitions.Select(d => d.Id).Distinct(StringComparer.Ordinal).Count() != Definitions.Length)
            throw new InvalidDataException("Missing or duplicate enemy definitions.");
        foreach (var definition in Definitions) definition.Validate();
        _definitions = Definitions.ToFrozenDictionary(d => d.Id, StringComparer.Ordinal);
    }

    public bool Contains(string id) => id is not null && _definitions.ContainsKey(id);
    public EnemyDefinition Get(string id) => _definitions.TryGetValue(id, out var definition) ? definition
        : throw new ArgumentException("Unknown enemy: " + id, nameof(id));

    public static EnemyCatalog Load(string json)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectNullableAnnotations = true, RespectRequiredConstructorParameters = true
        };
        options.Converters.Add(new JsonStringEnumConverter<EnemyBrainKind>(JsonNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
        try
        {
            var document = JsonSerializer.Deserialize<Document>(json, options) ?? throw new InvalidDataException("Missing enemy catalog.");
            if (document.SchemaVersion != 1 || string.IsNullOrWhiteSpace(document.SourceVersion) || document.Definitions.IsDefault)
                throw new InvalidDataException("Invalid enemy catalog header.");
            return new(document.Definitions);
        }
        catch (JsonException e) { throw new InvalidDataException("Invalid native enemy content.", e); }
    }

    private static EnemyCatalog LoadBuiltIn()
    {
        using var stream = typeof(EnemyCatalog).Assembly.GetManifestResourceStream("Roag.Tactics.Enemies.json")
            ?? throw new InvalidDataException("Missing embedded enemy catalog.");
        using var reader = new StreamReader(stream);
        return Load(reader.ReadToEnd());
    }
}
