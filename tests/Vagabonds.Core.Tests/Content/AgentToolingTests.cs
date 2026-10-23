using System.Text.Json;
using System.Text.Json.Nodes;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Tests.Content;

[Collection("Developer CLI")]
public sealed class AgentToolingTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "vagabonds-agent-" + Guid.NewGuid().ToString("N"));
    private string ContentRoot => Path.Combine(_root, "content");
    public AgentToolingTests()
    {
        Directory.CreateDirectory(ContentRoot);
        foreach (var definition in ContentCatalog.Default.Entries.Values)
            File.WriteAllText(Path.Combine(ContentRoot, definition.Id + ".json"), JsonSerializer.Serialize<ContentDefinition>(definition, ContentCatalog.Json));
    }
    public void Dispose() => Directory.Delete(_root, true);
    private (int Exit, JsonNode Json) Run(params string[] args)
    {
        var original = Console.Out; using var output = new StringWriter(); Console.SetOut(output);
        try { int exit = DeveloperEntry.Run([.. args, "--content", ContentRoot]); return (exit, JsonNode.Parse(output.ToString())!); }
        finally { Console.SetOut(original); }
    }
    private string Write(string name, string text) { string path = Path.Combine(_root, name); File.WriteAllText(path, text); return path; }
    [Fact]
    public void PatchIsDryByDefaultAndAppliesOnlyWithCurrentHash()
    {
        string path = Path.Combine(ContentRoot, "item.chainmail.json"); byte[] original = File.ReadAllBytes(path);
        string patch = Write("patch.json", "{\"armor\":3}");
        var preview = Run("content", "patch", "item.chainmail", "--patch", patch);
        Assert.Equal(0, preview.Exit); Assert.False(preview.Json["applied"]!.GetValue<bool>()); Assert.Equal(original, File.ReadAllBytes(path));
        Assert.NotEqual(preview.Json["gameplayHashBefore"]!.ToString(), preview.Json["gameplayHashAfter"]!.ToString());
        Assert.Equal(1, Run("content", "patch", "item.chainmail", "--patch", patch, "--apply").Exit);
        string hash = preview.Json["beforeSha256"]!.ToString();
        Assert.Equal(0, Run("content", "patch", "item.chainmail", "--patch", patch, "--apply", "--expect", hash).Exit);
        byte[] updated = File.ReadAllBytes(path); Assert.NotEqual(original, updated);
        Assert.Equal(1, Run("content", "patch", "item.chainmail", "--patch", patch, "--apply", "--expect", hash).Exit);
        Assert.Equal(updated, File.ReadAllBytes(path));
    }
    [Theory]
    [InlineData("{\"armor\":-100}")]
    [InlineData("{\"affixPool\":[\"affix.missing\"]}")]
    [InlineData("{\"typo\":3}")]
    [InlineData("{\"id\":\"item.renamed\"}")]
    [InlineData("{\"kind\":\"character\"}")]
    public void InvalidPatchesPreserveSourceBytes(string change)
    {
        string path = Path.Combine(ContentRoot, "item.chainmail.json"); byte[] original = File.ReadAllBytes(path);
        string patch = Write("patch.json", change);
        Assert.Equal(1, Run("content", "patch", "item.chainmail", "--patch", patch, "--apply", "--expect", DiagnosticJson.Hash(original)).Exit);
        Assert.Equal(original, File.ReadAllBytes(path)); Assert.Empty(Directory.GetFiles(ContentRoot, "*.tmp"));
    }
    [Fact]
    public void MergePatchRecursesRemovesNullPropertiesAndReplacesArrays()
    {
        string patch = Write("patch.json", "{\"yield\":{\"item.apple\":4,\"item.food\":null},\"biomes\":[\"Forest\"]}");
        string path = Path.Combine(ContentRoot, "resource.apple-tree.json");
        var result = Run("content", "patch", "resource.apple-tree", "--patch", patch, "--apply", "--expect", DiagnosticJson.Hash(File.ReadAllBytes(path)));
        Assert.Equal(0, result.Exit); var updated = JsonNode.Parse(File.ReadAllText(path))!;
        Assert.Equal(4, updated["yield"]!["item.apple"]!.GetValue<int>()); Assert.Null(updated["yield"]!["item.food"]);
        Assert.Single(updated["biomes"]!.AsArray()); Assert.Equal("Forest", updated["biomes"]![0]!.ToString());
    }
    [Fact]
    public void DiffAndImpactExposeGameplayAndTransitiveDependencies()
    {
        Assert.Contains("class.warden", Run("content", "impact", "ability.longsword").Json.ToJsonString());
        Assert.Contains("character.traveller", Run("content", "impact", "ability.longsword").Json.ToJsonString());
        Assert.Equal(1, Run("content", "impact", "missing").Exit);
        string patch = Write("patch.json", "{\"name\":\"Reworded stories\"}");
        string path = Path.Combine(ContentRoot, "dialogue.stories.json");
        Assert.Equal(0, Run("content", "patch", "dialogue.stories", "--patch", patch, "--apply", "--expect", DiagnosticJson.Hash(File.ReadAllBytes(path))).Exit);
        var diff = Run("content", "diff", "--against", Path.Combine(ContentTests.Root, "content"));
        Assert.Equal(0, diff.Exit); Assert.False(diff.Json["gameplayChanged"]!.GetValue<bool>());
        Assert.Contains("dialogue.stories", diff.Json["changes"]!.ToJsonString());
    }
    [Theory]
    [InlineData("world")]
    [InlineData("gathering")]
    [InlineData("camp")]
    [InlineData("cooperation")]
    [InlineData("discovery")]
    public void AuthoredScenariosReplayDeterministicallyWithExactContinuation(string name)
    {
        var scenario = ScenarioRunner.Read(Path.Combine(ContentTests.Root, "devtools/scenarios", name + ".json"));
        var a = ScenarioRunner.Run(ContentCatalog.Default, scenario); var b = ScenarioRunner.Run(ContentCatalog.Default, scenario);
        Assert.True(a.Passed, JsonSerializer.Serialize(a, ContentCatalog.Json)); Assert.True(b.Passed);
        Assert.Equal(a.FinalHash, b.FinalHash); Assert.Equal(JsonSerializer.Serialize(a.Trace), JsonSerializer.Serialize(b.Trace));
        Assert.Contains(a.Trace, t => t.Op == "save-load" && t.BeforeHash == t.AfterHash);
    }
    [Fact]
    public void ScenarioFailureStopsAtExactStepAndRetainsTrace()
    {
        string file = Write("bad.json", """{"schemaVersion":1,"name":"failure","steps":[{"op":"assert","path":"/tick","expected":1},{"op":"advance","ticks":20}]}""");
        string output = Path.Combine(_root, "trace.json"); var result = Run("scenario", "run", file, "--output", output);
        Assert.Equal(1, result.Exit); Assert.False(result.Json["passed"]!.GetValue<bool>());
        var report = JsonNode.Parse(File.ReadAllText(output))!; Assert.Equal(0, report["failedStep"]!.GetValue<int>());
        Assert.Single(report["trace"]!.AsArray()); Assert.Equal(0, report["state"]!["tick"]!.GetValue<int>());
        Assert.Contains("/tick", report["trace"]![0]!["message"]!.ToString());
    }
    [Theory]
    [InlineData("{\"op\":\"assert\",\"path\":\"/missing\",\"expected\":null}")]
    [InlineData("{\"op\":\"assert\",\"path\":null,\"expected\":0}")]
    [InlineData("{\"op\":\"advance\",\"ticks\":20001}")]
    [InlineData("{\"op\":\"teleport\"}")]
    [InlineData("{\"op\":\"assert\",\"path\":\"/tick\",\"expected\":0,\"typo\":true}")]
    public void InvalidScenarioStepsCannotSilentlyPass(string step)
    {
        string file = Write("bad.json", "{\"schemaVersion\":1,\"name\":\"bad\",\"steps\":[" + step + ", {\"op\":\"assert\",\"path\":\"/tick\",\"expected\":0}]}");
        Assert.Equal(1, Run("scenario", "run", file).Exit);
    }
    [Fact]
    public void ScenarioSchemaIsCurrentAndEmptySuitesFail()
    {
        string path = Path.Combine(_root, "scenario.schema.json");
        var original = Console.Out; using var output = new StringWriter(); Console.SetOut(output);
        try { Assert.Equal(0, DeveloperEntry.Run(["scenario", "schema", "--output", path, "--content", ContentRoot])); }
        finally { Console.SetOut(original); }
        Assert.Equal(File.ReadAllText(Path.Combine(ContentTests.Root, "devtools/scenario.schema.json")), File.ReadAllText(path));
        string empty = Path.Combine(_root, "empty"); Directory.CreateDirectory(empty);
        Assert.Equal(1, Run("scenario", "suite", empty).Exit);
    }
    [Fact]
    public void CustomCatalogContinuationDoesNotBypassDefaultSaveCompatibility()
    {
        string path = Path.Combine(ContentRoot, "item.chainmail.json"); var item = JsonNode.Parse(File.ReadAllText(path))!;
        item["armor"] = 3; File.WriteAllText(path, item.ToJsonString());
        var custom = ContentCatalog.LoadDirectory(ContentRoot); Assert.Empty(custom.Issues); Assert.NotEqual(ContentCatalog.Default.Hash, custom.Hash);
        string slot = Path.Combine(_root, "world.json"); var world = WorldSession.Create(42, custom); world.Save(slot);
        Assert.Equal(ScenarioRunner.StateHash(world), ScenarioRunner.StateHash(WorldSession.Load(slot, custom)));
        Assert.Throws<InvalidDataException>(() => WorldSession.Load(slot));
        var bytes = Directory.GetFiles(_root, "*", SearchOption.AllDirectories).ToDictionary(f => f, File.ReadAllBytes);
        var report = DiagnosticJson.Node(ScenarioRunner.InspectSave(slot, ContentCatalog.Default));
        Assert.False(report["compatible"]!.GetValue<bool>()); Assert.Contains("World content differs", report["reason"]!.ToString());
        Assert.True(DiagnosticJson.Node(ScenarioRunner.InspectSave(slot, custom))["compatible"]!.GetValue<bool>());
        foreach (var (file, before) in bytes) Assert.Equal(before, File.ReadAllBytes(file));
    }
    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{ broken")]
    public void MalformedSaveDiagnosticsAreReadOnlyAndStructured(string data)
    {
        string path = Write("save.json", data); var result = Run("diagnose", "save", path);
        Assert.Equal(1, result.Exit); Assert.False(result.Json["compatible"]!.GetValue<bool>());
        Assert.True(result.Json["readOnly"]!.GetValue<bool>()); Assert.Equal(data, File.ReadAllText(path));
    }
    [Fact]
    public void JsonDifferencesAreBoundedAndUseEscapedPointers()
    {
        var before = JsonNode.Parse("{\"a/b~c\":1}")!; var after = JsonNode.Parse("{\"a/b~c\":2}")!;
        var changes = DiagnosticJson.Diff(before, after); Assert.Equal("/a~1b~0c", Assert.Single(changes).Path);
        Assert.Equal(2, DiagnosticJson.At(after, changes[0].Path)!.GetValue<int>());
        Assert.Throws<ArgumentException>(() => DiagnosticJson.At(after, "/missing"));
        Assert.Equal("added", Assert.Single(DiagnosticJson.Diff(new JsonObject(), JsonNode.Parse("{\"nullable\":null}"))).Kind);
        Assert.Equal("removed", Assert.Single(DiagnosticJson.Diff(JsonNode.Parse("{\"nullable\":null}"), new JsonObject())).Kind);
        Assert.Single(DiagnosticJson.Diff(before, JsonNode.Parse("{\"one\":1,\"two\":2}"), limit: 1));
        var huge = new JsonArray(Enumerable.Range(0, 2000).Select(i => JsonValue.Create(i)).ToArray());
        Assert.True(Assert.Single(DiagnosticJson.Diff(null, huge)).After!["truncated"]!.GetValue<bool>());
        Assert.Equal(DiagnosticJson.Hash(JsonNode.Parse("{\"a\":1,\"b\":2}")), DiagnosticJson.Hash(JsonNode.Parse("{\"b\":2,\"a\":1}")));
    }
}
