using Roag.Core.Dungeons;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class DungeonPassageSealTests
{
    [Theory]
    [InlineData(1)] [InlineData(42)] [InlineData(-2147483648)]
    public void InitialSealsSeparateAnnexesAndUnchosenBranchesWithoutChangingAuthoredRooms(int seed)
    {
        var plan = WorldPlan.Generate(seed, false, floorCount: 8);
        var blueprint = DungeonComplexPlanner.Generate(plan, DungeonParcelPlanner.Reserve(plan)[0]);
        var open = new HashSet<WorldPosition>();
        foreach (var floor in blueprint.Floors)
            for (int y = 0; y < floor.Layout.Height; y++) for (int x = 0; x < floor.Layout.Width; x++)
                if (floor.Layout.Rows[y][x] is not ('#' or '~' or 'B')) open.Add(floor.Origin.Offset(x, y));
        var final = blueprint.Finale; var map = DungeonCatalog.Default.Finale;
        for (int y = 0; y < map.Height; y++) for (int x = 0; x < map.Width; x++)
            if (map.Rows[y][x] != '#') open.Add(final.Origin.Offset(x, y));
        var authoredOpen = new HashSet<WorldPosition>(open);
        foreach (var passage in blueprint.Passages)
        {
            open.UnionWith(passage.UpperCells);
            foreach (var route in passage.Routes) open.UnionWith(route.Cells);
        }
        foreach (var passage in blueprint.Passages)
        {
            open.Remove(passage.Barrier);
            foreach (var route in passage.Routes) open.Remove(route.Barrier);
        }
        foreach (var passage in blueprint.Passages)
        {
            AssertNoFloorAccess(passage.Top);
            AssertNoFloorAccess(passage.Bottom);
        }

        void AssertNoFloorAccess(WorldPosition start)
        {
            var seen = new HashSet<WorldPosition> { start }; var queue = new Queue<WorldPosition>(); queue.Enqueue(start);
            while (queue.TryDequeue(out var cell))
            {
                Assert.False(authoredOpen.Contains(cell),
                    $"Sealed passage from {start} leaks into original walkable terrain at {cell}.");
                foreach (var (dx, dy) in new[] { (0, -1), (1, 0), (0, 1), (-1, 0) })
                {
                    var next = cell.Offset(dx, dy);
                    if (open.Contains(next) && seen.Add(next)) queue.Enqueue(next);
                }
            }
        }
    }
}
