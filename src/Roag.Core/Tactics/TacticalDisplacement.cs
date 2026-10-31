using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private bool ProtectedForcedLanding(WorldPosition position) => _world.Chunks.At(position) is Material.StairsDown or Material.StairsUp;

    private bool TryShove(Actor target, Facing facing, long source, long action)
    {
        var (dx, dy) = AttackGeometry.Offset(facing); var next = target.Position.Offset(dx, dy);
        string? blocked = target.Life != LifeState.Alive || target.Tactics == null ? "invalid target"
            : target.Tactics.Invulnerable ? "immune"
            : !_world.Plan.Contains(next) || !_world.GroundOpen(next) ? "wall"
            : ProtectedForcedLanding(next) ? "stairs"
            : target.Tactics.Enemy?.PursuitBoundary is { } boundary && !boundary.Contains(next) ? "dungeon boundary"
            : _world.OccupantAt(next, target.Id) != null ? "occupied" : null;
        if (blocked != null)
        {
            Emit("ShoveBlocked", source, target.Id, action, target.Position, ability: blocked);
            return false;
        }
        int? interruptedRecovery = State.Attacks.FirstOrDefault(a => a.Source == target.Id && a.RecoveryCost != null)?.RecoveryCost;
        CancelAttacks(target.Id, source, action, "displacement");
        var from = target.Position; _world.MoveTacticalActor(target, next);
        RecordCombatControl(source, target, ActionTiming.Recovery(target.Tactics!.MoveCost, EffectiveSpeed(target)));
        Emit("Displaced", source, target.Id, action, next, forced: true, from: from);
        Emit("TileEntered", target.Id, target.Id, action, next, forced: true, from: from);
        CollectLootAt(target, next, action);
        if (interruptedRecovery is { } cost) ScheduleReady(target, cost);
        if (_resolutionDepth == 0 && !_drainingBoons)
        {
            TriggerSnares(target, action);
            TriggerEnemySnares(target, action, next);
        }
        return true;
    }
}
