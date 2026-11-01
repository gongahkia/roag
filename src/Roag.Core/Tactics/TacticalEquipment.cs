using Roag.Core.World;

namespace Roag.Core.Tactics;

internal sealed record EquipmentCover(long Source, int Amount);

public sealed partial class TacticalCombat
{
    private EquipmentCover? EquipmentCoverFor(Actor target, Actor? source, int damage, bool direct)
    {
        if (!direct || source == null || damage <= 1 || target.Life != LifeState.Alive || !Friendly(target)
            || !HostileTo(target, source) || (long)target.Health * 2 > target.MaxHealth) return null;
        var protector = _world.Party.Where(a => a.Id != target.Id && a is { Life: LifeState.Alive, Health: > 0,
                Tactics: { Build: not null, EquipmentCoverUsed: false } } && a.Tactics.StunnedUntil <= Clock
                && a.Position.Distance(target.Position) <= Tuning.AllyCoverRange
                && EnemySquads.Clear(a.Position, target.Position, SupportOpen))
            .Select(a => (Actor: a, Power: _world.EquipmentEffectPower(a, "protect"))).Where(p => p.Power > 0)
            .OrderByDescending(p => p.Power).ThenBy(p => p.Actor.Id).FirstOrDefault();
        if (protector.Actor == null) return null;
        protector.Actor.Tactics!.EquipmentCoverUsed = true;
        // Preserve the imported minimum of one remaining damage: cover mitigates
        // a real hit, rather than providing a general invulnerability ward.
        return new(protector.Actor.Id, Math.Min(damage - 1, protector.Power));
    }

    private TacticalPreview PreviewEquipment(Actor actor, TacticalIntent intent)
    {
        if (!Friendly(actor) || actor.Tactics?.Build == null || intent.Target != 0 && intent.Target != actor.Id)
            return Invalid("Only the ready member can change their own equipment.");
        if (string.IsNullOrWhiteSpace(intent.Item)) return Invalid("Choose carried gear or an occupied slot.");
        if (intent.Action == TacticalAction.Unequip)
        {
            if (!actor.Equipment.ContainsKey(intent.Item)) return Invalid("Choose an occupied equipment slot.");
        }
        else if (!_world.TryItem(intent.Item, out var item) || item.Slot == "none" || _world.Count(item.Stack) < 1)
            return Invalid("Choose equipment from the shared pack.");
        else if (actor.Equipment.GetValueOrDefault(item.Slot)?.Item == item.Id)
            return Invalid("This member already wears that equipment.");

        // Always use the current gear's speed. The new gear affects later actions,
        // never the recovery already paid for changing it, nor anyone else's work.
        return new(true, "", ActionTiming.Recovery(Tuning.EquipmentCost, EffectiveSpeed(actor)), []);
    }

    private void CommitEquipment(Actor actor, TacticalIntent intent, long action)
    {
        bool remove = intent.Action == TacticalAction.Unequip;
        var item = _world.Item(remove ? actor.Equipment[intent.Item].Item : intent.Item);
        var result = _world.ChangeTacticalEquipment(actor, intent.Item, remove);
        if (!result.Success) throw new InvalidOperationException("Validated equipment change failed: " + result.Message);
        Emit(remove ? "EquipmentRemoved" : "EquipmentChanged", actor.Id, actor.Id, action, actor.Position, ability: item.Id);
    }
}
