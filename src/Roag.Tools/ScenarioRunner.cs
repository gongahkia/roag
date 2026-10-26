using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Roag.Core.Content;
using Roag.Core.Expeditions;
using Roag.Core.World;

public sealed class GameScenario
{
    [JsonRequired] public int SchemaVersion { get; set; }
    [JsonRequired] public string Name { get; set; } = "";
    public int Seed { get; set; } = 42;
    public string Fixture { get; set; } = "world";
    public string Definition { get; set; } = "";
    [JsonRequired] public ScenarioStep[] Steps { get; set; } = [];
}
public sealed class ScenarioStep
{
    [JsonRequired] public string Op { get; set; } = "";
    public WorldCommandKind? Kind { get; set; }
    public string Anchor { get; set; } = "fixture";
    public int Dx { get; set; }
    public int Dy { get; set; }
    public int PartyIndex { get; set; } = -1;
    public long Actor { get; set; }
    public string Item { get; set; } = "food";
    public int Amount { get; set; } = 1;
    public Material Material { get; set; } = Material.Wall;
    public Cell[] Cells { get; set; } = [];
    public int Ticks { get; set; } = 1;
    public int Skill { get; set; }
    public int Target { get; set; } = -1;
    public int Partner { get; set; } = -1;
    public bool Success { get; set; } = true;
    public string Path { get; set; } = "";
    public JsonElement Expected { get; set; }
    public double? Minimum { get; set; }
    public double? Maximum { get; set; }
}
public sealed record ScenarioTrace(int Index, string Op, bool Passed, string Message, string BeforeHash, string AfterHash, object Changes);
public sealed record ScenarioReport(bool Passed, string Name, int Seed, string Fixture, string ContentHash, int? FailedStep, string FinalHash, object State, List<ScenarioTrace> Trace);

