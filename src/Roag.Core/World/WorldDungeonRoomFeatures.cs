using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private void PopulateDungeonRoomFeatures(DungeonLayout layout, WorldPosition origin, TacticalCombat combat)
    {
        var floor = State.Dungeon!.Floors.Single(f => f.Origin == origin);
        int seed = RoagRandom.Derive(DungeonSeed, $"floor:{origin.Floor}/room-features");
        bool Safe(WorldPosition p) => Chunks.At(p) == Material.Floor && DungeonTerrainAt(p) == null && CanEnter(p) && ObjectAt(p) == null
            && p.Distance(floor.Entry) >= 4 && p.Distance(floor.Exit) >= 3
            && !combat.State.Chests.Chests.Any(n => n.Position.Distance(p) < 2)
            && !combat.State.Doors.Nodes.Any(n => n.Position.Distance(p) < 2)
            && !combat.State.Secrets.Nodes.Any(n => n.Position.Distance(p) < 2 || n.Clue == p)
            && !combat.State.OptionalTerrain.Nodes.Any(n => n.Position == p);
        var candidates = new List<(WorldPosition Position, bool Room)>();
        for (int y = 1; y < layout.Height - 1; y++) for (int x = 1; x < layout.Width - 1; x++)
        {
            var p = origin.Offset(x, y);
            if (Safe(p)) candidates.Add((p, floor.Rooms.Any(r => r.Contains(p))));
        }
        bool Place(DungeonRoomFeatureKind kind, bool room, int variant = 1)
        {
            foreach (var candidate in candidates.Where(c => c.Room == room)
                .OrderBy(c => RoagRandom.Derive(seed, kind + ":" + variant + ":" + c.Position.X + "," + c.Position.Y)))
            {
                var p = candidate.Position;
                string marker = $"floor:{origin.Floor}/feature:{kind}/{variant}/{p.X},{p.Y}";
                if (!Safe(p) || kind == DungeonRoomFeatureKind.Pillar && EncounterDirections.Count(d => Materials.Walkable(Chunks.At(p.Offset(d.X, d.Y)))) != 4
                    || !combat.CanPlaceRoomFeature(kind, marker, p)) continue;
                combat.SpawnRoomFeature(kind, marker, p, variant); return true;
            }
            return false;
        }
        foreach (var kind in new[] { DungeonRoomFeatureKind.Pillar, DungeonRoomFeatureKind.Ore, DungeonRoomFeatureKind.Brazier, DungeonRoomFeatureKind.Ward })
            Place(kind, room: true);
        DungeonRoomFeatureKind? scenery = layout.Biome switch
        {
            "core:biome/overgrown_ruins" => DungeonRoomFeatureKind.Overgrowth,
            "core:biome/fungal_hollows" => DungeonRoomFeatureKind.Mushroom,
            "core:biome/ashen_bastion" or "core:biome/sundered_forge" => DungeonRoomFeatureKind.MineRail,
            _ => null
        };
        if (scenery is { } decoration) for (int variant = 1; variant <= 2; variant++) Place(decoration, decoration != DungeonRoomFeatureKind.MineRail, variant);
        foreach (var room in layout.Rooms)
            for (int i = 0; i < room.Shape.Structures.Length; i++)
            {
                var structure = room.Shape.Structures[i]; var point = room.At(new(structure.X, structure.Y)); var p = origin.Offset(point.X, point.Y);
                string marker = $"floor:{origin.Floor}/structure:{room.Index}/{i + 1}";
                if (RoagRandom.Derive(seed, marker) % 100 >= 42 || !Safe(p)) continue;
                var kind = structure.Kind switch { "pillar" => DungeonRoomFeatureKind.Pillar, "brazier" => DungeonRoomFeatureKind.Brazier,
                    "ward" => DungeonRoomFeatureKind.Ward, "mushroom" => DungeonRoomFeatureKind.Mushroom, "crystal" => DungeonRoomFeatureKind.Crystal,
                    _ => throw new InvalidDataException("Unknown authored room structure.") };
                if (combat.CanPlaceRoomFeature(kind, marker, p)) combat.SpawnRoomFeature(kind, marker, p, kind == DungeonRoomFeatureKind.Mushroom ? (i + 1) % 2 + 1 : 1);
            }
    }
}
