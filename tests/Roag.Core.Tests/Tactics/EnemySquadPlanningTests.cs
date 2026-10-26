using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class EnemySquadPlanningTests
{
    [Fact]
    public void SquadControllersMatchOriginalIdentityTerrainLeashesHealingAndTraps()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-squad-decisions.json")));
        var origin = new WorldPosition(1000, 1000, 2); int count = 0;
        foreach (var sample in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); var expected = sample.GetProperty("expected");
            string Text(string key) => input.GetProperty(key).GetString()!;
            int Number(string key) => input.GetProperty(key).GetInt32();
            HashSet<WorldPosition> Cells(string key) => input.GetProperty(key).EnumerateArray().Select(p => origin.Offset(p[0].GetInt32(), p[1].GetInt32())).ToHashSet();
            var kind = Enum.GetValues<EnemySquadKind>().Single(k => JsonNamingPolicy.SnakeCaseLower.ConvertName(k.ToString()) == Text("group"));
            var group = new EnemySquad("test", kind, Number("leader"), origin);
            var target = origin.Offset(Number("dx"), Number("dy"));
            var neighbors = input.GetProperty("neighbors").EnumerateArray().Select(n => new EnemyNeighbor(n.GetProperty("id").GetInt64(),
                origin.Offset(n.GetProperty("dx").GetInt32(), n.GetProperty("dy").GetInt32()), n.GetProperty("hp").GetInt32(), n.GetProperty("max_hp").GetInt32(),
                n.GetProperty("ally").GetBoolean(), n.GetProperty("definition").GetString()!, group with { Id = n.GetProperty("group").GetString()! }))
                .Append(new(1, target, 30, 32, false)).ToArray();
            var traps = input.GetProperty("snares").EnumerateArray().Select(t => new EnemySnare(t.GetProperty("source").GetInt64(),
                origin.Offset(t.GetProperty("dx").GetInt32(), t.GetProperty("dy").GetInt32()))).ToArray();
            var walls = Cells("walls"); var mist = Cells("mist");
            bool Open(WorldPosition p) => p.Floor == origin.Floor && Math.Abs(p.X - origin.X) <= 8 && Math.Abs(p.Y - origin.Y) <= 8 && !walls.Contains(p);
            bool Free(WorldPosition p) => Open(p) && !neighbors.Any(n => n.Health > 0 && n.Position == p);
            bool TargetFree(WorldPosition p) => Open(p) && p != origin && !neighbors.Any(n => n.Id != 1 && n.Health > 0 && n.Position == p);
            var memory = new EnemyMemory { Squad = group, WardReady = input.GetProperty("ward").GetBoolean() };
            string before = JsonSerializer.Serialize(memory);
            var intent = EnemyPlanning.Choose(EnemyCatalog.Default.Get(Text("id")), memory, new(origin,
                input.GetProperty("decoy").GetBoolean() ? origin.Offset(-4, 0) : target, Free, Open, mist.Contains,
                goals => EnemyPlanningTests.FirstStep(origin, goals, Free), Neighbors: neighbors,
                Self: new(Number("actor"), origin, Number("hp"), 10, true, Text("id"), group), Snares: traps, TargetFree: TargetFree));
            Assert.True(expected.GetProperty("kind").GetString() == intent.Kind.ToString().ToLowerInvariant(), $"Case {count}: {input}");
            if (intent.Kind == EnemyIntentKind.Move)
                Assert.Equal((expected.GetProperty("dx").GetInt32(), expected.GetProperty("dy").GetInt32()), AttackGeometry.Offset(intent.Facing));
            if (intent.Kind == EnemyIntentKind.Windup)
            {
                Assert.Equal(expected.GetProperty("ability").GetString(), intent.Ability);
                Assert.Equal(expected.GetProperty("delivery").GetString(), JsonNamingPolicy.SnakeCaseLower.ConvertName(intent.Delivery.ToString()));
                Assert.Equal(expected.GetProperty("cells").EnumerateArray().Select(c => origin.Offset(c.GetProperty("x").GetInt32(), c.GetProperty("y").GetInt32())), intent.Cells);
            }
            var support = expected.GetProperty("support");
            if (support.ValueKind == JsonValueKind.Null) Assert.Null(intent.Support);
            else Assert.Equal(new EnemySupport(Enum.Parse<EnemySupportKind>(support.GetProperty("kind").GetString()!, true),
                support.GetProperty("power").GetInt32(), Target: support.GetProperty("target").GetInt64()), intent.Support);
            if (expected.GetProperty("shield_dx").ValueKind == JsonValueKind.Null) Assert.Null(intent.ShieldFacing);
            else Assert.Equal((expected.GetProperty("shield_dx").GetInt32(), expected.GetProperty("shield_dy").GetInt32()), AttackGeometry.Offset(intent.ShieldFacing!.Value));
            Assert.Equal(before, JsonSerializer.Serialize(memory)); count++;
        }
        Assert.Equal(3200, count);
    }
}
