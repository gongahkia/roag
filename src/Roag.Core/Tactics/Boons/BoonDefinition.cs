using System.Collections.Immutable;

namespace Roag.Core.Tactics;

public enum BoonRarity { Common, Uncommon, Rare, Legendary }
public enum BoonStackPolicy { AddPower, ChanceOverflow }
public enum BoonScopeKind { Owner, Nearby, Radius, Floor }
public enum BoonTrigger
{
    HitConfirmed, EntityKilled, DamageTaken, DamageBlocked, TileEntered, AttackPerformed,
    WindupStarted, Moved, CurrencyCollected, BoonActivated, TerrainTriggered, TerrainBroken,
    SupplyCollected, ChestClaimed, Waited, StatusApplied, StatusCleansed, DoorOpened,
    SecretDiscovered, BeaconReturned, SpiritPositioned, SentryDeployed, StormGrounded, MirrorReflected
}
public enum BoonEffectKind
{
    DamageNearest, DamageSource, Area, Echo, ApplyStatus, HealOwner, DamageTarget,
    AdjacentHostiles, PathAdjacentHostiles, Prime, SpeedBoost, Ward, MarkDamage,
    StatusNeighbors, CleanseOwner, RefundPurchase, EnrichDrop, Pattern, DisplaceTarget,
    StunTarget, WardToHeal, ConsumeBurning, GatherPickups, ExtendBurning
}
public enum BoonPredicateKind
{
    SourceOwner, TargetOwner, TargetHostile, SourceNotOwner, Positive, SourceTargetSameFaction,
    PairAdjacentDamage, SourceId, TargetId, SourceTag, TargetTag, SourceFaction, TargetFaction,
    DirectAttack, TargetWasFull, AdjacentHostiles, BurningPair, SourceHostile, SourceInRadius,
    OwnerHpPercent, DistinctChainGroup, AbilityId, UniqueWindup, OtherBoon, EventKind,
    FromTerrain, PathTerrain, SourceOnTerrain, TargetNearTerrain, TerrainId, DamageType, Primed,
    SpecialAttack, TargetHpPercentBefore, OwnerInLockedCells, OverlappingWindups, ApproachingOwner,
    AlignedHostiles, EventDistance, VisibleSource, VisibleEvent, SupplyKind, SupplyExcess,
    EventAmount, RewardedDrop, RollFace, UniqueAction, PatternAxis, ActualDash, RegularTarget,
    BasicAttack, CooldownReady, OwnerMissingHp, ActiveWard, BurningTarget, OwnerAxis,
    BackToWall, EventStatus, TargetEntityHostile, OnSpecialTerrain
}
public enum BoonPattern { Lance, Rook, Bishop, Knight, Crown, Crosswind, Fan, Backtrail }
public enum BoonAxis { Cardinal, Diagonal }
public enum BoonNeighborhood { Cardinal, Eight }
public enum BoonEventField { Amount, ActualGain, Price, TargetId, BoonId }
public enum BoonScale { AdjacentHostiles, SupplyExcess }
public enum BoonDisplacement { Pull, Push }
public enum BoonTarget { Nearest }
public enum BoonStatus { Burning }
public enum BoonPreviewTrigger { Hit, Kill, Dash, Wait, Return }
public enum BoonModifierKind
{
    ForwardCell, AddDamage, LineSecondCell, PathStrike, BranchNear, LineExtend,
    AreaExpand, StatusChain, Shockwave, DashPrime, WeaponForm
}
public enum BoonWeaponForm { PommelReturn, NeedleFlight, HollowSigil, Backswing, DistantBastion, VolatileVial, SplitThrust }

public sealed record BoonStrength(long Chance, int Power);
public sealed record BoonScope(BoonScopeKind Kind, int? Radius = null);
public sealed record BoonAttackPayment(int Hp);
public sealed record BoonPatternPreview(BoonPreviewTrigger Trigger, BoonAxis? Axis = null, bool? Special = null);
public sealed record BoonModifier(string AbilityId, BoonModifierKind Kind, BoonWeaponForm? Form = null);

public sealed record BoonEffect
{
    public required BoonEffectKind Kind { get; init; }
    public DamageElement? DamageType { get; init; }
    public double? ProcCoefficient { get; init; }
    public int? Radius { get; init; }
    public int? Delay { get; init; }
    public int? Interval { get; init; }
    public int? Ticks { get; init; }
    public int? Duration { get; init; }
    public int? Cap { get; init; }
    public int? Threshold { get; init; }
    public int? Cooldown { get; init; }
    public BoonStatus? Status { get; init; }
    public BoonScale? Scale { get; init; }
    public BoonPredicate? When { get; init; }
    public bool? ConsumePrime { get; init; }
    public BoonPattern? Pattern { get; init; }
    public BoonDisplacement? Mode { get; init; }
    public bool? CollisionDamage { get; init; }
    public bool? ClearPath { get; init; }
    public BoonTarget? Target { get; init; }
}

/// <summary>Authored content only. Behavior is implemented by native C# handlers.</summary>
public sealed record BoonDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ImmutableArray<string> Tags { get; init; }
    public required ImmutableArray<BoonTrigger> Triggers { get; init; }
    public required BoonScope Scope { get; init; }
    public BoonPredicate? Predicate { get; init; }
    public required BoonStackPolicy StackPolicy { get; init; }
    public required ImmutableDictionary<BoonRarity, BoonStrength> Parameters { get; init; }
    public required ImmutableArray<BoonEffect> Effects { get; init; }
    public ImmutableArray<BoonModifier> Modifiers { get; init; } = [];
    public ImmutableArray<string> Incompatible { get; init; } = [];
    public ImmutableArray<string> EligibleClasses { get; init; } = [];
    public string? Unit { get; init; }
    public BoonPatternPreview? PatternPreview { get; init; }
    public BoonAttackPayment? AttackPayment { get; init; }

    public bool Allows(ClassKind? kind) => EligibleClasses.IsEmpty || kind is { } value && EligibleClasses.Contains(ClassCatalog.Get(value).Id);
}
