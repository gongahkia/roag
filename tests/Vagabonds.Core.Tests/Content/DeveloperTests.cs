using System.Text.Json;
using Vagabonds.Core.Content;
using Xunit;

namespace Vagabonds.Tests.Content;

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
            var report = JsonSerializer.SerializeToNode(Vagabonds.Core.World.AbilityPreview.Run(ContentCatalog.Default, ability.Id, 42), ContentCatalog.Json)!;
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
