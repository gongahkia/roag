using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class EnemyPlanningTests
{
    [Fact]
    public void AllFiftyDefinitionsPreserveTheOriginalRegisteredEnemyData()
    {
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-enemy-catalog.json")));
        var catalog = EnemyCatalog.Default;
        Assert.Equal(50, catalog.Definitions.Length);
        foreach (var entry in source.RootElement.GetProperty("entries").EnumerateObject())
        {
            var original = entry.Value; var definition = catalog.Get(entry.Name); var tactics = definition.CreateTactics();
            Assert.Equal(original.GetProperty("name").GetString(), definition.Name);
            Assert.Equal(original.GetProperty("ai").GetString(), JsonNamingPolicy.SnakeCaseLower.ConvertName(definition.Ai.ToString()));
            Assert.Equal(original.GetProperty("hp").GetInt32(), tactics.Stats.MaxHealth);
            Assert.Equal(original.GetProperty("damage").GetInt32(), tactics.Stats.Damage);
            Assert.Equal(original.GetProperty("defense").GetInt32(), tactics.Stats.Defense);
            Assert.Equal(original.GetProperty("speed").GetInt32(), tactics.Stats.Speed);
            Assert.Equal(original.GetProperty("windup").GetInt32(), definition.Windup);
            Assert.Equal(original.GetProperty("recovery").GetInt32(), definition.Recovery);
            Assert.Equal(original.GetProperty("move_cost").GetInt32(), tactics.MoveCost);
            Assert.Equal(original.GetProperty("range").GetInt32(), definition.Range);
            Assert.Equal(original.GetProperty("xp_reward").GetInt32(), tactics.ExperienceReward);
            Assert.Equal(original.GetProperty("currency_drop").GetInt32(), tactics.CurrencyDrop);
            Assert.Equal(original.TryGetProperty("min_range", out var min) ? min.GetInt32() : 1, definition.MinRange);
            Assert.Equal(original.TryGetProperty("invulnerable", out var invulnerable) && invulnerable.GetBoolean(), tactics.Invulnerable);
            Assert.Equal(original.TryGetProperty("special_encounter", out var special) && special.GetBoolean(), definition.SpecialEncounter);
            Assert.NotNull(tactics.Enemy); tactics.Validate();
        }
        Assert.False(catalog.Get("core:enemy/rotating_sentinel").Regular);
        Assert.False(catalog.Get("core:enemy/thorn_warden").Regular);
        Assert.True(catalog.Get("core:enemy/mossbound_guard").Regular);
        Assert.True(catalog.Get("core:enemy/thorn_warden").PullImmune);
        Assert.False(catalog.Get("core:enemy/rotating_sentinel").PullImmune);
    }

    [Fact]
    public void BasicAndEliteDecisionsMatchAllOriginalGeometryNavigationAndMistCases()
    {
        using var source = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Tactics/Fixtures/original-enemy-decisions.json")));
        var origin = new WorldPosition(1000, 1000, 2); int count = 0;
        foreach (var sample in source.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = sample.GetProperty("input"); var expected = sample.GetProperty("expected");
            var target = origin.Offset(input.GetProperty("dx").GetInt32(), input.GetProperty("dy").GetInt32());
            var walls = ReadCells(input, "walls", origin); var mist = ReadCells(input, "mist", origin); var bodies = ReadCells(input, "bodies", origin);
            bodies.Add(target);
            bool Open(WorldPosition p) => p.Floor == origin.Floor && Math.Abs(p.X - origin.X) <= 8 && Math.Abs(p.Y - origin.Y) <= 8 && !walls.Contains(p);
            bool Free(WorldPosition p) => Open(p) && !bodies.Contains(p);
            var aim = input.GetProperty("decoy").GetBoolean() ? origin.Offset(-4, 0) : target;
            var memory = new EnemyMemory { Attacks = input.GetProperty("attacks").GetInt64(), PhaseTwo = input.GetProperty("phase_two").GetBoolean() };
            string before = JsonSerializer.Serialize(memory);
            var actual = EnemyPlanning.Choose(EnemyCatalog.Default.Get(input.GetProperty("id").GetString()!), memory,
                new(origin, aim, Free, Open, mist.Contains, goals => FirstStep(origin, goals, Free)));
            Assert.Equal(expected.GetProperty("kind").GetString(), actual.Kind.ToString().ToLowerInvariant());
            if (actual.Kind == EnemyIntentKind.Move)
                Assert.Equal((expected.GetProperty("dx").GetInt32(), expected.GetProperty("dy").GetInt32()), AttackGeometry.Offset(actual.Facing));
            if (actual.Kind == EnemyIntentKind.Windup)
            {
                Assert.Equal(expected.GetProperty("ability").GetString(), actual.Ability);
                Assert.Equal(expected.GetProperty("delivery").GetString(), JsonNamingPolicy.SnakeCaseLower.ConvertName(actual.Delivery.ToString()));
                Assert.Equal(expected.GetProperty("cells").EnumerateArray().Select(p => origin.Offset(p.GetProperty("x").GetInt32(), p.GetProperty("y").GetInt32())), actual.Cells);
            }
            Assert.Equal(before, JsonSerializer.Serialize(memory));
            count++;
        }
        Assert.Equal(2400, count);
    }

    private static HashSet<WorldPosition> ReadCells(JsonElement input, string key, WorldPosition origin) => input.GetProperty(key)
        .EnumerateArray().Select(p => origin.Offset(p[0].GetInt32(), p[1].GetInt32())).ToHashSet();

    // Test navigation policy supplies exact finite-board BFS, independent of the controller's goal construction.
    internal static Facing? FirstStep(WorldPosition origin, IReadOnlySet<WorldPosition> goals, Func<WorldPosition, bool> free)
    {
        if (goals.Count == 0) return null;
        var seen = new HashSet<WorldPosition> { origin };
        var queue = new Queue<(WorldPosition Position, Facing? First)>(); queue.Enqueue((origin, null));
        while (queue.TryDequeue(out var current))
            foreach (var facing in new[] { Facing.North, Facing.East, Facing.South, Facing.West })
            {
                var (x, y) = AttackGeometry.Offset(facing); var next = current.Position.Offset(x, y);
                if (seen.Contains(next) || !free(next)) continue;
                seen.Add(next); var first = current.First ?? facing;
                if (goals.Contains(next)) return first;
                queue.Enqueue((next, first));
            }
        return null;
    }

    [Fact]
    public void EveryOriginalControllerIsSupportedAndMalformedContentStillFails()
    {
        var definition = EnemyCatalog.Default.Get("core:enemy/mirror_hexer");
        Assert.All(EnemyCatalog.Default.Definitions, d => Assert.True(EnemyPlanning.Supported(d), d.Id));
        Assert.Throws<InvalidDataException>(() => new EnemyCatalog([definition, definition]));
        Assert.Throws<InvalidDataException>(() => new EnemyCatalog([definition with { Windup = 0 }]));
        Assert.Throws<InvalidDataException>(() => EnemyCatalog.Load("{\"schema_version\":1,\"source_version\":\"x\",\"definitions\":[{}]}"));
        var tactics = definition.CreateTactics(); tactics.Enemy!.Attacks = -1;
        Assert.Throws<InvalidDataException>(tactics.Validate);
    }
}
