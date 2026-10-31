using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewEating(Actor actor)
        => !Friendly(actor) || actor.Body.Hunger <= 0 || _world.State.Inventory.GetValueOrDefault("food") <= 0
            ? Invalid("This member needs hunger and one shared food provision to eat.")
            : new(true, "", ActionTiming.Recovery(Tuning.EatCost, EffectiveSpeed(actor)), []);

    private void Eat(Actor actor, long action)
    {
        // Hunger is the imported authoritative need (zero fed, one hundred empty).
        // No saturation reserve or instant HP healing is introduced.
        int amount = _world.Simulation.MealReduction;
        double before = actor.Body.Hunger, gain = Math.Min(amount, before);
        _world.State.Inventory["food"]--;
        _world.State.ResourcesConsumed++;
        actor.Body.Hunger = Math.Max(0, before - amount);
        var snapshot = CaptureBoonEvent(BoonTrigger.SupplyCollected, actor.Id, 0, action, actor.Position) with
        { SupplyKind = "food", Amount = amount, ActualGain = gain, HpBefore = actor.Health, TargetMaxHp = actor.MaxHealth };
        // Existing food boons now respond to actual paid eating, not stocking the
        // globally shared pack. The event preserves their original causal trigger.
        Emit("FoodEaten", actor.Id, actor.Id, action, actor.Position, amount);
        Emit("SupplyCollected", actor.Id, 0, action, actor.Position, amount, snapshot: snapshot);
    }

    private bool ShouldEatAutomatically(Actor member) => member.Tactics is { AutoEat: true } tactics
        && member.Body.Hunger >= _world.Simulation.MealAt
        && (tactics.AutoEatInCombat || !_world.IsActorInCombat(member.Id))
        && PreviewEating(member).Valid;

    private long SurvivalInterval => (long)Tuning.SurvivalIntervalSeconds * Tuning.UnitsPerSecond;

    private void EnsureSurvivalScheduled()
    {
        if (State.DrivesWorldClock && !State.Queue.Snapshot().Pending.Any(w => w.Kind == ScheduledKind.Survival))
            State.Queue.Schedule(ScheduledKind.Survival, checked(Clock + SurvivalInterval));
    }

    private void ResolveSurvival()
    {
        long action = State.NextAction++;
        ResolveAtomically(RootCause(action), () =>
        {
            foreach (var member in _world.Party.Where(p => p.Life != LifeState.Dead).OrderBy(p => p.Id).ToArray())
            {
                if (member.Body.Hunger >= _world.Simulation.StarveAt)
                {
                    if (member.Life == LifeState.Downed)
                    {
                        member.Tactics!.BleedTurns--;
                        if (member.Tactics.BleedTurns == 0) Die(member, 0, action);
                        else Emit("BleedShortened", 0, member.Id, action, member.Position, member.Tactics.BleedTurns);
                    }
                    else
                    {
                        member.Health = Math.Max(0, member.Health - 1);
                        Emit("StarvationDamage", 0, member.Id, action, member.Position, 1);
                        if (member.Health == 0) Down(member, 0, action);
                    }
                }
                else if (member.Life == LifeState.Alive && member.Body.Hunger <= Tuning.WellFedHungerMaximum
                         && member.Health < member.MaxHealth)
                {
                    member.Health++;
                    RecordCombatHealing(member.Id, member.Id, 1);
                    Emit("WellFedHealing", member.Id, member.Id, action, member.Position, 1);
                }
            }
        });
        if (!_world.State.AwaitingContinuation) EnsureSurvivalScheduled();
    }
}
