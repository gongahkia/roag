using Roag.Core.Dungeons;
using Roag.Core.Tactics;

namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private void PopulateDungeonSecrets(DungeonLayout layout, WorldPosition origin, TacticalCombat combat)
    {
        int seed = RoagRandom.Derive(DungeonSeed, $"floor:{origin.Floor}/secrets");
        WorldPosition At(RoomPoint p) => origin.Offset(p.X, p.Y);
        var optional = layout.Rooms.Where(r => !layout.CriticalRooms.Contains(r.Index)
            && r.Shape.Markers.GetValueOrDefault("secret_anchor") is { Length: > 0 })
            .OrderBy(r => RoagRandom.Derive(seed, "room:" + r.Index)).ToArray();
        foreach (var room in optional)
        {
            var anchor = At(room.At(room.Shape.Markers["secret_anchor"][0]));
            string marker = $"floor:{origin.Floor}/secret:room:{room.Index}";
            // Original paired floors carry a cracked cache upstairs and a movement sigil downstairs.
            if (origin.Floor % 2 == 0)
            {
                if (!CanEnter(anchor) || Chunks.At(anchor) != Material.Floor || DungeonTerrainAt(anchor) != null
                    || ObjectAt(anchor) != null) continue;
                combat.SpawnSecret(DungeonSecretKind.MossSigil, marker, anchor); return;
            }
            if (layout.Doors.Any(d => d.Room == room.Index)
                || room.Shape.Markers.GetValueOrDefault("secret_wall") is not { Length: > 0 } walls) continue;
            var local = walls[0]; var wall = At(room.At(local));
            int dx = local.X == 1 ? -1 : local.X == room.Shape.Width ? 1 : 0;
            int dy = local.Y == 1 ? -1 : local.Y == room.Shape.Height ? 1 : 0;
            if (Math.Abs(dx) + Math.Abs(dy) != 1) continue;
            var cache = wall.Offset(dx, dy);
            int cx = cache.X - origin.X, cy = cache.Y - origin.Y;
            if (cx <= 0 || cy <= 0 || cx >= layout.Width - 1 || cy >= layout.Height - 1
                || layout.Rooms.Any(r => cx >= r.X && cx < r.X + r.Shape.Width && cy >= r.Y && cy < r.Y + r.Shape.Height)
                || Chunks.At(wall) != Material.Wall || Chunks.At(cache) != Material.Wall) continue;
            combat.SpawnSecret(DungeonSecretKind.CrackedCache, marker, anchor, wall, cache); return;
        }
    }
}
