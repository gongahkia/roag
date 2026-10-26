namespace Roag.Core.World;

public sealed partial class WorldSession
{
    private const int PlacementRadius = 6;

    /// <summary>Read-only placement query used by floor transfers and reproducible previews.</summary>
    public bool TryPartyPlacement(WorldPosition anchor, out Dictionary<long, WorldPosition> positions) => TryPartyPlacement(anchor, Party.Where(a => a.Life != LifeState.Dead).ToArray(), false, out positions);
    private bool TryPartyPlacement(WorldPosition anchor, Actor[] members, bool retainPositions, out Dictionary<long, WorldPosition> positions, bool legacyStack = false)
    {
        var ids = members.Select(a => a.Id).ToHashSet();
        var occupied = State.Actors.Where(a => a.Life != LifeState.Dead && !ids.Contains(a.Id)).Select(a => a.Position).ToHashSet();
        bool Open(WorldPosition p) => legacyStack ? Plan.Contains(p) && Materials.Walkable(Chunks.At(p)) : !occupied.Contains(p) && GroundOpen(p);
        positions = []; if (members.Length == 0 || !Open(anchor)) return false;
        var cells = new List<WorldPosition>(); var seen = new HashSet<WorldPosition> { anchor }; var queue = new Queue<WorldPosition>(); queue.Enqueue(anchor);
        while (queue.TryDequeue(out var p))
        {
            cells.Add(p);
            foreach (var (dx, dy) in Directions)
            {
                var next = p.Offset(dx, dy);
                if (next.Distance(anchor) <= PlacementRadius && Plan.Contains(next) && seen.Add(next) && Open(next)) queue.Enqueue(next);
            }
        }
        if (cells.Count < members.Length) return false;
        var used = new HashSet<WorldPosition>(); positions[members[0].Id] = anchor; used.Add(anchor);
        foreach (var member in members.Skip(1))
        {
            var preferred = retainPositions && cells.Contains(member.Position) && !used.Contains(member.Position) ? member.Position
                : cells.First(p => !used.Contains(p));
            positions[member.Id] = preferred; used.Add(preferred);
        }
        return true;
    }
    private void ApplyPartyPlacement(WorldPosition anchor, Dictionary<long, WorldPosition> positions)
    {
        State.Position = anchor;
        foreach (var actor in Party.Where(a => positions.ContainsKey(a.Id))) actor.Position = positions[actor.Id];
    }

    private void RestorePartyFormation()
    {
        var members = Party.Where(a => a.Life != LifeState.Dead).ToArray();
        if (members.Length == 0) return; // A saved wipe awaits the continuation choice.
        bool stacked = members.Length > 1 && members.All(a => a.Position == State.Position);
        if (stacked)
        {
            // Older compatible world saves deliberately stored the whole party on one cell.
            // Repair that representation in memory; loading never rewrites the selected slot.
            if (!TryPartyPlacement(State.Position, members, false, out var positions, legacyStack: true)) throw new InvalidDataException("The saved party has no room to stand on separate walkable tiles.");
            ApplyPartyPlacement(State.Position, positions);
        }
        else if (members[0].Position != State.Position || members.Any(a => a.Position.Floor != State.Position.Floor || !Materials.Walkable(Chunks.At(a.Position)))
            || members.Select(a => a.Position).Distinct().Count() != members.Length)
            throw new InvalidDataException("Invalid saved party formation.");
    }

    private void UpdatePartyLeader()
    {
        if (Party.FirstOrDefault(a => a.Life != LifeState.Dead) is not { } leader) return;
        State.Position = leader.Position;
        Reveal();
    }

    private bool TryPartyStep(WorldPosition destination, out Dictionary<long, WorldPosition> positions)
    {
        var members = Party.Where(a => a.Life != LifeState.Dead).ToArray();
        var layout = new Dictionary<long, WorldPosition>(); positions = layout;
        if (members.Length == 0 || !CanEnter(destination, members[0].Id)) return false;
        var ids = members.Select(a => a.Id).ToHashSet();
        var occupied = State.Actors.Where(a => a.Life != LifeState.Dead && !ids.Contains(a.Id)).Select(a => a.Position).ToHashSet();
        var used = new HashSet<WorldPosition> { destination }; layout[members[0].Id] = destination;
        var candidates = new Dictionary<long, WorldPosition[]>();
        for (int i = 1; i < members.Length; i++)
        {
            var member = members[i]; var goal = members[i - 1].Position;
            // A short known-terrain distance field keeps separated followers moving around
            // corners without consulting unexplored terrain or consuming randomness.
            var distances = new Dictionary<WorldPosition, int> { [goal] = 0 }; var queue = new Queue<WorldPosition>(); queue.Enqueue(goal);
            while (queue.TryDequeue(out var p))
            {
                int distance = distances[p]; if (distance >= 8) continue;
                foreach (var (dx, dy) in Directions)
                {
                    var next = p.Offset(dx, dy);
                    if (!distances.ContainsKey(next) && Chunks.Seen(next) && !occupied.Contains(next) && GroundOpen(next)) { distances[next] = distance + 1; queue.Enqueue(next); }
                }
            }
            candidates[member.Id] = Directions.Select(d => member.Position.Offset(d.X, d.Y)).Prepend(member.Position)
                .Where(p => Plan.Contains(p) && (p == member.Position || Chunks.Seen(p)) && !occupied.Contains(p) && GroundOpen(p))
                .OrderBy(p => distances.GetValueOrDefault(p, 100 + p.Distance(goal)) * 10 + (p == member.Position ? 1 : 0)).ToArray();
        }
        bool assigned = Assign(1);
        if (!assigned) layout.Clear();
        return assigned;
        bool Assign(int index)
        {
            if (index == members.Length) return State.WorkRally == null || layout.Values.All(p => p.Distance(destination) <= WorkRallyRadius);
            var member = members[index];
            foreach (var p in candidates[member.Id])
            {
                if (members.Skip(index + 1).Any(a => a.Position == p) || !used.Add(p)) continue;
                layout[member.Id] = p;
                if (Assign(index + 1)) return true;
                used.Remove(p); layout.Remove(member.Id);
            }
            return false;
        }
    }
}
