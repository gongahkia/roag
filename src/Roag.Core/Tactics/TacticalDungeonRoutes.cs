using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private void ActivateDungeonElites()
    {
        if (_world.State.Dungeon is not { } run) return;
        foreach (var floor in run.Floors.Where(f => f.Boss > 0 && f.Stage < 4))
        {
            var boss = Actor(floor.Boss);
            if (boss is not { Life: LifeState.Alive, Tactics.Enemy: { Dormant: true } memory }
                || !_world.Party.Any(a => a.Life == LifeState.Alive && a.Position.Distance(boss.Position) <= 7)) continue;
            memory.Dormant = false;
            State.Queue.Schedule(ScheduledKind.ActorReady, Clock, boss.Id);
            Emit("EliteEngaged", boss.Id, boss.Id, 0, boss.Position);
        }
    }

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
        var corpse = Actor(victim).Position;
        if (floor.Stage == 4)
        {
            Emit("RegentDefeated", e.SourceId ?? 0, victim, e.RootActionId ?? 0, corpse);
            return;
        }
        run.PendingEliteRewards.Add(victim);
        Emit("EliteDefeated", e.SourceId ?? 0, victim, e.RootActionId ?? 0, corpse);
        if (!TryPlaceEliteReward(victim, e.RootActionId ?? 0)) Emit("EliteRewardDeferred", 0, victim, e.RootActionId ?? 0, corpse);
    }

    private bool TryPlaceEliteReward(long victim, long action)
    {
        var run = _world.State.Dungeon!; var floor = run.Floors.Single(f => f.Boss == victim); var corpse = Actor(victim).Position;
        // Keep the receipt when bodies/fixtures temporarily occupy all nearby ground.
        // Walking away can expose a real landing later; no object is erased to fit it.
        for (int radius = 1; radius <= 5; radius++)
            for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++)
            {
                var cell = corpse.Offset(dx, dy);
                if (Math.Abs(dx) + Math.Abs(dy) != radius || !ChestCellOpen(cell)) continue;
                SpawnBoonChest("core:chest/free_elite", "elite:stage:" + floor.Stage, cell);
                run.PendingEliteRewards.Remove(victim);
                Emit("EliteRewardPlaced", 0, victim, action, cell);
                return true;
            }
        return false;
    }

    private void SettlePendingEliteRewards()
    {
        if (_world.State.Dungeon is not { } run || run.PendingEliteRewards.Count == 0) return;
        foreach (long id in run.PendingEliteRewards.Order().ToArray()) TryPlaceEliteReward(id, 0);
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
