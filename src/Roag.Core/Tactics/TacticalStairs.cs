using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewStairs(Actor actor, TacticalClass? definition)
    {
        var landing = _world.StairLanding(actor.Position);
        if (landing == null || !_world.CanEnter(landing.Value, actor.Id))
            return Invalid("Stand on an unobstructed stair with a clear landing.");
        int cost = definition?.Costs.Move ?? actor.Tactics!.MoveCost;
        return new(true, "", ActionTiming.Recovery(cost, EffectiveSpeed(actor)), []) { StairLanding = landing };
    }

    private void CommitStairs(Actor actor, WorldPosition landing, long action)
    {
        var from = actor.Position;
        _world.MoveTacticalActor(actor, landing);
        Emit("FloorChanged", actor.Id, actor.Id, action, landing, from: from);
        // Arrival is a real occupied tile, not a traversed line between different floors.
        Emit("TileEntered", actor.Id, actor.Id, action, landing);
        CollectLootAt(actor, landing, action);
        if (State.ControlledActor == actor.Id)
        {
            _world.State.Position = landing;
            _world.Chunks.Prefetch(landing);
            _world.Reveal();
        }
    }
}
