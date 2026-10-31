using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    public IReadOnlyList<DungeonRoute> AvailableDungeonRoutes(long actorId)
    {
        if (State.Dungeon is not { Victory: false } run || run.Stage > 3 || !State.Party.Contains(actorId)) return [];
        var actor = ActorById(actorId);
        var floor = run.Floors.LastOrDefault(f => f.Stage == run.Stage && f.Boss > 0);
        return actor.Life == LifeState.Alive && floor != null && actor.Position.Distance(floor.Exit) <= 1
            && Visible.Contains(floor.Exit) && ActorById(floor.Boss).Life == LifeState.Dead
            && run.BossExitReceipts.Contains(floor.Boss) ? DungeonRoutes.ForStage(run.Stage, run.RouteHistory) : [];
    }

    internal void ConnectDungeonRoute(TacticalCombat combat, int choice)
    {
        var run = State.Dungeon!; var route = DungeonRoutes.ForStage(run.Stage, run.RouteHistory)[choice - 1];
        var previous = run.Floors.Last(); var landing = previous.Exit.Offset(-4, 0) with { Floor = previous.Origin.Floor + 1 };
        run.RouteHistory.Add(route.Id); run.Stage++;
        if (route.Biome is { } biome)
        {
            int seed = RoagRandom.Derive(DungeonSeed, "stage:" + run.Stage);
            var first = DungeonLayoutGenerator.Generate(seed, biome);
            var second = DungeonLayoutGenerator.Generate(RoagRandom.Derive(seed, "lower"), biome);
            var origin = new WorldPosition(landing.X - first.Start.X, landing.Y - first.Start.Y, landing.Floor);
            PaintDungeonFloor(first, origin, combat, boss: false);
            var upperExit = origin.Offset(first.Elite.X, first.Elite.Y);
            var lowerLanding = upperExit.Offset(-4, 0) with { Floor = upperExit.Floor + 1 };
            var lowerOrigin = new WorldPosition(lowerLanding.X - second.Start.X, lowerLanding.Y - second.Start.Y, lowerLanding.Floor);
            PaintDungeonFloor(second, lowerOrigin, combat, boss: true);
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
}