public static class ScenarioRunner
{
    public static string StateHash(WorldSession world) => DiagnosticJson.Hash(DiagnosticJson.Node(world.State));
    public static JsonNode Snapshot(WorldSession world, WorldPosition anchor)
    {
        var resource = world.ResourceAt(anchor);
        return DiagnosticJson.Node(new { world.State.Tick, world.State.Position, world.IsInCombat, world.Paused,
            inventory = world.State.Inventory, work = world.State.Work.Take(24), workCount = world.State.Work.Count, workTruncated = world.State.Work.Count > 24,
            partyCount = world.State.Party.Count, party = world.Party.Select(a => new { a.Id, a.Name, a.Position, a.Health, a.Mana, a.Body, a.Preparations, a.Equipment }),
            battle = world.State.Battle, world.State.Endgame,
            anchor = new { position = anchor, terrain = world.Chunks.At(anchor).ToString(), resource = resource == null ? null : new { resource.Definition.Id, resource.Available, resource.RecoverIn },
                discovery = world.State.Discoveries.Where(d => d.Position == anchor).Select(d => new { d.Id, d.Outcome, d.Engaged }).FirstOrDefault() },
            random = new { world.State.CombatRandom }, events = world.State.CombatEvents.TakeLast(12).ToArray() });
    }
    public static ScenarioReport Run(ContentCatalog content, GameScenario scenario)
    {
        if (content.Issues.Count > 0) throw new InvalidDataException("Scenario content is invalid.");
        if (scenario.SchemaVersion != 1 || string.IsNullOrWhiteSpace(scenario.Name) || scenario.Steps == null || scenario.Steps.Length is < 1 or > 256)
            throw new ArgumentException("A scenario needs schemaVersion 1, a name and 1–256 steps.");
        if (scenario.Steps.Any(s => s == null || s.Ticks is < 0 or > 6000 || Math.Abs((long)s.Dx) > 100 || Math.Abs((long)s.Dy) > 100) || scenario.Steps.Sum(s => (long)s.Ticks) > 20000)
            throw new ArgumentException("Bound scenarios to 6,000 ticks per step, 20,000 total and local offsets within 100 tiles.");
        if (!scenario.Steps.Any(s => s.Op is "assert" or "save-load")) throw new ArgumentException("A scenario must assert behavior or verify exact save continuation.");
        var world = WorldSession.Create(scenario.Seed, content); WorldPosition anchor = world.State.Position;
        switch (scenario.Fixture)
        {
            case "world": break;
            case "camp": anchor = CampPreview.Prepare(world); break;
            case "resource": anchor = ResourcePreview.Prepare(world, scenario.Definition); break;
            case "cooperation": CooperationPreview.Prepare(world); anchor = world.State.Position; break;
            case "situation": anchor = SituationPreview.Prepare(world, scenario.Definition).Position; break;
            case "wildlife": anchor = WildlifePreview.Prepare(world, scenario.Definition).Habitat!.Value; break;
            default: throw new ArgumentException("Unknown fixture. Use world, camp, resource, cooperation, situation or wildlife.");
        }
        var trace = new List<ScenarioTrace>(); int? failed = null;
        string directory = Path.Combine(Path.GetTempPath(), "vagabonds-scenario-" + Guid.NewGuid().ToString("N"));
        try
        {
            for (int index = 0; index < scenario.Steps.Length; index++)
            {
                var step = scenario.Steps[index]; var before = Snapshot(world, anchor); string hash = StateHash(world); string message = ""; bool pass = true;
                try
                {
                    if (step.Anchor is not ("fixture" or "party")) throw new ArgumentException("Anchor must be fixture or party.");
                    var target = (step.Anchor == "party" ? world.State.Position : anchor).Offset(step.Dx, step.Dy);
                    long actor = step.PartyIndex >= 0 ? world.State.Party.ElementAt(step.PartyIndex) : step.Actor;
                    switch (step.Op)
                    {
                        case "command":
                            if (step.Kind == null) throw new ArgumentException("Command needs kind.");
                            var result = world.Submit(new(step.Kind.Value, target, actor, step.Material, step.Item, step.Amount));
                            pass = result.Success == step.Success; message = result.Message; break;
                        case "designate":
                            if (step.Kind == null || step.Cells == null || step.Cells.Length is < 1 or > 256) throw new ArgumentException("Designation needs kind and 1–256 relative cells.");
                            var batch = world.DesignateWork(step.Kind.Value, step.Cells.Select(c => target.Offset(c.X, c.Y)), step.Material);
                            pass = (batch.Queued > 0) == step.Success; message = batch.Message; break;
                        case "advance": world.Paused = false; world.AdvanceTicks(step.Ticks); message = "Advanced the ordinary simulation; combat and other freezes still apply."; break;
                        case "combat":
                            var acted = world.Act(step.Skill, step.Target, step.Partner); pass = acted.Success == step.Success; message = acted.Message; break;
                        case "enemy": var enemy = world.AdvanceEnemy(); pass = enemy.Success == step.Success; message = enemy.Message; break;
                        case "assert":
                            var actual = DiagnosticJson.At(Snapshot(world, anchor), step.Path);
                            if (step.Expected.ValueKind != JsonValueKind.Undefined) pass = JsonNode.DeepEquals(actual, JsonNode.Parse(step.Expected.GetRawText()));
                            else
                            {
                                if (step.Minimum == null && step.Maximum == null) throw new ArgumentException("Assertion needs expected, minimum or maximum.");
                                double number = actual?.GetValue<double>() ?? throw new ArgumentException("Expected a numeric value at " + step.Path);
                                pass = (step.Minimum == null || number >= step.Minimum) && (step.Maximum == null || number <= step.Maximum);
                            }
                            message = step.Path + " = " + (actual?.ToJsonString() ?? "null"); break;
                        case "save-load":
                            Directory.CreateDirectory(directory); string path = Path.Combine(directory, "world.json"); bool paused = world.Paused;
                            world.Save(path); var restored = WorldSession.Load(path, content); restored.SavePath = null; restored.Paused = paused;
                            pass = StateHash(world) == StateHash(restored); message = pass ? "Exact saved state restored in an isolated temporary slot." : "Saved state differs after loading."; world = restored; break;
                        default: throw new ArgumentException("Unknown scenario operation: " + step.Op);
                    }
                }
                catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or JsonException or IndexOutOfRangeException)
                { pass = false; message = e.GetType().Name + ": " + e.Message; }
                if (!step.Success && step.Op is "command" or "designate" or "combat" or "enemy" && hash != StateHash(world))
                { pass = false; message += " Rejected action mutated state."; }
                if (message.Length > 1500) message = message[..1500] + "...";
                var after = Snapshot(world, anchor); trace.Add(new(index, step.Op, pass, message, hash, StateHash(world), DiagnosticJson.Diff(before, after)));
                if (!pass) { failed = index; break; }
            }
            return new(failed == null, scenario.Name, scenario.Seed, scenario.Fixture, content.Hash, failed, StateHash(world), Snapshot(world, anchor), trace);
        }
        finally { world.SavePath = null; if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
    public static GameScenario Read(string path, int? seed = null)
    {
        var scenario = JsonSerializer.Deserialize<GameScenario>(File.ReadAllText(path), ContentCatalog.Json) ?? throw new ArgumentException("Empty scenario.");
        if (seed != null) scenario.Seed = seed.Value; return scenario;
    }
    public static object InspectSave(string path, ContentCatalog content)
    {
        byte[] bytes = File.ReadAllBytes(path); JsonObject? raw = null; WorldSession world;
        try
        {
            raw = JsonNode.Parse(bytes) as JsonObject ?? throw new InvalidDataException("Save must be an object.");
            world = WorldSession.Load(path, content); world.SavePath = null;
        }
        catch (Exception e) when (e is InvalidDataException or JsonException or IOException)
        {
            return new { compatible = false, readOnly = true, fileSha256 = DiagnosticJson.Hash(bytes), format = raw?["format"]?.ToString(),
                savedContentHash = raw?["contentHash"]?.ToString(), currentContentHash = content.Hash, reason = e.Message,
                next = "Keep this save. To play this build, use ./roag.sh --new --save with a different, unused save filename." };
        }
        return new { compatible = true, readOnly = true, fileSha256 = DiagnosticJson.Hash(File.ReadAllBytes(path)),
            format = raw["format"]?.ToString(), version = raw["version"]?.ToString(), contentHash = content.Hash,
            world.Seed, stateHash = StateHash(world), state = Snapshot(world, world.State.Position) };
    }
}
