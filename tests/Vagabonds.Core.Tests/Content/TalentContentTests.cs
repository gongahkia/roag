using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Tests.Content;

public sealed class TalentContentTests
{
    [Theory]
    [InlineData("unknown-acquisition")] [InlineData("missing-class")] [InlineData("unknown-class")]
    [InlineData("early-level")] [InlineData("late-level")] [InlineData("null-bonuses")]
    [InlineData("empty-bonuses")] [InlineData("too-many-bonuses")] [InlineData("unknown-effect")]
    [InlineData("zero-bonus")] [InlineData("excess-bonus")] [InlineData("excess-discount")]
    [InlineData("excess-calm")] [InlineData("missing-sprite")] [InlineData("missing-description")]
    public void InvalidTalentEditsReportTheAuthoredSource(string invalid)
    {
        const string id = "talent.warden-bulwark";
        var catalog = ContentTests.Edited(id, node =>
        {
            switch (invalid)
            {
                case "unknown-acquisition": node["acquisition"] = "secret-unlock"; break;
                case "missing-class": node["class"] = null; break;
                case "unknown-class": node["class"] = "class.missing"; break;
                case "early-level": node["level"] = 1; break;
                case "late-level": node["level"] = 5; break;
                case "null-bonuses": node["bonuses"] = null; break;
                case "empty-bonuses": node["bonuses"] = new JsonObject(); break;
                case "too-many-bonuses": node["bonuses"] = new JsonObject { ["protect"] = 1, ["initiative"] = 1, ["healing"] = 1 }; break;
                case "unknown-effect": node["bonuses"] = new JsonObject { ["unregistered-handler"] = 1 }; break;
                case "zero-bonus": node["bonuses"]!["protect"] = 0; break;
                case "excess-bonus": node["bonuses"]!["protect"] = 9; break;
                case "excess-discount": node["bonuses"] = new JsonObject { ["mana-discount"] = 3 }; break;
                case "excess-calm": node["bonuses"] = new JsonObject { ["calm"] = 21 }; break;
                case "missing-sprite": node["sprite"] = null; break;
                case "missing-description": node["description"] = ""; break;
            }
        });
        Assert.Contains(catalog.Issues, issue => issue.File == catalog.Source(id));
    }

    [Theory]
    [InlineData("class.warden", "calm")] [InlineData("class.warden", "burn-damage")]
    [InlineData("class.warden", "all-target-damage")] [InlineData("class.ranger", "calm")]
    public void AClassCannotOfferAChoiceWhoseEffectItCannotUse(string classId, string effect)
    {
        var catalog = ContentTests.Edited("talent.warden-bulwark", node =>
        { node["class"] = classId; node["bonuses"] = new JsonObject { [effect] = 2 }; });
        Assert.Contains(catalog.Issues, issue => issue.Field == "bonuses." + effect && issue.Message.Contains("cannot use"));
    }

    [Fact]
    public void InnateEffectsMustBeUsableByEveryGeneratedClass()
    {
        var catalog = ContentTests.Edited("talent.quick-footed", node => node["bonuses"] = new JsonObject { ["calm"] = 2 });
        Assert.Contains(catalog.Issues, issue => issue.Field == "bonuses.calm" && issue.Message.StartsWith("Warden"));
        Assert.DoesNotContain(catalog.Issues, issue => issue.Field == "bonuses.calm" && issue.Message.StartsWith("Vestal"));
    }

    [Fact]
    public void AValidCatalogNeedsAnInnatePoolAndCanPreviewASingleInnate()
    {
        var documents = ContentTests.Documents();
        bool IsInnate((string File, string Text) document)
        { var node = JsonNode.Parse(document.Text)!; return node["kind"]?.GetValue<string>() == "talent" && node["acquisition"]?.GetValue<string>() == "innate"; }
        var empty = ContentCatalog.Load(documents.Where(d => !IsInnate(d)));
        Assert.Contains(empty.Issues, issue => issue.File == "$catalog" && issue.Field == "talents");
        var single = ContentCatalog.Load(documents.Where(d => !IsInnate(d) || JsonNode.Parse(d.Text)!["id"]!.GetValue<string>() == "talent.quick-footed"));
        Assert.Empty(single.Issues);
        var world = WorldSession.Create(42, single);
        Assert.All(world.State.Actors.Where(a => a.Kind == ActorKind.Person), actor => Assert.Equal("talent.quick-footed", actor.CombatTrait));
        var preview = JsonSerializer.SerializeToNode(TalentPreview.Run(single, "talent.quick-footed", 42), ContentCatalog.Json)!;
        Assert.True(preview["passed"]!.GetValue<bool>());
    }

    [Theory]
    [InlineData("class.arcanist", "abilities")] [InlineData("game.vagabonds", "commonAbilities")]
    [InlineData("game.vagabonds", "cooperativeAbilities")]
    public void PreparedAttacksCannotEnterPlayerActionPools(string id, string field)
    {
        var catalog = ContentTests.Edited(id, node => node[field]![0] = "ability.cinder-surge");
        Assert.Contains(catalog.Issues, issue => issue.File == catalog.Source(id) && issue.Field == field
            && issue.Message.Contains("player actions resolve immediately"));
    }

    [Fact]
    public void CommonAndCooperativeAbilitiesCountTowardsTalentApplicability()
    {
        foreach (string effect in new[] { "healing", "cooperative-damage" })
        {
            var catalog = ContentTests.Edited("talent.warden-bulwark", node => node["bonuses"] = new JsonObject { [effect] = 2 });
            Assert.Empty(catalog.Issues);
            var preview = JsonSerializer.SerializeToNode(TalentPreview.Run(catalog, "talent.warden-bulwark", 42), ContentCatalog.Json)!;
            Assert.True(preview["passed"]!.GetValue<bool>(), preview.ToJsonString());
        }
        Assert.Contains(ContentCatalog.Default.References, reference => reference.Source == "talent.warden-bulwark"
            && reference.Field == "class" && reference.Target == "class.warden");
    }

    [Fact]
    public void ADiscountMustHaveAnAbilityWhoseCostCanActuallyBeReducedByThatAmount()
    {
        var catalog = ContentCatalog.Load(ContentTests.Documents().Select(document =>
        {
            var node = JsonNode.Parse(document.Text)!.AsObject();
            if (node["kind"]!.GetValue<string>() == "ability" && node["mana"]!.GetValue<int>() > 0) node["mana"] = 1;
            return (document.File, Text: node.ToJsonString());
        }));
        Assert.Contains(catalog.Issues, issue => issue.Field == "bonuses.mana-discount" && issue.File == catalog.Source("talent.arcanist-reservoir"));
    }
}
