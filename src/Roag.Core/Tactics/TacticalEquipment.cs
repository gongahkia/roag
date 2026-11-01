using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
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
