using System.Text.Json.Serialization;
using Roag.Core.World;

namespace Roag.Core.Tactics;

/// <summary>Combat metadata attached to a persistent world actor; never stores another position or HP.</summary>
public sealed class ActorTactics
{
    public CharacterBuild? Build { get; set; }
    public BuildStats? InnateStats { get; set; }
    [JsonIgnore] public BuildStats Stats => Build?.Stats ?? (InnateStats is { } innate
        ? Enemy is { PhaseTwo: true } ? innate with { Speed = checked(innate.Speed + EnemyPhases.SpeedBonus) } : innate
        : throw new InvalidDataException("Missing tactical stats."));
    public Facing Facing { get; set; } = Facing.South;
    public bool Guarding { get; set; }
    public int BleedTurns { get; set; }
    public int MoveCost { get; set; } = 100;
    public long SnaredBy { get; set; }
    public int EnemySnareDelay { get; set; }
    public int WebRecoveryDelay { get; set; }
    public bool ImmuneToPull { get; set; }
    public bool Invulnerable { get; set; }
    public bool RegularEnemy { get; set; }
    public bool RewardExempt { get; set; }
    public string DefinitionId { get; set; } = "";
    public int CurrencyDrop { get; set; }
    public bool HealthDrop { get; set; }
    public EnemyMemory? Enemy { get; set; }
    public int ExperienceReward { get; set; }
    public bool RewardsClaimed { get; set; }
    public int ExperienceFraction { get; set; }
    public bool AutoEat { get; set; } = true;
    public bool AutoEatInCombat { get; set; }
    public long StunnedUntil { get; set; }
    public BoonBurning? Burning { get; set; }
    public ClassResources Resources { get; set; } = new();
    public ActorBoons Boons { get; set; } = new();

    public void Validate()
    {
        if ((Build is null) == (InnateStats is null) || InnateStats?.Speed is <= 0 || !Enum.IsDefined(Facing) || BleedTurns is < 0 or > 5
            || MoveCost <= 0 || SnaredBy < 0 || EnemySnareDelay is not (0 or EnemySquads.SnareDelay)
            || WebRecoveryDelay < 0 || WebRecoveryDelay % EnemyWebRules.Delay != 0
            || StunnedUntil < 0 || DefinitionId is null || CurrencyDrop < 0 || Resources is null || Boons is null
            || ExperienceReward < 0 || ExperienceFraction is < 0 or >= ContributionRewards.UnitsPerExperience
            || Build is null && ExperienceFraction != 0
            || Burning is { } burning && (burning.Power < 0 || burning.Remaining <= 0 || burning.Interval <= 0
                || burning.Due < 0 || burning.Schedule <= 0 || burning.Activation is null))
            throw new InvalidDataException("Invalid actor tactics.");
        Build?.Validate();
        if (Enemy != null)
        {
            if (Build != null || !EnemyCatalog.Default.Contains(DefinitionId)) throw new InvalidDataException("Invalid enemy identity.");
            Enemy.Validate();
            if (Enemy.PhaseTwo && (!EnemyPhases.Supported(DefinitionId) || InnateStats!.Speed > int.MaxValue - EnemyPhases.SpeedBonus))
                throw new InvalidDataException("Invalid enemy phase role or speed.");
            if (Enemy.MirrorCasts > 0 && DefinitionId != EnemyMirrors.Definition)
                throw new InvalidDataException("Invalid mirror owner role.");
            if (Enemy.EmberDives > 0 && DefinitionId != EnemyEmbers.Definition)
                throw new InvalidDataException("Invalid ember owner role.");
            if (Enemy.SiltCharges > 0 && DefinitionId != EnemySilt.Definition)
                throw new InvalidDataException("Invalid silt owner role.");
            if (Enemy.SentinelStrikes > 0 && DefinitionId != EnemySentinel.Definition)
                throw new InvalidDataException("Invalid sentinel owner role.");
            if ((Enemy.OathPartner != 0 || Enemy.OathBroken) && !EnemyOaths.IsTwin(DefinitionId))
                throw new InvalidDataException("Invalid oath owner role.");
            if (Enemy.WebCasts > 0 && DefinitionId != "core:enemy/web_weaver")
                throw new InvalidDataException("Invalid web caster role.");
            if (Enemy.AlarmSpent && DefinitionId != "core:enemy/alarm_keeper"
                || Enemy.CalledBy > 0 && (DefinitionId != "core:enemy/ruin_skitter" || !RewardExempt
                    || Enemy.SummonedBy != 0 || Enemy.FragmentOf != 0 || Enemy.RevivalOf != 0))
                throw new InvalidDataException("Invalid alarm role or rewards.");
            if (Enemy.FragmentOf > 0 && (DefinitionId != "core:enemy/bone_fragment" || !RewardExempt || Enemy.SummonedBy != 0))
                throw new InvalidDataException("Invalid fragment role or rewards.");
            if (Enemy.RevivalSpent && DefinitionId != "core:enemy/grave_cantor"
                || Enemy.RevivalOf > 0 && (!RewardExempt || Enemy.FragmentOf != 0 || Enemy.SummonedBy != 0
                    || !EnemyRevivalRules.Revivable(DefinitionId)))
                throw new InvalidDataException("Invalid revival role or rewards.");
            if (Enemy.WardReady && DefinitionId != "core:enemy/ward_weaver"
                || Enemy.ShieldLowered && DefinitionId != "core:enemy/shield_bearer"
                || Enemy.Squad is { } squad && DefinitionId != EnemySquads.Roster(squad.Kind).Leader && DefinitionId != EnemySquads.Roster(squad.Kind).Follower)
                throw new InvalidDataException("Enemy squad role/state mismatch.");
        }
        Resources.Validate(Build?.Class);
        Boons.Validate(Build?.Class);
        var s = Stats;
        if (s.MaxHealth <= 0 || s.Damage < 0 || s.SpecialDamage < 0 || s.Defense < 0 || s.Speed <= 0
            || s.CriticalChancePartsPerMillion < 0 || s.CriticalDamagePercent < 100)
            throw new InvalidDataException("Invalid tactical stats.");
    }
}

