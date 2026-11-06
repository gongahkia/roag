using Roag.Core.World;
using Roag.Core.Content;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    internal bool CanReachCampCompanion(WorldPosition camp, WorldPosition member) => EnemySquads.Clear(camp, member, SupportOpen);

    private Actor? MedicineRecipient(Actor actor, long target) => target == 0 ? actor
        : _world.Party.FirstOrDefault(a => a.Id == target);

    private TacticalPreview PreviewMedicine(Actor actor, long target)
    {
        var recipient = MedicineRecipient(actor, target);
        if (!Friendly(actor) || recipient is not { Life: LifeState.Alive, Health: > 0, Tactics.Build: not null }
            || recipient.Health >= recipient.MaxHealth || _world.Count("medicine") < 1)
            return Invalid("Choose an injured conscious member and one shared medicine. Revival is a separate action.");
        if (actor.Position.Distance(recipient.Position) > 1 || !_world.GroundOpen(recipient.Position)
            || !_world.VisionFrom(actor.Position).Contains(recipient.Position))
            return Invalid("Stand on or beside the conscious member to administer medicine.");
        return new(true, "", ActionTiming.Recovery(Tuning.ReviveCost, EffectiveSpeed(actor)), [recipient.Position]);
    }

    private void UseMedicine(Actor actor, long target, long action)
    {
        var recipient = MedicineRecipient(actor, target)!;
        int amount = checked(_world.Content.Get<AbilityDefinition>("ability.medicine").Power + _world.EquipmentEffectPower(actor, "healing") + RetainedTalentPower(actor, "healing"));
        _world.State.Inventory["medicine"]--; _world.RecordNativeConsumption(1);
        CleanseNativeConditions(recipient, complete: false);
        HealCampCompanion(actor, recipient, amount, action, "ability.medicine");
    }

    internal void HealCampCompanion(Actor actor, Actor recipient, int amount, long action, string ability)
    {
        int before = recipient.Health, gain = Math.Min(amount, recipient.MaxHealth - before);
        recipient.Health += gain;
        RecordCombatHealing(actor.Id, recipient.Id, gain);
        Emit("Healed", actor.Id, recipient.Id, action, recipient.Position, gain, ability: ability);
        // As with a picked-up healing supply, the recipient owns any excess-heal
        // ward. The administering member separately earns actual healing credit.
        var snapshot = CaptureBoonEvent(BoonTrigger.SupplyCollected, recipient.Id, recipient.Id, action, recipient.Position, ability) with
        { SupplyKind = "health", Amount = amount, ActualGain = gain, HpBefore = before, TargetMaxHp = recipient.MaxHealth };
        Emit("SupplyCollected", recipient.Id, recipient.Id, action, recipient.Position, amount, ability: ability, snapshot: snapshot);
    }

    // Automatic eating preserves ordinary provision use first, then consumes the
    // first available recipe by stable ID. A player can explicitly choose a meal.
    private string? EatingSource(string item) => item == ""
        ? _world.Count("food") > 0 ? "food" : State.PreparedMeals.OrderBy(p => p.Key, StringComparer.Ordinal).FirstOrDefault(p => p.Value > 0).Key
        : item == "food" ? _world.Count("food") > 0 ? "food" : null
        : State.PreparedMeals.GetValueOrDefault(item) > 0 ? item : null;

    private TacticalPreview PreviewEating(Actor actor, string item = "")
        => !Friendly(actor) || actor.Body.Hunger <= 0 || EatingSource(item) == null
            ? Invalid("This member needs hunger and one available shared provision or prepared serving to eat.")
            : new(true, "", ActionTiming.Recovery(Tuning.EatCost, EffectiveSpeed(actor)), []);

    private void Eat(Actor actor, long action, string item)
    {
        // Hunger is the imported authoritative need (zero fed, one hundred empty).
        // No saturation reserve or instant HP healing is introduced.
        string source = EatingSource(item)!;
        if (source == "food") _world.State.Inventory["food"]--;
        else
        {
            if (--State.PreparedMeals[source] == 0) State.PreparedMeals.Remove(source);
            var meal = _world.Content.Get<CampDefinition>(source);
            actor.Preparations.RemoveAll(p => p.Effect == meal.Effect || p.Until <= _world.State.Tick);
            actor.Preparations.Add(new(meal.Id, meal.Effect, meal.Power, checked(_world.State.Tick + meal.Duration)));
        }
        _world.RecordNativeConsumption(1);
        RestoreFoodHunger(actor, action, _world.Simulation.MealReduction, source);
    }

    private void RestoreFoodHunger(Actor actor, long action, int amount, string source)
    {
        double before = actor.Body.Hunger, gain = Math.Min(amount, before);
        actor.Body.Hunger = Math.Max(0, before - amount);
        var snapshot = CaptureBoonEvent(BoonTrigger.SupplyCollected, actor.Id, 0, action, actor.Position) with
        { SupplyKind = "food", Amount = amount, ActualGain = gain, HpBefore = actor.Health, TargetMaxHp = actor.MaxHealth };
        // Existing food boons now respond to actual paid eating, not stocking the
        // globally shared pack. The event preserves their original causal trigger.
        Emit("FoodEaten", actor.Id, actor.Id, action, actor.Position, amount, ability: source);
        Emit("SupplyCollected", actor.Id, 0, action, actor.Position, amount, snapshot: snapshot);
    }

    private bool ShouldEatAutomatically(Actor member) => member.Tactics is { AutoEat: true } tactics
        && member.Body.Hunger >= _world.Simulation.MealAt
        && (tactics.AutoEatInCombat || !_world.IsActorInCombat(member.Id))
        && PreviewEating(member).Valid;

    private long SurvivalInterval => (long)Tuning.SurvivalIntervalSeconds * Tuning.UnitsPerSecond;

    private void EnsureSurvivalScheduled()
    {
        if (State.DrivesWorldClock && Tuning.EnableSurvival && !State.Queue.Snapshot().Pending.Any(w => w.Kind == ScheduledKind.Survival))
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
                    StopActivity(member, "starvation");
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
