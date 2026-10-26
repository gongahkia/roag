namespace Roag.Core.Tactics;

public sealed record BoonMemory(long? ExpiresAt = null, int? Charges = null, int? Speed = null, int? Ward = null,
    int? MarkCount = null, long? MarkTarget = null, int? Face = null, long? RollAction = null);
public sealed record BoonWardAbsorption(string Boon, int Amount);
public sealed record BoonDiceRoll(int Face, long Power);
public sealed record BoonVowPayment(int HealthCost, long DamageBonus);

/// <summary>Owned stacks and finite effect memory on the real world actor, using the shared clock.</summary>
public sealed class ActorBoons
{
    public BoonInventory Inventory { get; set; } = new();
    public Dictionary<string, BoonMemory> Memory { get; set; } = [];
    public long? ReversalTarget { get; set; }

    public void ApplyTimed(string id, BoonEffectKind kind, int power, int duration, long clock)
    {
        if (!Inventory.Stacks.ContainsKey(id)) throw new InvalidOperationException("A boon effect needs its owner's stack.");
        ArgumentOutOfRangeException.ThrowIfNegative(power);
        ArgumentOutOfRangeException.ThrowIfNegative(clock);
        if (duration is < 1 or > 10000) throw new ArgumentOutOfRangeException(nameof(duration));
        var memory = Memory.GetValueOrDefault(id, new());
        bool unexpired = memory.ExpiresAt >= clock;
        var renewed = memory with { ExpiresAt = checked(clock + duration) };
        Memory[id] = kind switch
        {
            BoonEffectKind.Prime => renewed with { Charges = 1 },
            BoonEffectKind.SpeedBoost => renewed with { Speed = Math.Max(unexpired ? memory.Speed ?? 0 : 0, power) },
            BoonEffectKind.Ward => renewed with { Ward = Math.Max(unexpired ? memory.Ward ?? 0 : 0, power) },
            _ => throw new ArgumentOutOfRangeException(nameof(kind), "This effect does not use timed boon memory.")
        };
    }

    public bool ConsumePrime(string id, long clock)
    {
        if (!Memory.TryGetValue(id, out var memory) || memory.Charges is not > 0 || memory.ExpiresAt == null || memory.ExpiresAt < clock) return false;
        Memory[id] = memory with { Charges = 0 };
        return true;
    }

    public int SpeedBonus(long clock, BoonCatalog? catalog = null)
    {
        if (Memory.Count == 0) return 0;
        int speed = 0;
        foreach (string id in (catalog ?? BoonCatalog.Default).Ids)
            if (Memory.TryGetValue(id, out var memory) && memory.ExpiresAt >= clock)
                speed = checked(speed + (memory.Speed ?? 0));
        return speed;
    }

    public int WardTotal(long clock, BoonCatalog? catalog = null)
    {
        if (Memory.Count == 0) return 0;
        int total = 0;
        foreach (string id in (catalog ?? BoonCatalog.Default).Ids)
            if (Memory.TryGetValue(id, out var memory) && memory.ExpiresAt >= clock)
                total = checked(total + (memory.Ward ?? 0));
        return total;
    }

    /// <summary>Wards are one-hit protection: partially consumed wards lose their unused capacity.</summary>
    public IReadOnlyList<BoonWardAbsorption> ConsumeWards(int damage, long clock, BoonCatalog? catalog = null)
    {
        if (damage <= 0 || Memory.Count == 0) return [];
        var absorbed = new List<BoonWardAbsorption>();
        int remaining = damage;
        foreach (string id in (catalog ?? BoonCatalog.Default).Ids)
        {
            if (!Memory.TryGetValue(id, out var memory) || memory.ExpiresAt < clock || memory.ExpiresAt == null || memory.Ward is not > 0) continue;
            int amount = Math.Min(remaining, memory.Ward.Value);
            Memory[id] = memory with { Ward = 0 };
            absorbed.Add(new(id, amount)); remaining -= amount;
            if (remaining == 0) break;
        }
        return absorbed;
    }

    public BoonVowPayment? Vow(BoonCatalog? catalog = null)
    {
        const string id = "core:boon/pilgrims_vow";
        if (!Inventory.Stacks.ContainsKey(id)) return null;
        catalog ??= BoonCatalog.Default;
        return new(catalog.Get(id).AttackPayment!.Hp, Inventory.Magnitude(id, catalog).Power);
    }

    public BoonDiceRoll? RollDice(RoagRandom random, long action, BoonCatalog? catalog = null)
    {
        const string id = "core:boon/fateful_dice";
        if (!Inventory.Stacks.ContainsKey(id)) return null;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(action);
        int face = random.NextOneBased(8);
        Memory[id] = Memory.GetValueOrDefault(id, new()) with { Face = face, RollAction = action };
        return new(face, Inventory.Magnitude(id, catalog).Power);
    }

    public void Validate(ClassKind? kind, long? nextAction = null, BoonCatalog? catalog = null)
    {
        if (Inventory is null || Memory is null) throw new InvalidDataException("Missing owned boon state.");
        Inventory.Validate(kind, catalog);
        if (ReversalTarget <= 0 || ReversalTarget != null && !Inventory.Stacks.ContainsKey("core:boon/ring_of_reversal"))
            throw new InvalidDataException("Invalid owned reversal target.");
        if (Memory.Count == 0) return;
        catalog ??= BoonCatalog.Default;
        foreach (var (id, memory) in Memory)
        {
            if (!catalog.Contains(id) || memory is null || !Inventory.Stacks.ContainsKey(id)
                || memory.ExpiresAt < 0 || memory.Charges < 0 || memory.Speed < 0 || memory.Ward < 0 || memory.MarkCount < 0 || memory.MarkTarget < 0
                || (memory.Charges != null || memory.Speed != null || memory.Ward != null || memory.MarkCount != null) && memory.ExpiresAt == null
                || memory.MarkCount != null && memory.MarkTarget == null
                || memory.Face != null && (id != "core:boon/fateful_dice" || memory.Face is < 1 or > 8
                    || memory.RollAction is null or < 1 || nextAction != null && memory.RollAction >= nextAction)
                || memory.RollAction != null && memory.Face == null)
                throw new InvalidDataException("Invalid owned boon memory.");
        }
    }
}
