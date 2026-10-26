using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed record BoonActivationKey(long Owner, string Boon);

/// <summary>Immutable causal facts, captured when an event happens rather than read from a later target state.</summary>
public sealed record BoonEvent
{
    public required long Id { get; init; }
    public required long ChainId { get; init; }
    public required BoonTrigger Kind { get; init; }
    public long? RootActionId { get; init; }
    public long? SourceId { get; init; }
    public long? TargetId { get; init; }
    public string? SourceFaction { get; init; }
    public string? TargetFaction { get; init; }
    public string? TargetDefinitionId { get; init; }
    public WorldPosition? Position { get; init; }
    public WorldPosition? From { get; init; }
    public WorldPosition? SourcePosition { get; init; }
    public WorldPosition? PatternOrigin { get; init; }
    public ImmutableArray<WorldPosition> Path { get; init; } = [];
    public ImmutableArray<WorldPosition> TargetCells { get; init; } = [];
    public ImmutableArray<string> SourceTags { get; init; } = [];
    public ImmutableArray<string> TargetTags { get; init; } = [];
    public ImmutableArray<BoonActivationKey> Ancestry { get; init; } = [];
    public string? AbilityId { get; init; }
    public string? BoonId { get; init; }
    public string? TerrainId { get; init; }
    public DamageElement? DamageType { get; init; }
    public BoonStatus? Status { get; init; }
    public string? SupplyKind { get; init; }
    public int? HpBefore { get; init; }
    public int? TargetMaxHp { get; init; }
    public int? SourceHpAtAttack { get; init; }
    public int? SourceMaxHpAtAttack { get; init; }
    public int? ActualLoss { get; init; }
    public int? Amount { get; init; }
    public int? ActualGain { get; init; }
    public int? Price { get; init; }
    public int? AdjacentHostileCount { get; init; }
    public int? AdjacentBurningCount { get; init; }
    public int? FateFace { get; init; }
    public long? WindupId { get; init; }
    public double ProcCoefficient { get; init; } = 1;
    public bool? TargetBurning { get; init; }
    public bool DirectAttack { get; init; }
    public bool WallBacked { get; init; }
    public bool RewardExempt { get; init; }
    public bool FateMiss { get; init; }
    public bool OathSharedOnly { get; init; }
    public bool ClassGenerated { get; init; }
    [JsonIgnore] public WorldPosition? ObservedPosition => Position ?? From ?? SourcePosition;
}

/// <summary>A transient view over the persistent actor, not another health/position authority.</summary>
public sealed record BoonActorView(Actor Actor, string Faction, bool RegularTarget = false,
    IReadOnlyList<WorldPosition>? LockedCells = null);

/// <summary>Explicit live queries. Visibility is evaluated for the boon owner, never a shared party view.</summary>
public sealed record BoonEvaluationContext(long Clock, IReadOnlyList<BoonActorView> Actors,
    Func<WorldPosition, string?> Terrain, Func<WorldPosition, string?> SpecialTerrain,
    Func<long, WorldPosition, bool> Visible, Func<string, int?> CurrencyDrop, bool LastActionWasDash = false);
