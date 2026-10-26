using System.Text.Json.Nodes;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Tests.Content;

public sealed class EncounterPopulationContentTests
{
    [Theory]
    [InlineData("roles-count")] [InlineData("unknown-role")] [InlineData("false-preparer")]
    [InlineData("hidden-support")] [InlineData("two-supports")]
    [InlineData("unknown-tier")] [InlineData("zero-weight")] [InlineData("excess-weight")]
    [InlineData("unknown-purpose")] [InlineData("duplicate-purpose")] [InlineData("empty-biomes")]
    [InlineData("duplicate-biome")] [InlineData("surface")] [InlineData("reversed-depth")]
    public void IncompatibleRolesAndInvalidPopulationContextsAreRejected(string invalid)
    {
        var catalog = ContentTests.Edited("encounter.road-wardens", node =>
        {
            switch (invalid)
            {
                case "roles-count": node["roles"] = new JsonArray("support"); break;
                case "unknown-role": node["roles"]![1] = "wizard"; break;
                case "false-preparer": node["roles"]![1] = "preparer"; break;
                case "hidden-support": node["roles"]![0] = "attacker"; break;
                case "two-supports": node["members"]![1] = "character.vow-mender"; node["roles"]![1] = "support"; break;
                case "unknown-tier": node["tier"] = "scales-with-player"; break;
                case "zero-weight": node["weight"] = 0; break;
                case "excess-weight": node["weight"] = 101; break;
                case "unknown-purpose": node["purposes"] = new JsonArray("castle"); break;
                case "duplicate-purpose": node["purposes"] = new JsonArray("ruin", "ruin"); break;
                case "empty-biomes": node["biomes"] = new JsonArray(); break;
                case "duplicate-biome": node["biomes"] = new JsonArray("Forest", "Forest"); break;
                case "surface": node["minDepth"] = 0; break;
                case "reversed-depth": node["minDepth"] = 3; node["maxDepth"] = 2; break;
            }
        });
        Assert.Contains(catalog.Issues, issue => issue.File == catalog.Source("encounter.road-wardens"));
    }

    [Fact]
    public void EveryBiomePurposeAndDepthHasAtLeastTwoEligibleChoices()
    {
        var catalog = ContentCatalog.Default;
        foreach (string purpose in EncounterDefinition.AllowedPurposes) foreach (var biome in Enum.GetValues<Biome>())
            for (int floor = 1; floor < WorldPlan.Floors; floor++)
                Assert.True(catalog.Game.Encounters.Select(catalog.Get<EncounterDefinition>).Count(e => e.Fits(purpose, biome, floor)) >= 2,
                    purpose + "/" + biome + "/" + floor);
        var incomplete = ContentTests.Edited("game.vagabonds", node => node["encounters"] = new JsonArray("encounter.cinder-ambush"));
        Assert.Contains(incomplete.Issues, issue => issue.Field == "encounters" && issue.Message.Contains("at least two eligible"));
    }

    [Fact]
    public void LargerEliteGroupsHaveThematicRewardsExceedingTheirSmallerCounterparts()
    {
        var catalog = ContentCatalog.Default;
        foreach (var (elite, smaller) in new[] { ("cinder-retinue", "cinder-ambush"), ("penitent-procession", "hollow-vigil"), ("broken-guard", "road-wardens") })
        {
            var group = catalog.Get<EncounterDefinition>("encounter." + elite);
            var reward = catalog.Get<RewardDefinition>(group.Reward);
            var ordinary = catalog.Get<RewardDefinition>(catalog.Get<EncounterDefinition>("encounter." + smaller).Reward);
            Assert.Equal("elite", group.Tier); Assert.True(reward.Experience > ordinary.Experience); Assert.True(reward.Gold > ordinary.Gold);
            Assert.Equal(ordinary.EquipmentPool, reward.EquipmentPool); Assert.Null(reward.Signature);
        }
        Assert.Equal(4, catalog.Get<EncounterDefinition>("encounter.cinder-retinue").Members.Length);
    }
}
