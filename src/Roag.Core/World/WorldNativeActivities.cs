using Roag.Core.Tactics;
using Roag.Core.Expeditions;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    /// <summary>Advance only explicitly ordered sustained activity; idle rendering never calls this for an unassigned member.</summary>
    public bool ContinueDungeonActivities(int actionBudget = 16)
    {
        if (actionBudget is < 1 or > 256) throw new ArgumentOutOfRangeException(nameof(actionBudget));
        if (State.Dungeon == null || State.AwaitingContinuation || DungeonCombat.Active?.Tactics?.Activity == null) return false;
        var combat = DungeonCombat; long before = combat.State.NextAction, events = combat.State.NextEvent;
        combat.AdvanceToPartyDecision(actionBudget);
        return combat.State.NextAction != before || combat.State.NextEvent != events;
    }

    public bool WarmedByCampFire(WorldPosition position)
    {
        if (State.Tactics is not { } state) return false;
        for (int i = 0; i < state.CampFires.Count; i++)
        {
            var fire = state.CampFires[i];
            if (fire.Until > state.Queue.Clock && fire.Position.Distance(position) <= state.Camp.FireWarmthRange
                && VisionFrom(position).Contains(fire.Position)) return true;
        }
        return false;
    }

    public ActionResult CancelDungeonActivity(long memberId) => State.Dungeon != null && DungeonCombat.CancelActivity(memberId)
        ? new(true, "Activity cancelled; already committed recovery is unchanged.")
        : new(false, "This member has no continuing activity.");
}
