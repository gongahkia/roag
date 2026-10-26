using System.Text.Json;
using Roag.Core.Tactics;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class BoonGeometryTests
{
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Tactics", "Fixtures", "original-boon-geometry.json")));
    private static WorldPosition Cell(JsonElement value) => new(value[0].GetInt32(), value[1].GetInt32(), 9);
    private static WorldPosition[] Cells(JsonElement value) => value.EnumerateArray().Select(Cell).ToArray();
    private static AttackDelivery Delivery(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.False => AttackDelivery.Melee,
        JsonValueKind.True => AttackDelivery.Projectile,
        _ => value.GetString() switch { "area" => AttackDelivery.Area, "piercing_line" => AttackDelivery.PiercingLine,
            _ => throw new InvalidDataException("Unexpected reference delivery.") }
    };

    [Fact]
    public void EveryBoonPatternMatchesOriginalOrderingAndBlockedCornerRules()
    {
        using var source = Reference();
        foreach (var sample in source.RootElement.GetProperty("patterns").EnumerateArray())
        {
            var pattern = JsonSerializer.Deserialize<BoonPattern>(sample.GetProperty("pattern").GetRawText(), BoonCatalog.Json);
            var walls = Cells(sample.GetProperty("walls")).ToHashSet();
            var context = new BoonPatternOrigin(new(1000, 2000, 9), Cell(sample.GetProperty("source")),
                sample.GetProperty("hasFrom").GetBoolean() ? new(997, 2002, 9) : null);
            var actual = BoonGeometry.Pattern(pattern, context, p => !walls.Contains(p));
            Assert.Equal(Cells(sample.GetProperty("cells")), actual);
        }
    }

    [Fact]
    public void EveryWeaponFormMatchesOriginalExplicitBumpCompositionAndConcealedBodyRules()
    {
        using var source = Reference();
        var owner = new WorldPosition(1000, 2000, 9);
        foreach (var sample in source.RootElement.GetProperty("weapons").EnumerateArray())
        {
            var form = JsonSerializer.Deserialize<BoonWeaponForm>(sample.GetProperty("form").GetRawText(), BoonCatalog.Json);
            var facing = (Facing)sample.GetProperty("facing").GetInt32();
            var (dx, dy) = AttackGeometry.Offset(facing);
            int step = sample.GetProperty("bodyStep").GetInt32();
            var body = step > 0 ? owner.Offset(dx * step, dy * step) : (WorldPosition?)null;
            var walls = Cells(sample.GetProperty("walls")).ToHashSet();
            var actual = BoonGeometry.Weapon(form, owner, facing, Cells(sample.GetProperty("current")), Cells(sample.GetProperty("base")),
                Delivery(sample.GetProperty("inputMode")), p => !walls.Contains(p), p => body == p,
                sample.GetProperty("explicit").GetBoolean(), sample.GetProperty("knownOnly").GetBoolean(),
                p => body != p || !sample.GetProperty("hidden").GetBoolean());
            Assert.Equal(Cells(sample.GetProperty("cells")), actual.Cells);
            Assert.Equal(Delivery(sample.GetProperty("mode")), actual.Delivery);
            Assert.Equal(sample.GetProperty("hint").GetBoolean(), actual.Form != null);
        }
    }

    [Fact]
    public void WorldGeometryCannotConnectDifferentFloorsOrInventMissingDashOrigins()
    {
        Assert.False(BoonGeometry.ClearSegment(new(1, 1, 1), new(1, 2, 2), _ => true));
        Assert.False(BoonGeometry.Axis(new(1, 1, 1), new(1, 2, 2), BoonAxis.Cardinal));
        Assert.Empty(BoonGeometry.Pattern(BoonPattern.Backtrail, new(new(1, 1)), _ => true));
        Assert.Empty(BoonGeometry.Pattern(BoonPattern.Lance, new(new(1, 1, 1), new(1, 2, 2)), _ => true));
    }

    [Fact]
    public void AbilityModifiersAndTheirCombinationsMatchOriginalClassAndBodyQueries()
    {
        using var source = Reference();
        var owner = new WorldPosition(1000, 2000, 9);
        foreach (var sample in source.RootElement.GetProperty("abilities").EnumerateArray())
        {
            var kind = ClassCatalog.Get(sample.GetProperty("class").GetString()!).Kind;
            var inventory = new BoonInventory();
            foreach (var id in sample.GetProperty("boons").EnumerateArray())
                inventory = inventory.Grant(id.GetString()!, BoonRarity.Common, kind).Grant(id.GetString()!, BoonRarity.Rare, kind);
            var walls = Cells(sample.GetProperty("walls")).ToHashSet();
            var bodies = sample.GetProperty("bodies").EnumerateArray().Select(b => new BoonBody(b.GetProperty("id").GetInt64(),
                new(b.GetProperty("x").GetInt32(), b.GetProperty("y").GetInt32(), 9), b.GetProperty("hostile").GetBoolean(),
                b.GetProperty("burning").GetBoolean(), b.GetProperty("visible").GetBoolean())).ToArray();
            var footprint = new AttackFootprint(sample.GetProperty("ability").GetString()!, Delivery(sample.GetProperty("inputMode")),
                DamageElement.Physical, Cells(sample.GetProperty("base")));
            var actual = BoonAbilityGeometry.Apply(inventory, 1, owner, footprint, sample.GetProperty("baseDamage").GetInt32(),
                (Facing)sample.GetProperty("facing").GetInt32(), bodies, p => !walls.Contains(p), p => !walls.Contains(p),
                sample.GetProperty("explicit").GetBoolean(), sample.GetProperty("knownOnly").GetBoolean());
            Assert.Equal(Cells(sample.GetProperty("cells")), actual.Cells);
            Assert.Equal(sample.GetProperty("damage").GetInt32(), actual.Damage);
            Assert.Equal(Delivery(sample.GetProperty("mode")), actual.Delivery);
            Assert.Equal(sample.GetProperty("hint").GetBoolean(), actual.Form != null);
        }
    }
}
