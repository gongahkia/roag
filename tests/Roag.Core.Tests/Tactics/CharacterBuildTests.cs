using System.Text.Json;
using Roag.Core.Tactics;
using Xunit;

namespace Roag.Core.Tests.Tactics;

public sealed class CharacterBuildTests
{
    [Fact]
    public void EveryClassPreservesOriginalCumulativeXpRandomGrowthAndSpecialDamageGrowth()
    {
        using var reference = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,
            "Tactics", "Fixtures", "original-foundation.json")));
        string[] names = ["max_health", "damage", "defense", "speed", "crit_chance", "crit_damage"];
        foreach (var sample in reference.RootElement.GetProperty("progression").EnumerateArray())
        {
            var build = CharacterBuild.Create(ClassCatalog.Get(sample.GetProperty("class").GetString()!).Kind,
                sample.GetProperty("seed").GetInt32());
            var amounts = sample.GetProperty("amounts").EnumerateArray().Select(e => e.GetInt32()).ToArray();
            int step = 0;
            foreach (var expected in sample.GetProperty("steps").EnumerateArray())
            {
                var advance = build.GainExperience(amounts[step++]);
                build = advance.Build;
                build.Validate();
                Assert.Equal(expected.GetProperty("level").GetInt32(), build.Level);
                Assert.Equal(expected.GetProperty("xp").GetInt64(), build.Experience);
                Assert.Equal(expected.GetProperty("random").GetInt32(), build.RandomState);
                Assert.Equal(expected.GetProperty("growth").EnumerateArray().Select(e => e.GetString()),
                    advance.Growth.Select(e => names[(int)e.Stat]));
                var s = build.Stats;
                Assert.Equal(expected.GetProperty("stats").EnumerateArray().Select(e => e.GetInt32()),
                    new[] { s.MaxHealth, s.Damage, s.SpecialDamage, s.Defense, s.Speed,
                        s.CriticalChancePartsPerMillion, s.CriticalDamagePercent });
            }
        }
    }

    [Fact]
    public void DuplicateClassesGrowIndependentlyAndResumeExactly()
    {
        var first = CharacterBuild.Create(ClassKind.Ranger, 42);
        var second = CharacterBuild.Create(ClassKind.Ranger, 99);
        var originalSecond = second;
        first = first.GainExperience(30).Build;
        Assert.Equal(originalSecond, second);
        Assert.Equal(1, second.Level);
        var resumed = JsonSerializer.Deserialize<CharacterBuild>(JsonSerializer.Serialize(first))!;
        resumed.Validate();
        var a = first.GainExperience(200);
        var b = resumed.GainExperience(200);
        Assert.Equal(a.Build, b.Build);
        Assert.Equal(a.Growth, b.Growth);
        Assert.Equal(ClassKind.Ranger, a.Build.Class);
    }

    [Fact]
    public void ChunkingXpDoesNotChangeTheBuildOrConsumeRandomnessBeforeLevelUp()
    {
        var initial = CharacterBuild.Create(ClassKind.Spiritbinder, 1234);
        var partial = initial.GainExperience(4);
        Assert.Empty(partial.Growth);
        Assert.Equal(initial.RandomState, partial.Build.RandomState);
        var bulk = initial.GainExperience(100).Build;
        var incremental = initial;
        for (int i = 0; i < 100; i++) incremental = incremental.GainExperience(1).Build;
        Assert.Equal(bulk, incremental);
        Assert.Equal(0, initial.Experience);
    }

    [Fact]
    public void InvalidPersistedGrowthAndInvalidAwardsAreRejectedWithoutChangingTheBuild()
    {
        var original = CharacterBuild.Create(ClassKind.Adventurer, 42);
        var advanced = original.GainExperience(30).Build;
        Assert.Throws<ArgumentOutOfRangeException>(() => original.GainExperience(0));
        foreach (var invalid in new[] {
            original with { Class = (ClassKind)999 }, original with { Experience = 5 },
            original with { Level = 2 }, original with { RandomState = 0 },
            original with { Stats = original.Stats with { Damage = 999 } },
            original with { Stats = original.Stats with { MaxHealth = 25 } },
            advanced with { Stats = advanced.Stats with { SpecialDamage = 999 } },
            original with { Level = int.MaxValue }
        }) Assert.Throws<InvalidDataException>(invalid.Validate);
        original.Validate();
        Assert.Equal(0, original.Experience);
    }

    [Fact]
    public void HealthGrowthIsAnExplicitDeltaForTheSingleWorldHealthAuthority()
    {
        var result = CharacterBuild.Create(ClassKind.Sentinel, 1).GainExperience(1000);
        Assert.Contains(result.Growth, e => e.MaxHealthIncrease == 4);
        Assert.All(result.Growth, e => Assert.Equal(e.Stat == GrowthStat.MaxHealth ? 4 : 0, e.MaxHealthIncrease));
        Assert.Equal(result.Build.Stats.MaxHealth - ClassCatalog.Get(ClassKind.Sentinel).Health,
            result.Growth.Sum(e => e.MaxHealthIncrease));
    }
}
