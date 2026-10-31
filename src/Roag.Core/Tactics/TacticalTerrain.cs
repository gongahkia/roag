using Roag.Core.World;

namespace Roag.Core.Tactics;

public sealed partial class TacticalCombat
{
    private TacticalPreview PreviewBeaconReturn(Actor actor, ReturnBeacon beacon, long recovery)
    {
        var target = beacon.Position;
        if (target.Floor != actor.Position.Floor || target == actor.Position
            || _world.RoomAt(actor.Position) != beacon.Room
            || _world.RoomAt(target) != beacon.Room)
            return Invalid("Return is limited to a different tile in the beacon's room on this floor.");
        if (!_world.CanEnter(target, actor.Id) || !_world.Visible.Contains(target)
            || _world.Chunks.At(target) is Material.StairsDown or Material.StairsUp)
            return Invalid("The beacon landing must be visible, empty and clear of stairs.");
        var seen = new HashSet<WorldPosition> { actor.Position };
        var queue = new Queue<(WorldPosition Position, int Distance)>(); queue.Enqueue((actor.Position, 0));
        while (queue.TryDequeue(out var cell))
        {
            if (cell.Position == target) return new(true, "", recovery, [target], ReturnDistance: cell.Distance);
            if (cell.Distance == 6) continue;
            foreach (var next in Neighbors(cell.Position))
                if (!seen.Contains(next) && _world.GroundOpen(next) && _world.Chunks.Seen(next)
                    && _world.RoomAt(next) == beacon.Room)
                { seen.Add(next); queue.Enqueue((next, cell.Distance + 1)); }
        }
        return Invalid("The beacon needs an explored route of at most six tiles within its room.");
    }

    private static IEnumerable<WorldPosition> Neighbors(WorldPosition p)
    {
        yield return p.Offset(0, -1); yield return p.Offset(1, 0);
        yield return p.Offset(0, 1); yield return p.Offset(-1, 0);
    }

    /// <summary>
    /// Removing a traversable cell preserves connectivity exactly when all of its open
    /// neighbors remain connected. Search from every neighbor, merging components as
    /// they meet, so a small sealed pocket is detected without flooding the whole world.
    /// Mobile actors do not decide terrain connectivity. Only this owner's old block is
    /// ignored because replacement removes it; another Geomancer's block stays solid.
    /// </summary>
    private bool BulwarkPreservesRoutes(Actor owner, WorldPosition candidate) => TerrainRemovalPreservesRoutes(candidate, owner.Id);

    private bool TerrainRemovalPreservesRoutes(WorldPosition candidate, long ignoredOwner = 0)
    {
        bool Open(WorldPosition p) => p != candidate && _world.GroundOpen(p, ignoredOwner);
        var starts = Neighbors(candidate).Where(Open).ToArray();
        if (starts.Length <= 1) return true;
        var parents = Enumerable.Range(0, starts.Length).ToArray();
        var pending = Enumerable.Repeat(1, starts.Length).ToArray();
        var seen = new Dictionary<WorldPosition, int>();
        var queue = new Queue<(WorldPosition Position, int Group)>();
        int groups = starts.Length;
        for (int i = 0; i < starts.Length; i++) { seen.Add(starts[i], i); queue.Enqueue((starts[i], i)); }
        int Root(int n)
        {
            while (parents[n] != n) { parents[n] = parents[parents[n]]; n = parents[n]; }
            return n;
        }
        while (queue.TryDequeue(out var cell))
        {
            int group = Root(cell.Group); pending[group]--;
            foreach (var next in Neighbors(cell.Position))
            {
                if (!Open(next)) continue;
                if (seen.TryGetValue(next, out int known))
                {
                    int other = Root(known);
                    if (other == group) continue;
                    parents[other] = group; pending[group] += pending[other]; pending[other] = 0;
                    if (--groups == 1) return true;
                }
                else { seen.Add(next, group); queue.Enqueue((next, group)); pending[group]++; }
            }
            if (pending[group] == 0) return false;
        }
        return false;
    }
}