public enum DamageCause { DirectAttack, OngoingEffect, GroundHazard, TriggeredEffect }
public sealed record LockedAttack(long Id, long Source, WorldPosition Origin, WorldPosition[] Cells,
    string Ability, AttackDelivery Delivery, DamageElement Element, int Damage, long Due,
    DamageCause Cause = DamageCause.DirectAttack, bool Critical = false, bool ClassGenerated = false,
    bool HitsOwner = false, bool PersistsWithoutOwner = false, bool IgnoresTerrain = false, BoonCause? Causality = null,
    int? FateFace = null, long IntendedTarget = 0, int? RecoveryCost = null,
    EnemySupport? Support = null, EnemyAdvance? Advance = null, EnemyShove? Shove = null, Facing? SentinelFacing = null, WorldPosition? EmberLanding = null, MirrorReflection? Reflection = null);
public sealed record TacticalTimelineEntry(ScheduledKind Kind, long ActorId, long Due, long ReferenceId,
    bool Projected, long? Sequence);
public sealed record TacticalEvent(long Sequence, long Time, string Kind, long Source, long Target,
    long Action, WorldPosition Position, int Amount = 0, int Prevented = 0, string Ability = "",
    bool Forced = false, WorldPosition? From = null, LootChange? Loot = null, EnemyProtection? Protection = null, OathDamage? Oath = null, MirrorReflection? Reflection = null);

/// <summary>Persisted work queue and event identities shared across terrain/floors.</summary>
public sealed class TacticalWorldState
{
    private ActionTimeline? _queue;
    private TimelineSnapshot _snapshot = new(0, 1, []);
    public TimelineSnapshot Timeline
    {
        get => _queue?.Snapshot() ?? _snapshot;
        set { _snapshot = value; _queue = null; }
    }
    [JsonIgnore] public ActionTimeline Queue => _queue ??= ActionTimeline.Restore(_snapshot);
    public long ActiveActor { get; set; }
    public long ControlledActor { get; set; }
    public bool DrivesWorldClock { get; set; }
    public TacticalTuning Tuning { get; set; } = new();
    public long NextAction { get; set; } = 1;
    public long NextEvent { get; set; } = 1;
    public int RandomState { get; set; } = 1;
    public HashSet<long> Participants { get; set; } = [];
    public List<LockedAttack> Attacks { get; set; } = [];
    public List<TacticalEvent> Events { get; set; } = [];
    public List<EnemySnare> EnemySnares { get; set; } = [];
    public List<RevivalCorpse> Corpses { get; set; } = [];
    public List<EnemyWeb> Webs { get; set; } = [];
    public List<SiltTrail> Silt { get; set; } = [];
    public List<EnemyEmber> Embers { get; set; } = [];
    public List<EnemyMirror> Mirrors { get; set; } = [];
    public BoonWorldEffects BoonEffects { get; set; } = new();
    public ContributionRewards Rewards { get; set; } = new();
    public TacticalLootState Loot { get; set; } = new();
    public TacticalChestState Chests { get; set; } = new();
}

/// <summary>Delegated numerical tuning; one ordinary 100-cost action represents one second.</summary>
public sealed record TacticalTuning(int GuardReductionPercent = 50, int GuardCost = 100,
    int WaitCost = 100, int ReviveCost = 100, int UnitsPerSecond = 100, int EatCost = 100,
    int SurvivalIntervalSeconds = 10, int WellFedHungerMaximum = 20)
{
    // The imported calendar has 300 world ticks/hour. One world tick represents
    // twelve seconds; tactical timestamps retain sub-tick recovery exactly.
    public long UnitsPerWorldTick => (long)UnitsPerSecond * 3600 / WorldSession.TicksPerHour;
    public void Validate()
    {
        if (GuardReductionPercent is < 0 or > 100 || GuardCost <= 0 || WaitCost <= 0 || ReviveCost <= 0 || UnitsPerSecond <= 0 || EatCost <= 0
            || SurvivalIntervalSeconds <= 0 || WellFedHungerMaximum is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(TacticalTuning));
    }
}
