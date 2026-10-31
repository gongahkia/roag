using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Roag.Core.Tactics;

public sealed record BoonStacks(int Common = 0, int Uncommon = 0, int Rare = 0, int Legendary = 0)
{
    [JsonIgnore] public bool Any => Common > 0 || Uncommon > 0 || Rare > 0 || Legendary > 0;
    public int this[BoonRarity rarity] => rarity switch
    {
        BoonRarity.Common => Common, BoonRarity.Uncommon => Uncommon,
        BoonRarity.Rare => Rare, BoonRarity.Legendary => Legendary,
        _ => throw new ArgumentOutOfRangeException(nameof(rarity))
    };
    public void Validate()
    {
        if (Common < 0 || Uncommon < 0 || Rare < 0 || Legendary < 0 || !Any)
            throw new InvalidDataException("Owned boon stacks must be nonnegative and nonempty.");
    }
    internal BoonStacks Add(BoonRarity rarity, int amount) => rarity switch
    {
        BoonRarity.Common => this with { Common = checked(Common + amount) },
        BoonRarity.Uncommon => this with { Uncommon = checked(Uncommon + amount) },
        BoonRarity.Rare => this with { Rare = checked(Rare + amount) },
        BoonRarity.Legendary => this with { Legendary = checked(Legendary + amount) },
        _ => throw new ArgumentOutOfRangeException(nameof(rarity))
    };
}

public sealed record BoonMagnitude(long Chance, long Power);

/// <summary>Member-owned stacks. Only an explicit service can retire a copy; no transfers.</summary>
public sealed record BoonInventory
{
    public ImmutableDictionary<string, BoonStacks> Stacks { get; init; } = ImmutableDictionary<string, BoonStacks>.Empty;

    public BoonInventory Grant(string id, BoonRarity rarity, ClassKind? ownerClass, int amount = 1, BoonCatalog? catalog = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (!Enum.IsDefined(rarity)) throw new ArgumentOutOfRangeException(nameof(rarity));
        catalog ??= BoonCatalog.Default;
        var definition = catalog.Get(id);
        if (!definition.Allows(ownerClass)) throw new InvalidOperationException("This boon belongs to another class.");
        if (Stacks.Keys.Any(owned => catalog.Conflicts(id, owned))) throw new InvalidOperationException("Incompatible boons.");
        var added = Stacks.GetValueOrDefault(id, new()).Add(rarity, amount);
        _ = Aggregate(definition, added); // overflow cannot leave an unusable grant half-applied
        return this with { Stacks = Stacks.SetItem(id, added) };
    }

    public BoonMagnitude Magnitude(string id, BoonCatalog? catalog = null) =>
        Aggregate((catalog ?? BoonCatalog.Default).Get(id), Stacks.GetValueOrDefault(id, new()));

    public BoonInventory Retire(string id, BoonRarity rarity)
    {
        if (!Enum.IsDefined(rarity) || !Stacks.TryGetValue(id, out var owned) || owned[rarity] == 0)
            throw new InvalidOperationException("Choose an owned rarity stack to retire.");
        var remaining = owned.Add(rarity, -1);
        return this with { Stacks = remaining.Any ? Stacks.SetItem(id, remaining) : Stacks.Remove(id) };
    }

    public static BoonMagnitude Aggregate(BoonDefinition definition, BoonStacks stacks)
    {
        long chance = 0, power = 0;
        checked
        {
            foreach (var rarity in BoonCatalog.Rarities)
            {
                if (stacks[rarity] < 0) throw new ArgumentOutOfRangeException(nameof(stacks));
                chance += stacks[rarity] * definition.Parameters[rarity].Chance;
                power += (long)stacks[rarity] * definition.Parameters[rarity].Power;
            }
        }
        return new(chance, power);
    }

    public void Validate(ClassKind? ownerClass, BoonCatalog? catalog = null)
    {
        if (Stacks is null) throw new InvalidDataException("Missing boon inventory.");
        if (Stacks.Count == 0) return;
        catalog ??= BoonCatalog.Default;
        foreach (var (id, stacks) in Stacks)
        {
            if (!catalog.Contains(id) || stacks is null || !catalog.Get(id).Allows(ownerClass))
                throw new InvalidDataException("Unknown or class-ineligible owned boon.");
            stacks.Validate();
            if (Stacks.Keys.Any(other => catalog.Conflicts(id, other))) throw new InvalidDataException("Incompatible owned boons.");
            try { _ = Aggregate(catalog.Get(id), stacks); }
            catch (OverflowException e) { throw new InvalidDataException("Boon magnitude exceeds the native numeric range.", e); }
        }
    }
}

public static class BoonChance
{
    public const int PartsPerMillion = 1_000_000;

    /// <summary>Original guaranteed overflow plus one fractional roll; exact millions consume no RNG.</summary>
    public static long Roll(RoagRandom random, long chance, double coefficient)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfNegative(chance);
        if (!double.IsFinite(coefficient) || coefficient is < 0 or > 10) throw new ArgumentOutOfRangeException(nameof(coefficient));
        long scaled = checked((long)Math.Floor(chance * coefficient + .5));
        long guaranteed = scaled / PartsPerMillion, fraction = scaled % PartsPerMillion;
        return fraction > 0 && random.NextOneBased(PartsPerMillion) <= fraction ? guaranteed + 1 : guaranteed;
    }
}
