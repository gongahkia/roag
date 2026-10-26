using Roag.Core.World;

namespace Roag.Core.Tactics;

public enum GroundLootKind { Currency, Health }

/// <summary>Only uncollected physical loot is retained. Identity is never reused.</summary>
public sealed record GroundLoot(long Id, GroundLootKind Kind, int Amount, WorldPosition Position,
    long DefeatedActor = 0, long CarriedBy = 0);

/// <summary>Display facts survive collection/removal without retaining a second live drop.</summary>
public sealed record LootChange(long Drop, GroundLootKind Kind, int Amount, int? ActualGain = null, int? Balance = null);

public sealed class TacticalLootState
{
    public long NextDrop { get; set; } = 1;
    public List<GroundLoot> Drops { get; set; } = [];

    internal void Validate(WorldPlan plan, IReadOnlyDictionary<long, Actor> actors)
    {
        if (NextDrop <= 0 || Drops is null) throw new InvalidDataException("Invalid physical loot header.");
        long previous = 0;
        foreach (var drop in Drops)
        {
            if (drop is null || drop.Id <= previous || drop.Id >= NextDrop || !Enum.IsDefined(drop.Kind)
                || drop.Amount <= 0 || !plan.Contains(drop.Position) || drop.DefeatedActor < 0 || drop.CarriedBy < 0
                || drop.DefeatedActor != 0 && (!actors.TryGetValue(drop.DefeatedActor, out var enemy)
                    || enemy.Life != LifeState.Dead || enemy.Tactics is not { RewardsClaimed: true, RewardExempt: false })
                || drop.CarriedBy != 0 && !actors.ContainsKey(drop.CarriedBy))
                throw new InvalidDataException("Invalid physical loot or dangling owner.");
            previous = drop.Id;
        }
    }
}

public sealed partial class TacticalCombat
{
    internal GroundLoot SpawnLoot(GroundLootKind kind, int amount, WorldPosition position,
        long source = 0, long action = 0, long defeatedActor = 0)
    {
        if (!Enum.IsDefined(kind) || amount <= 0 || !_world.Plan.Contains(position))
            throw new ArgumentException("Physical loot requires a valid kind, positive amount and world position.");
        if (source != 0) _ = Actor(source);
        if (defeatedActor != 0 && Actor(defeatedActor) is not { Life: LifeState.Dead, Tactics: { RewardsClaimed: true, RewardExempt: false } })
            throw new ArgumentException("Kill loot requires a settled nonexempt victim.");
        long id = State.Loot.NextDrop;
        State.Loot.NextDrop = checked(id + 1);
        var drop = new GroundLoot(id, kind, amount, position, defeatedActor);
        State.Loot.Drops.Add(drop);
        Emit(kind == GroundLootKind.Currency ? "CurrencyDropped" : "SupplyDropped", source, defeatedActor,
            action, position, amount, loot: new(id, kind, amount));
        return drop;
    }

    private void SpawnKillLoot(BoonEvent e, Actor enemy)
    {
        if (Friendly(enemy) || enemy.Tactics is not { RewardExempt: false } tactics) return;
        if (tactics.CurrencyDrop > 0)
            SpawnLoot(GroundLootKind.Currency, tactics.CurrencyDrop, enemy.Position, e.SourceId ?? 0, e.RootActionId ?? 0, enemy.Id);
        if (tactics.HealthDrop)
            SpawnLoot(GroundLootKind.Health, 10, enemy.Position, e.SourceId ?? 0, e.RootActionId ?? 0, enemy.Id);
    }

    private bool CollectLootAt(Actor owner, WorldPosition position, long action)
    {
        if (!Friendly(owner) || owner.Life != LifeState.Alive || owner.Health <= 0 || owner.Position.Floor != position.Floor) return false;
        bool collected = false;
        foreach (var drop in State.Loot.Drops.Where(d => d.Position == position && d.CarriedBy == 0).ToArray())
        {
            if (drop.Kind == GroundLootKind.Currency)
            {
                int balance = checked(_world.State.Inventory.GetValueOrDefault("gold") + drop.Amount);
                _world.State.Inventory["gold"] = balance;
                State.Loot.Drops.Remove(drop); collected = true;
                var snapshot = CaptureBoonEvent(BoonTrigger.CurrencyCollected, owner.Id, 0, action, position) with { Amount = drop.Amount };
                Emit("CurrencyCollected", owner.Id, 0, action, position, drop.Amount, snapshot: snapshot,
                    loot: new(drop.Id, drop.Kind, drop.Amount, Balance: balance));
            }
            else if (owner.Health < owner.MaxHealth)
            {
                int before = owner.Health, gain = Math.Min(drop.Amount, owner.MaxHealth - before);
                owner.Health += gain;
                RecordCombatHealing(owner.Id, owner.Id, gain);
                State.Loot.Drops.Remove(drop); collected = true;
                var snapshot = CaptureBoonEvent(BoonTrigger.SupplyCollected, owner.Id, 0, action, position) with
                { Amount = drop.Amount, ActualGain = gain, SupplyKind = "health", HpBefore = before, TargetMaxHp = owner.MaxHealth };
                Emit("SupplyCollected", owner.Id, 0, action, position, drop.Amount, snapshot: snapshot,
                    loot: new(drop.Id, drop.Kind, drop.Amount, gain));
            }
        }
        return collected;
    }

    private void GatherLoot(Actor owner, BoonActivation activation, int power)
    {
        if (!Friendly(owner) || owner.Life != LifeState.Alive || power <= 0) return;
        var vision = _world.VisionFrom(owner.Position);
        int gathered = 0;
        foreach (var (dx, dy) in new[] { (0, -1), (1, 0), (0, 1), (-1, 0) })
        {
            if (gathered >= power) break;
            var position = owner.Position.Offset(dx, dy);
            if (_world.GroundOpen(position) && ProjectileOpen(position) && vision.Contains(position)
                && CollectLootAt(owner, position, activation.RootActionId ?? 0)) gathered++;
        }
    }

    private void EnrichLoot(Actor owner, BoonActivation activation, int power)
    {
        if (power <= 0 || activation.Event.TargetId is not { } victim) return;
        int index = State.Loot.Drops.FindIndex(d => d.Kind == GroundLootKind.Currency && d.DefeatedActor == victim);
        if (index < 0) return;
        var drop = State.Loot.Drops[index];
        int amount = checked(drop.Amount + power);
        State.Loot.Drops[index] = drop with { Amount = amount };
        Emit("CurrencyDropEnriched", owner.Id, victim, activation.RootActionId ?? 0, drop.Position,
            power, ability: activation.BoonId, loot: new(drop.Id, drop.Kind, amount));
    }
}
