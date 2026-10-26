namespace Roag.Core.Tactics;

public enum GrowthStat { MaxHealth, Damage, Defense, Speed, CriticalChance, CriticalDamage }

/// <summary>Permanent build stats. Current health and temporary modifiers belong to the world actor.</summary>
public sealed record BuildStats(int MaxHealth, int Damage, int SpecialDamage, int Defense, int Speed,
    int CriticalChancePartsPerMillion, int CriticalDamagePercent)
{
    public static BuildStats Starting(ClassKind kind)
    {
        var c = ClassCatalog.Get(kind);
        return new(c.Health, c.Damage, c.SpecialDamage, c.Defense, c.Speed,
            c.CriticalChancePartsPerMillion, c.CriticalDamagePercent);
    }

    internal BuildStats Grow(GrowthStat stat) => stat switch
    {
        GrowthStat.MaxHealth => this with { MaxHealth = checked(MaxHealth + 4) },
        GrowthStat.Damage => this with { Damage = checked(Damage + 1), SpecialDamage = checked(SpecialDamage + 1) },
        GrowthStat.Defense => this with { Defense = checked(Defense + 1) },
        GrowthStat.Speed => this with { Speed = checked(Speed + 10) },
        GrowthStat.CriticalChance => this with { CriticalChancePartsPerMillion = checked(CriticalChancePartsPerMillion + 50_000) },
        GrowthStat.CriticalDamage => this with { CriticalDamagePercent = checked(CriticalDamagePercent + 25) },
        _ => throw new ArgumentOutOfRangeException(nameof(stat))
    };
}

/// <summary>
/// Immutable per-member progression. Equipment/boon modifiers are applied separately;
/// a class cannot change through XP. A member owns its random stream and growth history.
/// </summary>
public sealed record CharacterBuild(ClassKind Class, int Level, long Experience, BuildStats Stats, int RandomState)
{
    public static CharacterBuild Create(ClassKind kind, int randomSeed)
        => new(kind, 1, 0, BuildStats.Starting(kind), new RoagRandom(randomSeed).State);

    // Original cumulative curve: 5, 15, 30, 50, ...
    public static long NextThreshold(int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(level);
        // Divide before multiplying, allowing the widest valid integer level range.
        long a = level, b = (long)level + 1;
        if (a % 2 == 0) a /= 2; else b /= 2;
        return checked(5 * a * b);
    }

    public BuildAdvance GainExperience(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        Validate();
        long experience = checked(Experience + amount);
        var random = new RoagRandom(RandomState);
        var stats = Stats;
        int level = Level;
        var growth = new List<LevelGrowth>();
        while (experience >= NextThreshold(level))
        {
            level = checked(level + 1);
            var stat = (GrowthStat)(random.NextOneBased(6) - 1);
            var before = stats;
            stats = stats.Grow(stat);
            growth.Add(new(level, stat, before, stats));
        }
        return new(this with { Level = level, Experience = experience, Stats = stats, RandomState = random.State },
            growth.AsReadOnly());
    }

    /// <summary>Validate persisted build state without changing it or drawing random values.</summary>
    public void Validate()
    {
        if (!Enum.IsDefined(Class) || Level < 1 || Experience < 0 || Stats is null
            || RandomState <= 0 || RandomState >= RoagRandom.Modulus)
            throw new InvalidDataException("Invalid character build header.");
        long threshold;
        try { threshold = NextThreshold(Level); }
        catch (OverflowException) { throw new InvalidDataException("Character level exceeds the XP range."); }
        if (Experience >= threshold || Level > 1 && Experience < NextThreshold(Level - 1))
            throw new InvalidDataException("Character level does not match accumulated experience.");
        var initial = BuildStats.Starting(Class);
        long growthCount = 0;
        foreach (var (current, start, increment) in new[] {
            (Stats.MaxHealth, initial.MaxHealth, 4), (Stats.Damage, initial.Damage, 1),
            (Stats.Defense, initial.Defense, 1), (Stats.Speed, initial.Speed, 10),
            (Stats.CriticalChancePartsPerMillion, initial.CriticalChancePartsPerMillion, 50_000),
            (Stats.CriticalDamagePercent, initial.CriticalDamagePercent, 25) })
        {
            long delta = (long)current - start;
            if (delta < 0 || delta % increment != 0)
                throw new InvalidDataException("Invalid permanent stat growth.");
            growthCount += delta / increment;
        }
        if (growthCount != Level - 1 || (long)Stats.SpecialDamage - initial.SpecialDamage != (long)Stats.Damage - initial.Damage)
            throw new InvalidDataException("Permanent stats do not match the character's growth count.");
    }
}

public sealed record LevelGrowth(int Level, GrowthStat Stat, BuildStats Before, BuildStats After)
{
    // The world applies this to living members under the original health-growth rule;
    // gaining XP must not independently revive a downed actor or create a second HP value.
    public int MaxHealthIncrease => After.MaxHealth - Before.MaxHealth;
}

public sealed record BuildAdvance(CharacterBuild Build, IReadOnlyList<LevelGrowth> Growth);
