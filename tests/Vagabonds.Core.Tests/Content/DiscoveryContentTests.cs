using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Tests.Content;

public sealed class DiscoveryContentTests
{
    [Theory]
    [InlineData("action")]
    [InlineData("reference")]
    [InlineData("layout")]
    [InlineData("reward")]
    [InlineData("supplies")]
    [InlineData("enemy")]
    [InlineData("null")]
    public void BadDiscoveryContentReportsItsSourceBeforeWorldCreation(string kind)
    {
        var catalog = ContentTests.Edited("discovery.prowlers-den", n =>
        {
            switch (kind)
            {
                case "action": n["action"] = "execute-code"; break;
                case "reference": n["text"] = "This belongs to {imaginary-world-id}."; break;
                case "layout": n["layout"] = new JsonArray("##", "##"); break;
                case "reward": n["rewards"] = new JsonArray("item.food", "item.wood", "item.stone"); break;
                case "supplies": n["supplies"] = JsonNode.Parse("{\"item.food\":-1}"); break;
                case "enemy": n["enemies"] = new JsonArray("character.villager"); break;
                case "null": n["layout"] = null; break;
            }
        });
        Assert.NotEmpty(catalog.Issues); Assert.Contains(catalog.Issues, i => i.File.Contains("prowlers-den", StringComparison.Ordinal));
        Assert.Throws<InvalidDataException>(() => WorldSession.Create(42, catalog));
    }
    [Fact]
    public void EveryAuthoredDiscoveryRunsThroughTheRealPreviewAndUsesActualWorldReferences()
    {
        foreach (var definition in ContentCatalog.Default.All<DiscoveryDefinition>())
        {
            var report = JsonSerializer.SerializeToNode(ExplorationPreview.Discovery(ContentCatalog.Default, definition.Id, 42), ContentCatalog.Json)!;
            Assert.True(report["visit"]!["success"]!.GetValue<bool>());
            Assert.True(report["unchangedClockAndRandom"]!.GetValue<bool>());
            Assert.DoesNotContain("{", report["text"]!.GetValue<string>());
            if (report["guarded"]!.GetValue<bool>()) Assert.False(report["claim"]!["success"]!.GetValue<bool>());
            else if (report["claim"] is { } claim) Assert.True(claim["success"]!.GetValue<bool>());
        }
    }
}
