using System.Text.Json;
using Roag.Core.Tactics;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class BoonRuleTests
{
    private static JsonDocument Reference() => JsonDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Tactics", "Fixtures", "original-boon-rules.json")));

    [Fact]
    public void EveryOriginalRarityStackMagnitudeIsPreserved()
    {
        using var source = Reference();
        foreach (var sample in source.RootElement.GetProperty("aggregates").EnumerateArray())
        {
            var counts = sample.GetProperty("counts").EnumerateArray().Select(v => v.GetInt32()).ToArray();
            var definition = BoonCatalog.Default.Get(sample.GetProperty("id").GetString()!);
            var actual = BoonInventory.Aggregate(definition, new(counts[0], counts[1], counts[2], counts[3]));
            Assert.Equal(sample.GetProperty("chance").GetInt64(), actual.Chance);
            Assert.Equal(sample.GetProperty("power").GetInt64(), actual.Power);
        }
    }

    [Fact]
    public void ChanceOverflowAndExactRandomConsumptionMatchOriginalLua()
    {
        using var source = Reference();
        foreach (var sample in source.RootElement.GetProperty("rolls").EnumerateArray())
        {
            var random = new RoagRandom(sample.GetProperty("seed").GetInt32());
            foreach (var expected in sample.GetProperty("values").EnumerateArray())
                Assert.Equal(expected.GetInt64(), BoonChance.Roll(random, sample.GetProperty("chance").GetInt64(), sample.GetProperty("coefficient").GetDouble()));
            Assert.Equal(sample.GetProperty("state").GetInt32(), random.State);
        }
        var untouched = new RoagRandom(42);
        Assert.Equal(3, BoonChance.Roll(untouched, 3_000_000, 1)); Assert.Equal(42, untouched.State);
        Assert.Equal(0, BoonChance.Roll(untouched, 3_000_000, 0)); Assert.Equal(42, untouched.State);
        Assert.Throws<ArgumentOutOfRangeException>(() => BoonChance.Roll(untouched, 1, double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => BoonChance.Roll(untouched, -1, 1));
    }

    [Fact]
    public void WardsAndSpeedPreserveInclusiveExpiryAndCatalogOrderRegardlessOfDictionaryOrder()
    {
        using var source = Reference();
        var ids = source.RootElement.GetProperty("wardIds").EnumerateArray().Select(v => v.GetString()!).ToArray();
        foreach (var sample in source.RootElement.GetProperty("wards").EnumerateArray())
        {
            var boons = new ActorBoons();
            int[] wards = [2, 7, 100], speeds = [4, 6, 100], expires = [100, 100, 99];
            var order = Enumerable.Range(0, 3);
            if (sample.GetProperty("reverse").GetBoolean()) order = order.Reverse();
            foreach (int i in order)
            {
                boons.Inventory = boons.Inventory.Grant(ids[i], BoonRarity.Common, ClassKind.Adventurer);
                boons.Memory.Add(ids[i], new(ExpiresAt: expires[i], Ward: wards[i], Speed: speeds[i]));
            }
            long clock = sample.GetProperty("clock").GetInt64();
            Assert.Equal(sample.GetProperty("total").GetInt32(), boons.WardTotal(clock));
            Assert.Equal(sample.GetProperty("speed").GetInt32(), boons.SpeedBonus(clock));
            var actual = boons.ConsumeWards(sample.GetProperty("damage").GetInt32(), clock);
            Assert.Equal(sample.GetProperty("absorbed").GetInt32(), actual.Sum(a => a.Amount));
            Assert.Equal(sample.GetProperty("remaining").EnumerateArray().Select(v => v.GetInt32()), ids.Select(id => boons.Memory[id].Ward!.Value));
            boons.Validate(ClassKind.Adventurer);
        }
    }

    [Fact]
    public void DiceAndVowUseOnlyTheirOwnersStacksAndRollsPersistExactly()
    {
        using var source = Reference();
        foreach (var sample in source.RootElement.GetProperty("dice").EnumerateArray())
        {
            var boons = new ActorBoons
            {
                Inventory = new BoonInventory().Grant("core:boon/fateful_dice", BoonRarity.Uncommon, ClassKind.Adventurer, 2)
                    .Grant("core:boon/fateful_dice", BoonRarity.Legendary, ClassKind.Adventurer)
            };
            var random = new RoagRandom(sample.GetProperty("seed").GetInt32());
            long action = 1;
            foreach (var expected in sample.GetProperty("values").EnumerateArray())
            {
                var roll = boons.RollDice(random, action++)!;
                Assert.Equal(expected.GetProperty("face").GetInt32(), roll.Face);
                Assert.Equal(expected.GetProperty("power").GetInt64(), roll.Power);
                boons = JsonSerializer.Deserialize<ActorBoons>(JsonSerializer.Serialize(boons))!;
                boons.Validate(ClassKind.Adventurer, action);
            }
            Assert.Equal(sample.GetProperty("state").GetInt32(), random.State);
        }
        var empty = new ActorBoons(); var noRoll = new RoagRandom(42);
        Assert.Null(empty.RollDice(noRoll, 1)); Assert.Equal(42, noRoll.State); Assert.Null(empty.Vow());
        var owner = new ActorBoons { Inventory = empty.Inventory.Grant("core:boon/pilgrims_vow", BoonRarity.Rare, ClassKind.Adventurer, 2) };
        Assert.Equal(new(2, 12), owner.Vow()); Assert.Null(empty.Vow());
        owner.Memory["core:boon/pilgrims_vow"] = new(Face: 1, RollAction: 1);
        Assert.Throws<InvalidDataException>(() => owner.Validate(ClassKind.Adventurer, 2));
    }
}
