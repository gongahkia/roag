using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private static readonly (int X, int Y)[] EncounterDirections = [(0, -1), (1, 0), (0, 1), (-1, 0)];

    private void PopulateDungeonEncounters(DungeonLayout layout, WorldPosition origin, TacticalCombat combat)
    {
        var floor = State.Dungeon!.Floors.Single(f => f.Origin.Floor == origin.Floor);
        string sourceFloor = DungeonCatalog.Default.Biomes.Single(b => b.Id == layout.Biome).FloorId
            + "/stage/" + floor.Stage + "/depth/" + (origin.Floor % 2 == 1 ? 1 : 2);
        PopulateDungeonSquad(layout, origin, floor, combat, sourceFloor);
        PopulateDungeonIdea(floor, combat, sourceFloor);
    }

    private void PopulateDungeonSquad(DungeonLayout layout, WorldPosition origin, DungeonFloorState floor, TacticalCombat combat, string sourceFloor)
    {
        var selection = DungeonEncounterSelection.Squad(DungeonSeed, layout.Biome, sourceFloor);
        var kind = selection.Kind; var roster = EnemySquads.Roster(kind); int followers = selection.Followers;
        (int X, int Y)[] offsets = [(0, 0), (-1, 1), (1, 1), (-1, 0), (1, 0), (0, 1)];
        foreach (var room in layout.Rooms.Where(r => r.Role is not ("start" or "elite")))
        {
            if (!room.Shape.Markers.TryGetValue("enemy", out var markers) || markers.Length == 0) continue;
            var mark = room.At(markers[0]);
            for (int oy = -2; oy <= 2; oy++) for (int ox = -2; ox <= 2; ox++)
            {
                var center = origin.Offset(mark.X + ox, mark.Y + oy);
                if (center.Distance(floor.Entry) < 10) continue;
                bool clear = true;
                for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                {
                    var cell = center.Offset(x, y);
                    if (!floor.Rooms[room.Index].Contains(cell) || !CanEnter(cell) || cell == floor.Entry || cell == floor.Exit) clear = false;
                }
                if (!clear) continue;
                var points = offsets.Take(followers + 1).Select(d => center.Offset(d.X, d.Y)).ToArray();
                if (kind == EnemySquadKind.WardedBodyguards && !DungeonFormationCanBeFlanked(floor, points)) continue;
                var leader = SpawnDungeonEnemy(combat, roster.Leader, points[0]);
                var squad = new EnemySquad($"floor:{origin.Floor}/squad:{room.Index}", kind, leader.Id, center);
                leader.Tactics!.Enemy!.Squad = squad;
                foreach (var cell in points.Skip(1)) SpawnDungeonEnemy(combat, roster.Follower, cell).Tactics!.Enemy!.Squad = squad;
                return;
            }
        }
    }

    private bool DungeonFormationCanBeFlanked(DungeonFloorState floor, WorldPosition[] points)
    {
        var blocked = points.Skip(1).ToHashSet();
        var seen = new HashSet<WorldPosition> { floor.Entry }; var queue = new Queue<WorldPosition>(); queue.Enqueue(floor.Entry);
        while (queue.TryDequeue(out var cell))
        {
            if (cell.Distance(points[0]) == 1) return true;
            foreach (var (dx, dy) in Directions)
            {
                var next = cell.Offset(dx, dy);
                if (floor.Contains(next) && !blocked.Contains(next) && (Materials.Walkable(Chunks.At(next)) || Chunks.At(next) == Material.ClosedDoor)
                    && (ObjectAt(next)?.Kind == "dungeon-door" || ObjectAt(next) is not { Solid: true } prop || prop.Kind == "boon-chest")
                    && seen.Add(next)) queue.Enqueue(next);
            }
        }
        return false;
    }

    private void PopulateDungeonIdea(DungeonFloorState floor, TacticalCombat combat, string sourceFloor)
    {
        var selection = DungeonEncounterSelection.Idea(DungeonSeed, sourceFloor);
        if (selection.Definition is not { } definition) return;
        int seed = selection.Seed;
        var choices = State.Actors.Where(a => a.Life == LifeState.Alive && a.Position.Floor == floor.Origin.Floor
                && a.Tactics?.Enemy is { Squad: null } && a.Position.Distance(floor.Entry) >= 8
                && EnemyCatalog.Default.Get(a.Tactics.DefinitionId).Ai is EnemyBrainKind.Guard or EnemyBrainKind.Skitter or EnemyBrainKind.ExpansionRegular)
            .OrderBy(a => a.Id).ToArray();
        if (choices.Length == 0) return;
        bool Beam(WorldPosition a, WorldPosition b) => EnemySquads.Clear(a, b,
            p => !Materials.Opaque(Chunks.At(p)) && !DungeonMistBlocks(p) && ObjectAt(p) is not { Solid: true });
        bool Companion(WorldPosition p) => CanEnter(p) && Chunks.At(p) == Material.Floor && DungeonTerrainAt(p) == null
            && p.Distance(floor.Entry) >= 8 && p.Distance(floor.Exit) > 2 && !combat.State.Chests.Chests.Any(c => c.Position == p);
        int offset = seed / 4 % choices.Length;
        for (int i = 0; i < choices.Length; i++)
        {
            var target = choices[(offset + i) % choices.Length]; WorldPosition? companion = null;
            if (definition == EnemySentinel.Definition && !floor.Rooms.Any(r => !r.Critical && r.Contains(target.Position)
                    && target.Position.X > r.Origin.X && target.Position.Y > r.Origin.Y
                    && target.Position.X < r.Origin.X + r.Width - 1 && target.Position.Y < r.Origin.Y + r.Height - 1)) continue;
            bool hasCompany = definition == "core:enemy/grave_cantor" && State.Actors.Any(a => a.Id != target.Id && a.Life == LifeState.Alive
                && a.Tactics is { RewardExempt: false, Enemy: not null } && EnemyRevivalRules.Revivable(a.Tactics.DefinitionId)
                && a.Position.Distance(target.Position) <= 4 && Beam(target.Position, a.Position));
            if (definition == EnemyOaths.A || definition == "core:enemy/grave_cantor" && !hasCompany)
            {
                foreach (var (dx, dy) in EncounterDirections)
                {
                    var cell = target.Position.Offset(dx, dy);
                    if (Companion(cell) && Beam(target.Position, cell) && Beam(cell, target.Position)) { companion = cell; break; }
                }
                if (companion == null) continue;
            }
            string original = target.Tactics!.DefinitionId;
            combat.ReplaceUnactedEnemy(target, definition);
            if (companion is { } position)
            {
                var other = SpawnDungeonEnemy(combat, definition == EnemyOaths.A ? EnemyOaths.B : original, position);
                if (definition == EnemyOaths.A) combat.LinkOathTwins(target.Id, other.Id);
            }
            return;
        }
    }
}
