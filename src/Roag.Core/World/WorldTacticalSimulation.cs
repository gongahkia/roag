using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    /// <summary>
    /// Native engine integration point. The client cutover follows the remaining class,
    /// boon and AI ports; integration tests already exercise this against the living world.
    /// </summary>
    internal TacticalCombat CreateTacticalSimulation(TacticalTuning? tuning = null)
    {
        var combat = new TacticalCombat(this, AdvanceTacticalWorld, tuning);
        combat.State.DrivesWorldClock = true;
        return combat;
    }

    private void AdvanceTacticalWorld(long time)
    {
        var tactics = State.Tactics ?? throw new InvalidOperationException("Missing tactical clock.");
        long target = time / tactics.Tuning.UnitsPerWorldTick;
        if (target < State.Tick) throw new InvalidOperationException("Tactical time cannot rewind the world.");
        while (State.Tick < target) AdvanceWorldTick();
    }

    private bool UsesTacticalTurns(long actorId) => State.Tactics?.Participants.Contains(actorId) == true;
    internal void MoveTacticalActor(Actor actor, WorldPosition destination) => CommitClimateMove(actor, destination);

    public EnemyInspection? InspectTacticalEnemy(long enemyId) => State.Tactics is { ControlledActor: > 0 } tactics
        ? new TacticalCombat(this, AdvanceTacticalWorld).InspectEnemy(tactics.ControlledActor, enemyId) : null;
}
