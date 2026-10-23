using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class EncounterContentTests
{
    private static ContentCatalog Changed(string id, Action<JsonObject> change) => ContentCatalog.Load(
        ContentCatalog.Default.Entries.Values.Select(d =>
        {
            var node = JsonSerializer.SerializeToNode<ContentDefinition>(d, ContentCatalog.Json)!.AsObject();
            if (d.Id == id) change(node);
            return (File: ContentCatalog.Default.Source(d.Id), Text: node.ToJsonString());
        }));

    [Theory]
    [InlineData("members", "character.villager")] [InlineData("members", "item.iron-sword")]
    [InlineData("reward", "item.iron-sword")] [InlineData("reward", "reward.missing")]
    public void GroupReferencesMustUseHostileMembersAndRealRewardProfiles(string field, string value)
    {
        var catalog = Changed("encounter.cinder-ambush", node => node[field] = field == "members" ? new JsonArray(value) : JsonValue.Create(value));
        Assert.Contains(catalog.Issues, issue => issue.Field == field);
    }

    [Theory]
    [InlineData("equipmentPool")] [InlineData("signature")]
    public void EquipmentRewardsCannotContainConsumables(string field)
    {
        var catalog = Changed("reward.cinder-ambush", node => node[field] = field == "equipmentPool" ? new JsonArray("item.food") : JsonValue.Create("item.food"));
        Assert.Contains(catalog.Issues, issue => issue.Field == field);
    }

    [Theory]
    [InlineData("preparationTurns", 2)] [InlineData("preparationTurns", -1)]
    public void PreparationIsBoundedToOneDeclaredTurn(string field, int value)
    {
        var catalog = Changed("ability.cinder-surge", node => node[field] = value);
        Assert.Contains(catalog.Issues, issue => issue.Field == field);
    }

    [Fact]
    public void UnknownConditionsAndFriendlyPreparedAbilitiesAreRejected()
    {
        Assert.Contains(Changed("character.vow-mender", node => node["behaviors"]![0]!["condition"] = "secret-rule").Issues,
            issue => issue.Field == "behaviors");
        Assert.Contains(Changed("ability.cinder-surge", node => node["target"] = "Ally").Issues,
            issue => issue.Field == "preparationTurns");
    }

    [Fact]
    public void NewReferencesAreDiscoverableForSafeContentRemoval()
    {
        var catalog = ContentCatalog.Default;
        Assert.Contains(catalog.References, r => r.Source == "game.vagabonds" && r.Target == "encounter.cinder-ambush");
        Assert.Contains(catalog.References, r => r.Source == "encounter.cinder-ambush" && r.Target == "character.cinder-channeler");
        Assert.Contains(catalog.References, r => r.Source == "character.cinder-channeler" && r.Target == "ability.cinder-surge");
        Assert.Contains(catalog.References, r => r.Source == "reward.guardian-cinder" && r.Target == "item.cinder-sceptre");
        Assert.All(catalog.All<CharacterDefinition>().Where(c => c.Ecology != null), character =>
            Assert.Contains(catalog.References, r => r.Source == character.Id && r.Target == "reward.wildlife"));
    }

    [Fact]
    public void OversizedSupplyListsAndUntaggedSignaturesAreRejected()
    {
        var supplies = new JsonObject();
        foreach (var item in ContentCatalog.Default.All<ItemDefinition>().Where(i => i.Slot == "none").Take(7)) supplies[item.Id] = 1;
        Assert.Equal(7, supplies.Count);
        Assert.Contains(Changed("reward.cinder-ambush", node => node["supplies"] = supplies).Issues,
            issue => issue.Field == "supplies");
        Assert.Contains(Changed("reward.cinder-ambush", node => node["signature"] = "item.iron-sword").Issues,
            issue => issue.Field == "signature");
    }

    [Fact]
    public void NonRepeatCannotContradictFallbackAndMarkedConditionsNeedEnemyTargets()
    {
        Assert.Contains(Changed("character.vow-mender", node => node["behaviors"]![1]!["noRepeat"] = true).Issues,
            issue => issue.Field == "behaviors");
        Assert.Contains(Changed("character.vow-mender", node => node["behaviors"]![0]!["condition"] = "target-marked").Issues,
            issue => issue.Field == "behaviors");
    }
}
