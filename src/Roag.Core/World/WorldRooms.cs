namespace Roag.Core.World;

public enum RoomArea { OpenSurface, Town, Site }
public readonly record struct WorldRoomId(RoomArea Area, int Place, int Floor, int Room);

public sealed partial class ChunkGenerator
{
    private static readonly (int X, int Y)[] RoomDirections = [(0, -1), (1, 0), (0, 1), (-1, 0)];
    /// <summary>
    /// Stable generated room membership, independent of later excavation and of chunks
    /// currently resident. Corridors are not a shared dungeon-wide room. Natural surface
    /// ground has no authored room subdivision, matching Roag's roomless-map fallback.
    /// </summary>
    public WorldRoomId? RoomAt(WorldPosition p)
    {
        if (!plan.Contains(p)) return null;
        // Match terrain painting precedence when footprints overlap.
        for (int i = plan.Sites.Count - 1; i >= 0; i--)
        {
            var site = plan.Sites[i];
            if (Inside(site.Center)) return InFootprint(false, site.Id, site.Center);
        }
        if (p.Floor != 0) return null;
        for (int i = plan.Towns.Count - 1; i >= 0; i--)
        {
            var town = plan.Towns[i];
            if (Inside(town.Center)) return InFootprint(true, town.Id, town.Center);
        }
        return new(RoomArea.OpenSurface, 0, 0, 0);

        bool Inside(WorldPosition center) => p.X >= center.X - 32 && p.X < center.X + 32
            && p.Y >= center.Y - 32 && p.Y < center.Y + 32;
        WorldRoomId? InFootprint(bool town, int id, WorldPosition center)
        {
            var footprint = GenerateFootprint(town, id, p.Floor);
            int room = footprint.RoomCells[(p.Y - center.Y + 32) * footprint.Width + p.X - center.X + 32];
            return room == 0 ? null : new(town ? RoomArea.Town : RoomArea.Site, id, p.Floor, room);
        }
    }

    /// <summary>
    /// WFC has tile patterns instead of room rectangles. Record enclosed floor components
    /// separated by walls/doors before ruin damage and connectivity repair open passages.
    /// The exterior component and door thresholds have no room identity.
    /// </summary>
    internal static int[] EnclosedRooms(Material[] tiles, int width, int height)
    {
        var result = new int[tiles.Length];
        var seen = new bool[tiles.Length];
        int nextRoom = 1;
        for (int start = 0; start < tiles.Length; start++)
        {
            if (seen[start] || !Interior(tiles[start])) continue;
            var cells = new List<int>(); var queue = new Queue<int>();
            queue.Enqueue(start); seen[start] = true;
            bool exterior = false;
            while (queue.TryDequeue(out int cell))
            {
                cells.Add(cell);
                int x = cell % width, y = cell / width;
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1) exterior = true;
                foreach (var (dx, dy) in RoomDirections)
                {
                    int xx = x + dx, yy = y + dy;
                    if (xx < 0 || yy < 0 || xx >= width || yy >= height) continue;
                    int next = yy * width + xx;
                    if (seen[next] || !Interior(tiles[next])) continue;
                    seen[next] = true; queue.Enqueue(next);
                }
            }
            if (exterior) continue;
            foreach (int cell in cells) result[cell] = nextRoom;
            nextRoom++;
        }
        return result;
        static bool Interior(Material material) => material is Material.Floor or Material.Grass or Material.Earth;
    }
}
