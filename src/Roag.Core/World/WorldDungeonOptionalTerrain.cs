using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private void PopulateDungeonOptionalTerrain(DungeonLayout layout, WorldPosition origin, TacticalCombat combat)
    {
        var floor = State.Dungeon!.Floors.Single(f => f.Origin.Floor == origin.Floor);
        int seed = RoagRandom.Derive(DungeonSeed, $"floor:{origin.Floor}/optional-terrain");
        bool Available(WorldPosition p) => CanEnter(p) && Chunks.At(p) == Material.Floor && DungeonTerrainAt(p) == null
            && p.Distance(floor.Entry) >= 6 && p.Distance(floor.Exit) >= 3
            && !combat.State.Chests.Chests.Any(c => c.Position.Distance(p) <= 1)
            && !combat.State.OptionalTerrain.Nodes.Any(n => n.Position.Distance(p) <= 1);
        var candidates = new List<(DungeonRoomPlacement Room, WorldPosition Position, int Score)>();
        foreach (var room in layout.Rooms.Where(r => r.Role is not ("start" or "elite")))
            for (int y = 1; y < room.Shape.Height - 1; y++) for (int x = 1; x < room.Shape.Width - 1; x++)
            {
                var p = origin.Offset(room.X + x, room.Y + y);
                if (Available(p)) candidates.Add((room, p, RoagRandom.Derive(seed, $"{room.X + x},{room.Y + y}")));
            }
        candidates.Sort((a, b) => a.Score != b.Score ? a.Score.CompareTo(b.Score)
            : a.Position.Y != b.Position.Y ? a.Position.Y.CompareTo(b.Position.Y) : a.Position.X.CompareTo(b.Position.X));
        var usedRooms = new HashSet<int>();
        foreach (var kind in new[] { OptionalTerrainKind.HeatVent, OptionalTerrainKind.PushFloor })
        {
            foreach (var candidate in candidates)
            {
                var (room, p, _) = candidate;
                if (usedRooms.Contains(room.Index) || !Available(p)) continue;
                Facing direction = Facing.South;
                if (kind == OptionalTerrainKind.PushFloor)
                {
                    bool clear = true;
                    for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                    {
                        var next = p.Offset(x, y);
                        if (!floor.Rooms[room.Index].Contains(next) || !Available(next)) clear = false;
                    }
                    if (!clear) continue;
                    direction = (Facing)((candidate.Score % 4) * 2);
                }
                string marker = $"floor:{origin.Floor}/terrain:{kind}/{p.X},{p.Y}";
                if (!combat.CanPlaceOptionalTerrain(kind, marker, p, direction)) continue;
                combat.PlaceOptionalTerrain(kind, marker, p, direction); usedRooms.Add(room.Index); break;
            }
        }
        foreach (var candidate in layout.OptionalShortcuts.OrderBy(p => RoagRandom.Derive(seed, $"shortcut:{p.X},{p.Y}")))
        {
            var position = origin.Offset(candidate.X, candidate.Y);
            string marker = $"floor:{origin.Floor}/terrain:FragileCrossing/{position.X},{position.Y}";
            if (!Available(position) || !combat.CanPlaceOptionalTerrain(OptionalTerrainKind.FragileCrossing, marker, position)) continue;
            combat.PlaceOptionalTerrain(OptionalTerrainKind.FragileCrossing, marker, position); break;
        }
    }
}
