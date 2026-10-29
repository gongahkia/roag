using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class MirrorRouteTests
{
    [Fact]
    public void CardinalSingleReflectionMatchesOriginalRangeBodiesWallsDirectionsAndLifetime()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-mirror-routes.json")));
        var origin = new WorldPosition(100, 100); int cases = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); var expected = sample.GetProperty("expected");
            int dx = input.GetProperty("dx").GetInt32(), dy = input.GetProperty("dy").GetInt32();
            int length = input.GetProperty("length").GetInt32(), step = input.GetProperty("step").GetInt32();
            string mode = input.GetProperty("mode").GetString()!;
            var delivery = input.GetProperty("delivery").GetString() switch
            { "projectile" => AttackDelivery.Projectile, "piercing" => AttackDelivery.PiercingLine, "area" => AttackDelivery.Area, _ => AttackDelivery.Melee };
            var cells = Enumerable.Range(1, length).Select(i => origin.Offset(dx * i, dy * i)).ToArray();
            var position = origin.Offset(dx * step, dy * step);
            AttackGeometry.TryFacing(mode == "wrong" ? -dx : dx, mode == "wrong" ? -dy : dy, out var facing);
            var mirror = new EnemyMirror(1, 2, mode == "floor" ? position with { Floor = 1 } : position,
                position.Offset(dx, dy), facing, mode == "expired" ? 0 : 400);
            bool Body(WorldPosition p) => p == position.Offset(dx, dy) && mode != "dead"
                || mode == "body_before" && p == cells[0] || mode == "body_marker" && p == position;
            var route = MirrorRoutes.Route(origin, cells, delivery, [mirror], 0,
                p => mode != "wall" || p != cells[0], Body, _ => mode != "dead");
            Assert.Equal(expected.GetProperty("cells").EnumerateArray().Select(c => origin.Offset(c.GetProperty("x").GetInt32(), c.GetProperty("y").GetInt32())), route.Cells);
            Assert.Equal(expected.GetProperty("step").ValueKind == JsonValueKind.Null ? (int?)null : expected.GetProperty("step").GetInt32(), route.Reflection?.Step);
            cases++;
        }
        Assert.Equal(1536, cases);
    }

    [Fact]
    public void ReflectionDoesNotChainMutateInputsOrExposeUnseenMirrorsInPrivatePreviews()
    {
        var origin = new WorldPosition(100, 100); var cells = Enumerable.Range(1, 4).Select(i => origin.Offset(i, 0)).ToArray();
        EnemyMirror[] mirrors = [new(1, 2, new(102, 100), new(103, 100), Facing.East, 400),
            new(2, 3, new(102, 101), new(102, 102), Facing.South, 400)];
        string before = JsonSerializer.Serialize((cells, mirrors));
        for (int i = 0; i < 20; i++)
        {
            var route = MirrorRoutes.Route(origin, cells, AttackDelivery.Projectile, mirrors, 0, _ => true, _ => false, _ => true);
            Assert.Equal(new[] { new WorldPosition(101, 100), new(102, 100), new(102, 101), new(102, 102) }, route.Cells);
            Assert.Equal(1, route.Reflection!.Mirror);
            Assert.Null(MirrorRoutes.Route(origin, cells, AttackDelivery.Projectile, mirrors, 0, _ => true, _ => false, _ => true, p => p != new WorldPosition(102, 100)).Reflection);
        }
        Assert.Equal(before, JsonSerializer.Serialize((cells, mirrors)));
    }
}
