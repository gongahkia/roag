using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewDungeonRoute(Actor actor, TacticalIntent intent)
    {
        var routes = _world.AvailableDungeonRoutes(actor.Id);
        return intent.Choice >= 1 && intent.Choice <= routes.Count
            ? new(true, "", ActionTiming.Recovery(100, EffectiveSpeed(actor)), [])
            : Invalid("Stand at the defeated elite's exit and choose an available route.");
    }

    private void ResolveDungeonBossDeath(BoonEvent e)
    {
        if (e.Kind != BoonTrigger.EntityKilled || e.TargetId is not { } victim || _world.State.Dungeon is not { } run) return;
        var floor = run.Floors.FirstOrDefault(f => f.Boss == victim);
        if (floor == null || !run.BossExitReceipts.Add(victim)) return;
        if (floor.Stage == 4)
        {
            Emit("RegentDefeated", e.SourceId ?? 0, victim, e.RootActionId ?? 0, floor.Exit);
            return;
        }
        // Rewards stay near the corpse, leaving its physical route/stair anchor open.
        for (int radius = 1; radius <= 5; radius++)
            for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++)
            {
                var cell = floor.Exit.Offset(dx, dy);
                if (Math.Abs(dx) + Math.Abs(dy) != radius || !_world.CanEnter(cell) || ProtectedForcedLanding(cell)
                    || State.Chests.Chests.Any(c => c.Position == cell)) continue;
                SpawnBoonChest("core:chest/free_elite", "elite:stage:" + floor.Stage, cell);
                Emit("EliteDefeated", e.SourceId ?? 0, victim, e.RootActionId ?? 0, floor.Exit);
                return;
            }
        throw new InvalidOperationException("Elite arena has no free physical reward cell.");
    }

    private void SettleDungeonVictory()
    {
        if (_world.State.Dungeon is not { Victory: false, Stage: 4 } run
            || !_world.Party.Any(a => a.Life == LifeState.Alive && a.Health > 0)) return;
        var finale = run.Floors.Single(f => f.Stage == 4);
        if (!run.BossExitReceipts.Contains(finale.Boss)) return;
        run.Victory = true;
        Emit("Victory", _world.State.Party[0], finale.Boss, 0, finale.Exit);
    }
}
