using System.Text.Json;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class LocalRoutingTests
{
    [Fact]
    public void IndexedRoutesRetainThePreviousSearchOrderAcrossLiveObstaclesAndFloors()
    {
        using var world = WorldCollisionTests.Arena(); var p = world.State.Position; long id = world.State.Party[0];
        var blocker = world.State.Actors.First(a => !a.Recruited); blocker.Position = p.Offset(1, 0);
        for (int floor = 0; floor < 2; floor++)
        {
            var start = p with { Floor = floor };
            for (int y = -12; y <= 12; y++) for (int x = -12; x <= 12; x++)
                world.Chunks.Edit(start.Offset(x, y), x == 3 && y != 4 || y == -4 && x < 2 ? Material.Wall : Material.Floor);
            foreach (var life in new[] { LifeState.Alive, LifeState.Downed, LifeState.Dead })
            {
                blocker.Life = life; blocker.Position = start.Offset(1, 0);
                string before = JsonSerializer.Serialize(world.State);
                for (int i = 0; i < 45; i++)
                {
                    var target = start.Offset(i % 15 - 7, i / 15 * 9 - 9);
                    Assert.Equal(PreviousStep(world, start, target, id), world.LocalStep(start, target, id));
                }
                // A distant/blocked target exercises the bounded partial search.
                Assert.Equal(PreviousStep(world, start, start.Offset(100, 0), id), world.LocalStep(start, start.Offset(100, 0), id));
                Assert.Equal(before, JsonSerializer.Serialize(world.State));
            }
        }
        world.State.Actors.Remove(blocker);
        Assert.Equal(PreviousStep(world, p, p.Offset(8, 0), id), world.LocalStep(p, p.Offset(8, 0), id));
        world.State.Actors.Insert(0, blocker); blocker.Life = LifeState.Alive; blocker.Position = p.Offset(-1, 0);
        world.Chunks.Edit(p.Offset(0, 1), Material.Wall);
        Assert.Equal(PreviousStep(world, p, p.Offset(-8, 0), id), world.LocalStep(p, p.Offset(-8, 0), id));
    }

    [Fact]
    public void RepeatedBoundedSearchesReuseStorageAndStillSeeActorsMovingInTheSameTick()
    {
        using var world = WorldCollisionTests.Arena(); var p = world.State.Position; long id = world.State.Party[0];
        var blocker = world.State.Actors.First(a => !a.Recruited); blocker.Position = p.Offset(1, 0);
        var target = p.Offset(7, 0);
        for (int i = 0; i < 20; i++) { world.LocalStep(p, target, id); world.LocalStep(p, p.Offset(100, 0), id); }
        long bytes = GC.GetAllocatedBytesForCurrentThread(); int checksum = 0;
        for (int i = 0; i < 1000; i++) checksum += world.LocalStep(p, target, id).X;
        bytes = GC.GetAllocatedBytesForCurrentThread() - bytes;
        Assert.InRange(bytes, 0, 1024); Assert.True(checksum > 0);
        Assert.NotEqual(blocker.Position, world.LocalStep(p, target, id));
        blocker.Position = p.Offset(-1, 0);
        Assert.Equal(p.Offset(1, 0), world.LocalStep(p, target, id));
        Assert.Equal(p, world.LocalStep(p, p, id));
    }

    // The pre-index implementation is an independent differential oracle for
    // tie order, occupied goals and partial progress at the 256-node boundary.
    private static WorldPosition PreviousStep(WorldSession world, WorldPosition start, WorldPosition destination, long actorId)
    {
        var occupied = world.State.Actors.Where(a => a.Life != LifeState.Dead && a.Id != actorId).Select(a => a.Position).ToHashSet();
        var open = new PriorityQueue<WorldPosition, (int Score, int Tie)>();
        var previous = new Dictionary<WorldPosition, WorldPosition> { [start] = start };
        var costs = new Dictionary<WorldPosition, int> { [start] = 0 };
        WorldPosition best = start; int tie = 0; open.Enqueue(start, (start.Distance(destination), tie++));
        while (open.TryDequeue(out var current, out _) && previous.Count < 256)
        {
            if (current.Distance(destination) < best.Distance(destination)) best = current;
            if (current == destination) break;
            foreach (var (x, y) in WorldSession.Directions)
            {
                var next = current.Offset(x, y); int cost = costs[current] + 1;
                if (!world.Plan.Contains(next) || next.Distance(start) > 24 || occupied.Contains(next) || !world.GroundOpen(next) || costs.TryGetValue(next, out int old) && old <= cost) continue;
                costs[next] = cost; previous[next] = current; open.Enqueue(next, (cost + next.Distance(destination), tie++));
            }
        }
        if (best == start) return start;
        while (previous[best] != start) best = previous[best]; return best;
    }
}
