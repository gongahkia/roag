using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    public IReadOnlyList<DungeonRoute> AvailableDungeonRoutes(long actorId)
    {
        if (!State.Party.Contains(actorId)) return [];
        var actor = ActorById(actorId);
        if (actor.Life != LifeState.Alive) return [];
        if (State.Dungeon == null)
        {
            var complex = SnapshotComplexAt(State, actor.Position);
            var source = DungeonFloorAt(actor.Position);
            return complex is { Populated: true, Routes: not null } && source is { Stage: < 4, Boss: > 0 }
                && !complex.Routes.Choices.ContainsKey(source.Branch) && actor.Position.Distance(source.Exit) <= 1
                && VisionFrom(actor.Position).Contains(source.Exit) && ActorById(source.Boss).Life == LifeState.Dead
                && complex.Run.BossExitReceipts.Contains(source.Boss) ? DungeonComplexRoutes.Offers(source) : [];
        }
        if (State.Dungeon is not { Victory: false, Stage: <= 3 } run) return [];
        var floor = run.Floors.LastOrDefault(f => f.Stage == run.Stage && f.Boss > 0);
        return floor != null && actor.Position.Distance(floor.Exit) <= 1
            && VisionFrom(actor.Position).Contains(floor.Exit) && ActorById(floor.Boss).Life == LifeState.Dead
            && run.BossExitReceipts.Contains(floor.Boss) ? DungeonRoutes.ForStage(run.Stage, run.RouteHistory) : [];
    }

    public bool DungeonExitCleared(WorldPosition exit)
    {
        var floor = DungeonFloorAt(exit);
        return floor is { Stage: < 4, Boss: > 0 } && floor.Exit == exit
            && DungeonAt(exit)!.BossExitReceipts.Contains(floor.Boss);
    }

    public bool DungeonRouteChosen(WorldPosition exit)
        => SnapshotComplexAt(State, exit) is { Routes: not null } complex && DungeonFloorAt(exit) is { } floor
            && complex.Routes.Choices.ContainsKey(floor.Branch);

    internal void ConnectDungeonRoute(TacticalCombat combat, Actor actor, int choice)
    {
        if (State.Dungeon == null)
        {
            OpenPhysicalDungeonRoute(actor, choice);
            return;
        }
        var run = State.Dungeon!; var route = DungeonRoutes.ForStage(run.Stage, run.RouteHistory)[choice - 1];
        var previous = run.Floors.Last(); var landing = previous.Exit.Offset(-4, 0) with { Floor = previous.Origin.Floor + 1 };
        run.RouteHistory.Add(route.Id); run.Stage++;
        if (route.Biome is { } biome)
        {
            int seed = RoagRandom.Derive(DungeonSeed, "stage:" + run.Stage);
            var first = DungeonLayoutGenerator.Generate(seed, biome);
            var second = DungeonLayoutGenerator.Generate(RoagRandom.Derive(seed, "lower"), biome);
            var origin = new WorldPosition(landing.X - first.Start.X, landing.Y - first.Start.Y, landing.Floor);
            PaintDungeonFloor(first, origin, combat, run, DungeonSeed, boss: false);
            var upperExit = origin.Offset(first.Elite.X, first.Elite.Y);
            var lowerLanding = upperExit.Offset(-4, 0) with { Floor = upperExit.Floor + 1 };
            var lowerOrigin = new WorldPosition(lowerLanding.X - second.Start.X, lowerLanding.Y - second.Start.Y, lowerLanding.Floor);
            PaintDungeonFloor(second, lowerOrigin, combat, run, DungeonSeed, boss: true);
            Chunks.Edit(upperExit, Material.StairsDown); Chunks.Edit(lowerLanding, Material.StairsUp);
        }
        else
        {
            var map = DungeonCatalog.Default.Finale;
            var origin = new WorldPosition(landing.X - (map.StartX - 1), landing.Y - (map.StartY - 1), landing.Floor);
            for (int y = 0; y < map.Height; y++) for (int x = 0; x < map.Width; x++)
                Chunks.Edit(origin.Offset(x, y), map.Rows[y][x] == '#' ? Material.Wall : Material.Floor);
            var bossPosition = origin.Offset(map.BossX - 1, map.BossY - 1);
            var boss = SpawnDungeonEnemy(combat, "core:enemy/hollow_regent", bossPosition);
            run.Floors.Add(new(4, DungeonRunState.FinaleBiome, origin, map.Width, map.Height,
                [new(0, map.Id, origin, map.Width, map.Height, 0, false)], landing, bossPosition, boss.Id));
        }
        Chunks.Edit(previous.Exit, Material.StairsDown); Chunks.Edit(landing, Material.StairsUp);
        Reveal();
    }

    private void OpenPhysicalDungeonRoute(Actor actor, int choice)
    {
        var complex = SnapshotComplexAt(State, actor.Position)!;
        var source = DungeonFloorAt(actor.Position)!;
        var route = AvailableDungeonRoutes(actor.Id)[choice - 1];
        var target = DungeonComplexRoutes.Target(complex.Run, source, route);
        complex.Routes!.Choices.Add(source.Branch, route.Id);
        Open(DungeonPassagePlanner.SourceSeal(source));
        Open(DungeonPassagePlanner.TargetSeal(target));
        Reveal();

        void Open(WorldPosition seal)
        {
            // Digging retires this identity. Rebuilt walls and unrelated edits
            // must survive a later paid choice, even when made by another member.
            if (complex.Routes.UnbrokenSeals.Remove(DungeonRunState.CellKey(seal)) && Chunks.At(seal) == Material.Wall
                && !State.Structures.Any(s => s.Position == seal)) Chunks.Edit(seal, Material.Floor);
        }
    }

    private void RetireDungeonPassageSeal(WorldPosition position)
        => SnapshotComplexAt(State, position)?.Routes?.UnbrokenSeals.Remove(DungeonRunState.CellKey(position));
}
