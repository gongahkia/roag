using System.Text.Json;
using Roag.Core.Content;
using Xunit;

namespace Roag.Tests.Content;

[CollectionDefinition("Developer CLI", DisableParallelization = true)]
public sealed class DeveloperCollection;

[Collection("Developer CLI")]
public sealed class DeveloperTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-authoring-" + Guid.NewGuid().ToString("N"));
    public DeveloperTests()
    {
        Directory.CreateDirectory(_root);
        foreach (var definition in ContentCatalog.Default.Entries.Values)
            File.WriteAllText(Path.Combine(_root, definition.Id + ".json"), JsonSerializer.Serialize<ContentDefinition>(definition, ContentCatalog.Json));
    }
    public void Dispose() => Directory.Delete(_root, true);
    private (int Exit, string Output) Run(params string[] args)
    {
        var original = Console.Out; using var output = new StringWriter(); Console.SetOut(output);
        try { int exit = DeveloperEntry.Run([.. args, "--content", _root]); return (exit, output.ToString()); }
        finally { Console.SetOut(original); }
    }
    [Fact]
    public void PerformanceWorkloadsRetainDeterministicStateAndRejectInvalidSampleCounts()
    {
        var a = Run("benchmark", "--samples", "3"); var b = Run("benchmark", "--samples", "3");
        Assert.Equal(0, a.Exit); Assert.Equal(0, b.Exit);
        var first = JsonSerializer.Deserialize<JsonElement>(a.Output); var second = JsonSerializer.Deserialize<JsonElement>(b.Output);
        Assert.True(first.GetProperty("inspectionReadOnly").GetBoolean());
        Assert.Equal(first.GetProperty("simulationHash").GetString(), second.GetProperty("simulationHash").GetString());
        Assert.Equal(first.GetProperty("checksum").GetInt64(), second.GetProperty("checksum").GetInt64());
        Assert.Equal(3, first.GetProperty("results").GetArrayLength());
        foreach (var result in first.GetProperty("results").EnumerateArray())
        {
            double limit = result.GetProperty("name").GetString() == "adjacent-work-tick" ? 200_000 : 16_000;
            Assert.True(result.GetProperty("bytesPerOperation").GetDouble() < limit, result.ToString());
        }
        Assert.Equal(1, Run("benchmark", "--samples", "0").Exit);
        Assert.Equal(1, Run("benchmark", "--samples", "101").Exit);
    }
    [Fact]
    public void AgentCanScaffoldValidateInspectAndRemoveUnusedContentWithoutOverwriting()
    {
        int files = Directory.GetFiles(_root).Length;
        Assert.Equal(1, Run("content", "scaffold", "item", "--id", "item.bad/../../escape").Exit);
        Assert.Equal(files, Directory.GetFiles(_root).Length);
        string path = Path.Combine(_root, "new-item.json");
        Assert.Equal(0, Run("content", "scaffold", "item", "--id", "item.testing", "--output", path).Exit);
        byte[] before = File.ReadAllBytes(path);
        Assert.Equal(1, Run("content", "scaffold", "item", "--id", "item.testing", "--output", path).Exit); Assert.Equal(before, File.ReadAllBytes(path));
        Assert.Equal(0, Run("content", "validate").Exit); Assert.Contains("item.testing", Run("content", "inspect", "item.testing").Output);
        Assert.Equal(0, Run("content", "remove", "item.testing").Exit); Assert.True(File.Exists(path));
        Assert.Equal(0, Run("content", "remove", "item.testing", "--apply").Exit); Assert.False(File.Exists(path));
    }
    [Theory]
    [InlineData("talent")]
    [InlineData("encounter")]
    [InlineData("reward")]
    public void PartyAndEncounterFamiliesSupportValidatedAuthoring(string kind)
    {
        string id = kind + ".tooling-test", path = Path.Combine(_root, id + ".json");
        Assert.Equal(0, Run("content", "scaffold", kind, "--id", id, "--output", path).Exit);
        Assert.Equal(0, Run("content", "validate").Exit);
        Assert.Contains(id, Run("content", "inspect", id).Output);
        Assert.Equal(0, Run("preview", kind, id).Exit);
        Assert.Equal(0, Run("content", "remove", id, "--apply").Exit);
        Assert.False(File.Exists(path));
    }
    [Fact]
    public void RemovingTheLastInnateCannotLeaveGeneratedPeopleWithoutAValidTrait()
    {
        var innates = ContentCatalog.Default.All<TalentDefinition>().Where(t => t.Acquisition == "innate").ToArray();
        foreach (var definition in innates.Skip(1))
            Assert.Equal(0, Run("content", "remove", definition.Id, "--apply").Exit);
        string last = innates[0].Id, path = Path.Combine(_root, last + ".json");
        byte[] before = File.ReadAllBytes(path);
        var rejected = Run("content", "remove", last, "--apply");
        Assert.Equal(1, rejected.Exit); Assert.Contains("innateCombatTalent", rejected.Output);
        Assert.Equal(before, File.ReadAllBytes(path)); Assert.Equal(0, Run("content", "validate").Exit);
    }
    [Fact]
    public void RemovingReferencedDefinitionsReportsTheirDependentsAndPreservesFiles()
    {
        string path = Path.Combine(_root, "ability.longsword.json"); byte[] before = File.ReadAllBytes(path);
        var result = Run("content", "remove", "ability.longsword", "--apply"); Assert.Equal(1, result.Exit); Assert.Contains("class.warden", result.Output);
        Assert.Equal(before, File.ReadAllBytes(path)); Assert.Equal(1, Run("content", "remove", "game.vagabonds", "--apply").Exit);
    }
    [Fact]
    public void SchemaGenerationIsReproducibleAndReplacementIsExplicit()
    {
        string path = Path.Combine(_root, "schema.json");
        Assert.Equal(0, Run("content", "schema", "--output", path).Exit); byte[] before = File.ReadAllBytes(path);
        Assert.Equal(1, Run("content", "schema", "--output", path).Exit); Assert.Equal(before, File.ReadAllBytes(path));
        Assert.Equal(0, Run("content", "schema", "--output", path, "--replace").Exit); Assert.Equal(before, File.ReadAllBytes(path));
        Assert.Equal(File.ReadAllText(Path.Combine(ContentTests.Root, "content/schema.json")), File.ReadAllText(path));
    }
    [Fact]
    public void AbilityPreviewsResolveActualActionsAndToolTyposFailClearly()
    {
        foreach (var ability in ContentCatalog.Default.All<AbilityDefinition>())
        {
            var report = JsonSerializer.SerializeToNode(Roag.Core.World.AbilityPreview.Run(ContentCatalog.Default, ability.Id, 42), ContentCatalog.Json)!;
            Assert.True(report["result"]!["success"]!.GetValue<bool>(), ability.Id);
        }
        Assert.Equal(1, Run("contnet", "validate").Exit);
        Assert.Equal(1, Run("content", "validate", "--sead", "42").Exit);
        Assert.Equal(1, Run("content", "validate", "--seed", "invalid").Exit);
        Assert.Equal(1, Run("preview", "ability", "ability.mend", "--seed").Exit);
    }
    [Theory]
    [InlineData("item", "item.chainmail", "equipped")]
    [InlineData("trait", "trait.stubborn", "workRate")]
    [InlineData("names", "names.travellers", "background")]
    [InlineData("class", "class.arcanist", "mage")]
    [InlineData("variant", "variant.swift", "variant.swift")]
    [InlineData("game", "game.vagabonds", "guardians")]
    [InlineData("discovery", "discovery.waycamp", "unchangedClockAndRandom")]
    [InlineData("dialogue", "dialogue.stories", "unchangedStateDuringSpeech")]
    [InlineData("party-work", "", "maxWorkers")]
    public void SupportingContentCanBePreviewedThroughRuntimeContexts(string kind, string id, string expected)
    {
        var result = Run("preview", kind, id, "--seed", "42"); Assert.Equal(0, result.Exit);
        Assert.Contains(expected, result.Output, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public void InteractionPreviewRunsDeterministicallyWithoutPlayerSaves()
    {
        var a = Run("preview", "interaction", "interaction.healing", "--seed", "42");
        var b = Run("preview", "interaction", "interaction.healing", "--seed", "42");
        Assert.Equal(0, a.Exit); Assert.Equal(a.Output, b.Output); Assert.Contains("\"success\": true", a.Output); Assert.Contains("\"randomUnchanged\": true", a.Output);
    }
}
