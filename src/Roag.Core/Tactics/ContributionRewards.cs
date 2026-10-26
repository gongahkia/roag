using System.Numerics;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed class CombatContribution
{
    public long Damage { get; set; }
    public long Healing { get; set; }
    public long Protection { get; set; }
    public long Control { get; set; }

    public BigInteger Weight(ContributionTuning tuning) => (BigInteger)Damage * tuning.Damage
        + (BigInteger)Healing * tuning.Healing + (BigInteger)Protection * tuning.Protection
        + (BigInteger)Control * tuning.Control;
}

/// <summary>Actual health points for damage/support; timeline units for enemy control.</summary>
public sealed record ContributionTuning(int Damage = 1000, int Healing = 1000, int Protection = 1000, int Control = 40)
{
    public void Validate()
    {
        if (Damage <= 0 || Healing <= 0 || Protection <= 0 || Control <= 0)
            throw new InvalidDataException("Contribution weights must be positive.");
    }
}

public sealed class EnemyContributions
{
    public Dictionary<long, CombatContribution> Members { get; set; } = [];
    // Outstanding actual damage, by recipient, bounds healing credit to enemy-caused wounds.
    public Dictionary<long, long> UnhealedDamage { get; set; } = [];
}

/// <summary>Persistent ledgers survive retreat, floor changes and saving; settled ledgers are removed.</summary>
public sealed class ContributionRewards
{
    public const int UnitsPerExperience = 1_000_000;
    public ContributionTuning Tuning { get; set; } = new();
    public Dictionary<long, EnemyContributions> Enemies { get; set; } = [];

    // Largest remainder allocates every unit exactly; stable actor identities break ties.
    internal static Dictionary<long, long> Allocate(long units, IEnumerable<(long Id, BigInteger Weight)> weights)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(units);
        var eligible = weights.Where(x => x.Weight > 0).OrderBy(x => x.Id).ToArray();
        var result = new Dictionary<long, long>();
        if (eligible.Length == 0) return result;
        BigInteger total = eligible.Aggregate(BigInteger.Zero, (sum, x) => sum + x.Weight);
        var remainders = new List<(long Id, BigInteger Remainder)>();
        long remaining = units;
        foreach (var entry in eligible)
        {
            var quotient = BigInteger.DivRem(units * entry.Weight, total, out var remainder);
            long share = checked((long)quotient);
            result.Add(entry.Id, share); remaining -= share;
            remainders.Add((entry.Id, remainder));
        }
        foreach (var entry in remainders.OrderByDescending(x => x.Remainder).ThenBy(x => x.Id).Take(checked((int)remaining)))
            result[entry.Id]++;
        return result;
    }

    internal void Validate(IReadOnlyDictionary<long, Actor> actors)
    {
        if (Tuning is null || Enemies is null) throw new InvalidDataException("Missing contribution rewards.");
        Tuning.Validate();
        foreach (var (id, ledger) in Enemies)
        {
            if (!actors.TryGetValue(id, out var enemy) || enemy.Life == LifeState.Dead
                || enemy.Tactics is not { RewardsClaimed: false, RewardExempt: false }
                || ledger is null || ledger.Members is null || ledger.UnhealedDamage is null)
                throw new InvalidDataException("Invalid enemy contribution ledger.");
            foreach (var (member, contribution) in ledger.Members)
                if (!actors.TryGetValue(member, out var actor) || actor.Tactics?.Build is null || contribution is null
                    || contribution.Damage < 0 || contribution.Healing < 0 || contribution.Protection < 0 || contribution.Control < 0
                    || contribution.Weight(Tuning) <= 0)
                    throw new InvalidDataException("Invalid combat contribution.");
            foreach (var (recipient, damage) in ledger.UnhealedDamage)
                if (!actors.TryGetValue(recipient, out var actor) || actor.Tactics?.Build is null || damage <= 0)
                    throw new InvalidDataException("Invalid outstanding combat damage.");
        }
        if (actors.Values.Any(a => a.Tactics is { RewardsClaimed: true } && a.Life != LifeState.Dead))
            throw new InvalidDataException("A living enemy cannot have a settled death reward.");
    }
}
