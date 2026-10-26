using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class NativeFoundationTests
{
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Tactics", "Fixtures", "original-foundation.json")));

    [Fact]
    public void AuthoredClassesPreserveAllOriginalStatsCostsAndActionIdentities()
    {
        using var reference = Reference();
        var originals = reference.RootElement.GetProperty("classes").EnumerateArray().ToArray();
        Assert.Equal(15, originals.Length);
        Assert.Equal(originals.Select(e => e.GetProperty("id").GetString()), ClassCatalog.Ids);
        foreach (var original in originals)
        {
            var actual = ClassCatalog.Get(original.GetProperty("id").GetString()!);
            foreach (var (key, value) in new[] { ("hp", actual.Health), ("damage", actual.Damage),
                ("slash_damage", actual.SpecialDamage), ("defense", actual.Defense), ("speed", actual.Speed),
                ("dash_distance", actual.DashDistance), ("crit_chance_ppm", actual.CriticalChancePartsPerMillion),
                ("crit_damage_percent", actual.CriticalDamagePercent) })
                Assert.Equal(original.GetProperty(key).GetInt32(), value);
            foreach (var (key, value) in new[] { ("name", actual.Name), ("basic_id", actual.BasicAbilityId),
                ("special_id", actual.SpecialAbilityId), ("basic_name", actual.BasicName),
                ("special_name", actual.SpecialName), ("dash_name", actual.DashName) })
                Assert.Equal(original.GetProperty(key).GetString(), value);
            Assert.Equal(original.GetProperty("costs").EnumerateArray().Select(v => v.GetInt32()),
                new[] { actual.Costs.Move, actual.Costs.BasicAttack, actual.Costs.Special, actual.Costs.Dash });
        }
    }

    [Fact]
    public void WorldCoordinateGeometryMatchesOriginalAcrossClassesDirectionsWallsAndCompanionBodies()
    {
        using var reference = Reference();
        var origin = new WorldPosition(1000, 2000, 7);
        foreach (var sample in reference.RootElement.GetProperty("geometry").EnumerateArray())
        {
            var walls = sample.GetProperty("walls").EnumerateArray().Select(Cell).ToHashSet();
            var kind = ClassCatalog.Get(sample.GetProperty("class").GetString()!).Kind;
            var action = sample.GetProperty("action").GetString() == "basic_attack" ? TacticalAction.BasicAttack : TacticalAction.Special;
            var facing = (Facing)sample.GetProperty("facing").GetInt32();
            var (dx, dy) = AttackGeometry.Offset(facing);
            int body = sample.GetProperty("bodyStep").GetInt32();
            var actual = AttackGeometry.For(kind, action, origin, facing, p => !walls.Contains(p),
                p => body > 0 && p == origin.Offset(dx * body, dy * body),
                sample.GetProperty("ownsSpirit").GetBoolean(), sample.GetProperty("explicitAim").GetBoolean());
            var expected = sample.GetProperty("cells").EnumerateArray().Select(Cell).ToArray();
            Assert.True(expected.SequenceEqual(actual.Cells), $"{kind} {action} {facing}: {sample}");
            Assert.Equal(sample.GetProperty("ability").GetString(), actual.AbilityId);
            var element = sample.GetProperty("element").GetString();
            // classes.lua leaves the delayed discharge element unset; game.lua's
            // Storm.resolve callback assigns lightning when that impact fires.
            var expectedElement = kind == ClassKind.Stormcaller && action == TacticalAction.Special
                ? DamageElement.Lightning
                : element is null ? DamageElement.Physical : Enum.Parse<DamageElement>(element, true);
            Assert.Equal(expectedElement, actual.Element);
            // Placement/parry/companion/delayed are now explicit types, rather than nil ranged flags.
            var ranged = sample.GetProperty("ranged");
            if (ranged.ValueKind == JsonValueKind.True) Assert.Equal(AttackDelivery.Projectile, actual.Delivery);
            if (ranged.ValueKind == JsonValueKind.String)
                Assert.Equal(ranged.GetString() == "area" ? AttackDelivery.Area : AttackDelivery.PiercingLine, actual.Delivery);
        }

        static WorldPosition Cell(JsonElement cell) => new(cell[0].GetInt32(), cell[1].GetInt32(), 7);
    }

    [Fact]
    public void RandomStreamsPreserveOriginalRejectionSamplingAndUtf8Derivation()
    {
        using var reference = Reference();
        foreach (var sample in reference.RootElement.GetProperty("random").EnumerateArray())
        {
            var stream = new RoagRandom(sample.GetProperty("seed").GetInt32());
            int maximum = sample.GetProperty("maximum").GetInt32();
            foreach (var expected in sample.GetProperty("values").EnumerateArray())
                Assert.Equal(expected.GetInt32(), stream.NextOneBased(maximum));
            Assert.Equal(sample.GetProperty("state").GetInt32(), stream.State);
            var resumed = new RoagRandom(stream.State);
            Assert.Equal(stream.NextRaw(), resumed.NextRaw());
        }
        foreach (var sample in reference.RootElement.GetProperty("derived").EnumerateArray())
            Assert.Equal(sample.GetProperty("value").GetInt32(), RoagRandom.Derive(
                sample.GetProperty("seed").GetInt32(), sample.GetProperty("label").GetString()!));
    }

    [Theory]
    [InlineData(100, 100, 100L)]
    [InlineData(100, 115, 87L)]
    [InlineData(180, 75, 240L)]
    [InlineData(1, 200, 1L)]
    [InlineData(3, 200, 2L)]
    [InlineData(int.MaxValue, 1, 214748364700L)]
    [InlineData(1, int.MaxValue, 1L)]
    public void RecoveryRoundsHalfUpWithoutOverflow(int cost, int speed, long expected)
        => Assert.Equal(expected, ActionTiming.Recovery(cost, speed));

    [Fact]
    public void InvalidRandomDrawsAndRecoveryCannotMutateStreams()
    {
        var stream = new RoagRandom(42);
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextOneBased(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextOneBased(int.MaxValue));
        Assert.Equal(42, stream.State);
        Assert.Throws<ArgumentOutOfRangeException>(() => ActionTiming.Recovery(0, 100));
        Assert.Throws<ArgumentOutOfRangeException>(() => ActionTiming.Recovery(100, 0));
    }
}
