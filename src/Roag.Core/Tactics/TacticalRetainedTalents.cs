using Roag.Core.Content;
using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    internal int RetainedTalentPower(Actor actor, string effect)
    {
        if (actor.Tactics?.Build == null) return 0;
        int total = 0;
        foreach (var (id, _) in actor.Tactics.Boons.Inventory.Stacks)
        {
            if (RetainedBoons.Find(id) is not { Active: false } entry) continue;
            var talent = _world.Content.Get<TalentDefinition>(entry.Source);
            int basis = talent.Bonuses.GetValueOrDefault(effect);
            if (basis <= 0) continue;
            long strength = Math.Min(20, actor.Tactics.Boons.Inventory.Magnitude(id).Power);
            total = checked(total + (int)strength * basis);
        }
        return Math.Min(20, total);
    }

    private int RetainedAttackBonus(Actor source, Actor target, DamageElement element, bool area)
    {
        int bonus = 0;
        if ((long)target.Health * 2 <= target.MaxHealth) bonus += RetainedTalentPower(source, "wounded-damage");
        if (target.Tactics?.Conditions.Any(c => c.Kind == NativeConditionKind.Mark) == true)
            bonus += RetainedTalentPower(source, "marked-damage");
        if (target.Tactics?.StunnedUntil > Clock) bonus += RetainedTalentPower(source, "stunned-damage");
        if (element == DamageElement.Fire) bonus += RetainedTalentPower(source, "burn-damage");
        if (area) bonus += RetainedTalentPower(source, "all-target-damage");
        return bonus;
    }

    private int RetainedManaEquivalent(Actor actor, int formerMana) => formerMana <= 0 ? 0
        : Math.Max(1, formerMana - RetainedTalentPower(actor, "mana-discount"));
}
