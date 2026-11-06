using System.Buffers;
using Roag.Core.World;

namespace Roag.Core.Dungeons;

public sealed record DungeonRoutePassage(int Branch, string Route, WorldPosition Barrier, WorldPosition[] Cells);
public sealed record DungeonStairPassage(int Branch, WorldPosition Top, WorldPosition Bottom, WorldPosition Barrier,
    WorldPosition[] UpperCells, DungeonRoutePassage[] Routes);

/// <summary>Plans real walking passages around authored floors, without deciding which barriers open.</summary>
public static class DungeonPassagePlanner
{
    private static readonly (int X, int Y)[] Directions = [(0, -1), (1, 0), (0, 1), (-1, 0)];

    public static DungeonStairPassage[] Generate(DungeonComplexBlueprint blueprint)
    {
        var result = new List<DungeonStairPassage>();
        var parcel = blueprint.Parcel;
        foreach (var outgoing in blueprint.Links.GroupBy(l => l.From))
        {
            var source = blueprint.Floors.Single(f => f.Branch == outgoing.Key && f.Elite);
            var eliteRoom = source.Layout.Rooms.Single(r => r.Role == "elite");
            var point = eliteRoom.Socket("E"); var socket = source.Origin.Offset(point.X, point.Y);
            int column = (source.Origin.X - parcel.X - DungeonParcelPlanner.Clearance) / DungeonParcelPlanner.SlotWidth;
            var top = new WorldPosition(parcel.X + DungeonParcelPlanner.Clearance + (column + 1) * DungeonParcelPlanner.SlotWidth
                - DungeonParcelPlanner.Clearance / 2, source.Exit.Y, source.Origin.Floor);
            var bottom = WorldStairs.Destination(top, Material.StairsDown)!.Value;
            int turn = source.Origin.X + source.Layout.Width + 1;
            if (turn >= top.X) throw new InvalidDataException("The stair annex would clip its source floor.");
            var upper = Line([socket, new(turn, socket.Y, top.Floor), new(turn, top.Y, top.Floor), top]);
            var targets = outgoing.Select(link =>
            {
                if (link.To == DungeonComplexBlueprint.FinalBranch)
                {
                    var final = blueprint.Finale;
                    var finalGate = new WorldPosition(final.Origin.X, final.Entry.Y, final.Origin.Floor);
                    return (Link: link, Outer: finalGate.Offset(-1, 0), Gate: finalGate);
                }
                var floor = blueprint.Floors.Single(f => f.Branch == link.To && !f.Elite);
                var start = floor.Layout.Rooms.Single(r => r.Role == "start"); var p = start.Socket("W");
                var gate = floor.Origin.Offset(p.X, p.Y);
                return (Link: link, Outer: new WorldPosition(floor.Origin.X - 1, gate.Y, gate.Floor), Gate: gate);
            }).ToArray();
            var obstacles = blueprint.Floors.Where(f => f.Origin.Floor == bottom.Floor).Select(f => f.Describe())
                .Concat(blueprint.Finale.Origin.Floor == bottom.Floor ? [blueprint.Finale] : []).ToArray();
            var routes = RoutesAroundFloors(parcel, bottom, targets.Select(t => t.Outer).ToArray(), obstacles);
            var branches = targets.Select((t, i) => new DungeonRoutePassage(t.Link.To, t.Link.Route, t.Gate,
                routes[i].Concat(Line([t.Outer, t.Gate]).Skip(1)).ToArray())).ToArray();
            result.Add(new(source.Branch, top, bottom, socket.Offset(2, 0), upper, branches));
        }
        return result.ToArray();
    }

    private static WorldPosition[] Line(WorldPosition[] points)
    {
        var cells = new List<WorldPosition> { points[0] };
        for (int i = 1; i < points.Length; i++)
        {
            var from = cells[^1]; var to = points[i];
            if (from.Floor != to.Floor || from.X != to.X && from.Y != to.Y)
                throw new InvalidDataException("A physical corridor must use cardinal steps on one floor.");
            while (from != to)
            {
                from = from.Offset(Math.Sign(to.X - from.X), Math.Sign(to.Y - from.Y)); cells.Add(from);
            }
        }
        return cells.ToArray();
    }

    private static WorldPosition[][] RoutesAroundFloors(DungeonParcel parcel, WorldPosition start, WorldPosition[] targets,
        DungeonFloorState[] obstacles)
    {
        int count = checked(parcel.Width * parcel.Height);
        var previous = ArrayPool<int>.Shared.Rent(count); var queue = ArrayPool<int>.Shared.Rent(count);
        int Index(WorldPosition p) => (p.Y - parcel.Y) * parcel.Width + p.X - parcel.X;
        WorldPosition Cell(int index) => new(parcel.X + index % parcel.Width, parcel.Y + index / parcel.Width, start.Floor);
        bool Open(WorldPosition p)
        {
            if (p.Floor != start.Floor || !parcel.Contains(p)) return false;
            for (int i = 0; i < obstacles.Length; i++) if (obstacles[i].Contains(p)) return false;
            return true;
        }
        try
        {
            if (!Open(start) || targets.Any(p => !Open(p))) throw new InvalidDataException("A stair or corridor approach clips an authored floor.");
            Array.Fill(previous, -1, 0, count);
            int first = Index(start), head = 0, tail = 1;
            queue[0] = first; previous[first] = first;
            var goals = targets.Select(Index).ToHashSet(); goals.Remove(first);
            while (head < tail && goals.Count > 0)
            {
                var cell = Cell(queue[head++]);
                foreach (var (dx, dy) in Directions)
                {
                    var next = cell.Offset(dx, dy);
                    if (!Open(next)) continue;
                    int index = Index(next);
                    if (previous[index] >= 0) continue;
                    previous[index] = Index(cell); queue[tail++] = index; goals.Remove(index);
                }
            }
            if (goals.Count != 0) throw new InvalidDataException("The reserved complex lacks an uncompressed corridor route.");
            return targets.Select(target =>
            {
                var path = new List<WorldPosition>(); int index = Index(target);
                while (true)
                {
                    path.Add(Cell(index));
                    if (index == first) break;
                    index = previous[index];
                }
                path.Reverse(); return path.ToArray();
            }).ToArray();
        }
        finally { ArrayPool<int>.Shared.Return(previous); ArrayPool<int>.Shared.Return(queue); }
    }
}
