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
    public void InteractionPreviewRunsDeterministicallyWithoutPlayerSaves()
    {
        var a = Run("preview", "interaction", "interaction.healing", "--seed", "42");
        var b = Run("preview", "interaction", "interaction.healing", "--seed", "42");
        Assert.Equal(0, a.Exit); Assert.Equal(a.Output, b.Output); Assert.Contains("\"success\": true", a.Output); Assert.Contains("\"randomUnchanged\": true", a.Output);
    }
}
