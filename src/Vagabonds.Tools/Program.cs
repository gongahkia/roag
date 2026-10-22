using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;

return DeveloperEntry.Run(args);

public static class DeveloperEntry
{
    public static int Run(string[] args)
    {
        try
        {
            string Value(string flag, string fallback) { int index = Array.IndexOf(args, flag); return index < 0 ? fallback : index + 1 < args.Length ? args[index + 1] : throw new ArgumentException("Missing " + flag); }
            var valueFlags = new[] { "--content", "--seed", "--output", "--id", "--samples", "--patch", "--expect", "--against" };
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal)) continue;
                if (valueFlags.Contains(args[i]))
                {
                    if (++i >= args.Length || args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException("Missing option value.");
                }
                else if (args[i] is not ("--replace" or "--apply")) throw new ArgumentException("Unknown option: " + args[i]);
            }
            string root = Path.GetFullPath(Value("--content", "content"));
            int seed = int.Parse(Value("--seed", "42"), System.Globalization.CultureInfo.InvariantCulture);
            var catalog = ContentCatalog.LoadDirectory(root);
            string command = args.ElementAtOrDefault(0) ?? "help";
            if (command == "content")
            {
                string operation = args.ElementAtOrDefault(1) ?? "list", id = args.ElementAtOrDefault(2) ?? "";
                switch (operation)
                {
                    case "list":
                        Print(catalog.Entries.Values.Where(d => id.Length == 0 || ContentCatalog.Kind(d) == id).OrderBy(d => d.Id)
                            .Select(d => new { d.Id, kind = ContentCatalog.Kind(d), d.Name, file = catalog.Source(d.Id) })); break;
                    case "inspect":
                        if (!catalog.Entries.TryGetValue(id, out var definition)) throw new ArgumentException("Unknown content ID: " + id);
                        Print(new { file = catalog.Source(id), sha256 = DiagnosticJson.Hash(File.ReadAllBytes(Path.Combine(root, catalog.Source(id)))), definition = JsonSerializer.SerializeToNode<ContentDefinition>(definition, ContentCatalog.Json),
                            incoming = catalog.References.Where(r => r.Target == id), outgoing = catalog.References.Where(r => r.Source == id) }); break;
                    case "patch":
                        var patchReport = ContentEditing.Patch(catalog, root, id, Value("--patch", ""), args.Contains("--apply"), Value("--expect", "") is { Length: > 0 } expected ? expected : null);
                        Print(patchReport); return patchReport.Valid ? 0 : 1;
                    case "diff": Print(ContentEditing.Compare(ContentCatalog.LoadDirectory(Value("--against", "")), catalog)); break;
                    case "impact": Print(new { id, affected = ContentEditing.Affected(catalog, [id]) }); break;
                    case "references": Print(catalog.References.Where(r => r.Source == id || r.Target == id)); break;
                    case "validate": Print(new { valid = catalog.Issues.Count == 0, count = catalog.Entries.Count, catalog.Hash, issues = catalog.Issues }); return catalog.Issues.Count == 0 ? 0 : 1;
                    case "schema":
                        var options = new JsonSerializerOptions(ContentCatalog.Json); options.MakeReadOnly(populateMissingResolver: true);
                        var schema = JsonSchemaExporter.GetJsonSchemaAsNode(options, typeof(ContentDefinition));
                        string? schemaOutput = Array.IndexOf(args, "--output") >= 0 ? Value("--output", "") : null;
                        if (schemaOutput != null) WriteNew(schemaOutput, schema.ToJsonString(ContentCatalog.Json) + "\n", args.Contains("--replace")); else Print(schema);
                        break;
                    case "scaffold":
                        var scaffold = Scaffold(id, Value("--id", id + ".new"), catalog);
                        string output = Value("--output", Path.Combine(root, id, scaffold.Id[(scaffold.Id.IndexOf('.') + 1)..] + ".json"));
                        WriteNew(output, JsonSerializer.Serialize<ContentDefinition>(scaffold, ContentCatalog.Json) + "\n"); Print(new { created = Path.GetFullPath(output), scaffold.Id }); break;
                    case "remove":
                        if (!catalog.Entries.ContainsKey(id)) throw new ArgumentException("Unknown content ID: " + id);
                        var incoming = catalog.References.Where(r => r.Target == id && r.Source != id).ToArray();
                        if (incoming.Length > 0) { Print(new { removed = false, reason = "References must be updated first.", incoming }); return 1; }
                        string file = Path.GetFullPath(Path.Combine(root, catalog.Source(id)));
                        if (!file.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new ArgumentException("Content path outside its root.");
                        bool apply = args.Contains("--apply"); if (apply) File.Delete(file);
                        Print(new { id, file, removed = apply, dryRun = !apply }); break;
                    default: throw new ArgumentException("Unknown content command: " + operation);
                }
                return 0;
            }
            if (command == "preview")
            {
                RequireValid(catalog); string kind = args.ElementAtOrDefault(1) ?? "", id = args.ElementAtOrDefault(2) ?? "";
                if (kind == "dialogue") Print(DialoguePreview.Run(catalog, id, seed));
                else if (kind == "discovery")
                { if (catalog.Get<DiscoveryDefinition>(id).Situation != null) Print(SituationPreview.Run(catalog, id, seed)); else Print(ExplorationPreview.Discovery(catalog, id, seed)); }
                else if (kind is "item" or "class" or "names" or "trait" or "variant" or "game") Print(ContentPreview.Run(catalog, kind, id, seed));
                else if (kind == "affix") Print(EquipmentPreview.Run(catalog, id, seed));
                else if (kind == "camp") Print(CampPreview.Run(catalog, id, seed));
                else if (kind == "resource") Print(ResourcePreview.Run(catalog, id, seed));
                else if (kind == "party-work") Print(PartyWorkPreview.Run(catalog, seed));
                else if (kind == "recipe") Print(SimulationPreview.Recipe(catalog, id, seed));
                else if (kind == "simulation") Print(SimulationPreview.Simulation(SimulationPreview.WithGame(catalog, g => g["simulation"] = catalog.Get<SimulationDefinition>(id).Id), seed));
                else if (kind == "incident") Print(SimulationPreview.Incident(catalog, id, seed));
                else if (kind == "encounter") Print(EncounterPreview.Run(catalog, id, seed));
                else if (kind == "generation")
                {
                    catalog = SimulationPreview.WithGame(catalog, g => g["generation"] = catalog.Get<GenerationDefinition>(id).Id);
                    var plan = WorldPlan.Generate(seed);
                    var generator = new ChunkGenerator(plan, catalog);
                    Print(plan.Sites.Take(4).Select(s => new { s.Purpose, floors = Enumerable.Range(0, 4).Select(f => generator.GenerateFootprint(false, s.Id, f).Trace) }));
                }
                else if (kind == "character")
                {
                    if (catalog.Get<CharacterDefinition>(id).Ecology != null) Print(WildlifePreview.Run(catalog, id, seed));
                    else
                    {
                        var actor = ActorFactory.Create(catalog, id, seed, 100001, new(0, 0), 0);
                        Print(new { seed, actor, interactions = catalog.Get<CharacterDefinition>(id).Interactions.Select(catalog.Get<InteractionDefinition>) });
                    }
                }
                else if (kind == "ability") Print(AbilityPreview.Run(catalog, id, seed));
                else if (kind == "interaction")
                {
                    var game = WorldSession.Create(seed, catalog); var actor = game.State.Actors.First(a => game.Interactions(a.Id).Any(i => i.Id == id));
                    actor.Position = game.State.Position.Offset(1, 0); game.Party.First().Health = Math.Max(1, game.Party.First().Health / 2);
                    long tick = game.State.Tick; uint rng = game.State.CombatRandom; var result = game.PerformInteraction(actor.Id, id);
                    Print(new { seed, actor.Name, result, tickBefore = tick, tickAfter = game.State.Tick, randomUnchanged = rng == game.State.CombatRandom, game.State.Inventory, party = game.Party });
                }
                else throw new ArgumentException("Choose a registered content preview, encounter or party-work. Use review-tooling to list previews.");
                return 0;
            }
            if (command == "review-tooling")
            {
                Print(new { catalogValid = catalog.Issues.Count == 0, count = catalog.Entries.Count,
                    kinds = ContentCatalog.Kinds.Select(k => new { kind = k, definitions = catalog.Entries.Values.Count(d => ContentCatalog.Kind(d) == k) }),
                    previews = new[] { "character", "ability", "interaction", "generation", "encounter", "recipe", "simulation", "incident", "item", "class", "names", "trait", "variant", "game", "discovery", "dialogue", "resource", "camp", "affix", "party-work" },
                    registeredEffects = ContentCatalog.Effects, registeredInteractions = ContentCatalog.Actions,
                    ecology = new { behaviors = new[] { "flee", "defend", "roost" }, preview = "preview character ID", audit = "audit-wildlife --seed N --samples N" },
                    cooperation = new { abilities = catalog.Game.CooperativeAbilities, preview = "preview ability ID", nativeScene = "world-cooperation", rule = "Both participants spend mana and one action; helper must still have a turn this round." },
                    documentation = new { authoring = File.Exists("docs/CONTENT_AUTHORING.md"), integration = File.Exists("docs/INTEGRATION.md") },
                    note = "Review real authoring friction and missing previews at each milestone; counts do not measure content quality." }); return catalog.Issues.Count == 0 ? 0 : 1;
            }
            if (command == "audit-world")
            {
                RequireValid(catalog);
                var audit = WorldVarietyAudit.Run(seed, int.Parse(Value("--samples", "8")), catalog);
                string output = Value("--output", "artifacts/world-audit");
                WriteNew(output + ".json", JsonSerializer.Serialize(audit.Report, ContentCatalog.Json) + "\n", args.Contains("--replace"));
                WriteNew(output + ".svg", audit.Svg, args.Contains("--replace"));
                Print(new { reportPath = output + ".json", comparison = output + ".svg", audit.Report }); return audit.Report.Connected ? 0 : 1;
            }
            if (command == "audit-exploration")
            {
                RequireValid(catalog); var audit = ExplorationPreview.Audit(catalog, seed, int.Parse(Value("--samples", "8")));
                string output = Value("--output", "artifacts/exploration-audit");
                WriteNew(output + ".json", JsonSerializer.Serialize(audit.Report, ContentCatalog.Json) + "\n", args.Contains("--replace"));
                WriteNew(output + ".svg", audit.Svg, args.Contains("--replace"));
                Print(audit.Report); return JsonSerializer.SerializeToNode(audit.Report)!["passed"]!.GetValue<bool>() ? 0 : 1;
            }
            if (command == "audit-situations")
            {
                RequireValid(catalog); var audit = SituationPreview.Audit(catalog, seed, int.Parse(Value("--samples", "4")));
                string output = Value("--output", "artifacts/situation-audit.json");
                WriteNew(output, JsonSerializer.Serialize(audit, ContentCatalog.Json) + "\n", args.Contains("--replace"));
                Print(new { audit.Passed, audit.Seeds, methods = audit.Observations.Length, audit.LayoutsChecked, audit.BrokenAlternatives, output }); return audit.Passed ? 0 : 1;
            }
            if (command == "audit-wildlife")
            {
                RequireValid(catalog); var audit = WildlifePreview.Audit(catalog, seed, int.Parse(Value("--samples", "4")));
                string output = Value("--output", "artifacts/wildlife-audit.json");
                WriteNew(output, JsonSerializer.Serialize(audit, ContentCatalog.Json) + "\n", args.Contains("--replace"));
                Print(new { audit.Passed, audit.Seeds, audit.Encounters, audit.Escapes, audit.DistinctHabitats, output }); return audit.Passed ? 0 : 1;
            }
            if (command != "help") throw new ArgumentException("Unknown developer command: " + command);
            Print(new { commands = new[] { "content list [kind]", "content inspect ID", "content references ID", "content impact ID", "content patch ID --patch FILE [--apply --expect SHA256]", "content diff --against DIRECTORY", "content validate", "content schema [--output PATH]", "content scaffold KIND --id ID [--output PATH]", "content remove ID [--apply]", "preview KIND ID [--seed N] (boss uses encounter)", "audit-world --seed N --samples N [--output PREFIX]", "audit-wildlife --seed N --samples N [--output FILE]", "audit-situations --seed N --samples N [--output FILE]", "audit-exploration --seed N --samples N [--output PREFIX]", "review-tooling" },
                options = new[] { "--content DIRECTORY (authoring preview/validation only)", "--seed N" } }); return 0;
        }
        catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException or InvalidDataException or JsonException or InvalidOperationException or FormatException or OverflowException)
        { Print(new { error = e.Message }); return 1; }
    }
    private static void RequireValid(ContentCatalog catalog) { if (catalog.Issues.Count > 0) throw new InvalidDataException(string.Join("; ", catalog.Issues.Select(i => $"{i.File}:{i.Field}: {i.Message}"))); }
    private static void Print(object value) => Console.WriteLine(JsonSerializer.Serialize(value, ContentCatalog.Json));
    private static void WriteNew(string output, string text, bool replace = false)
    {
        string target = Path.GetFullPath(output); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string temporary = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, text); File.Move(temporary, target, overwrite: replace);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static ContentDefinition Scaffold(string kind, string id, ContentCatalog c)
    {
        if (!ContentCatalog.ValidId(id) || !id.StartsWith(kind + ".", StringComparison.Ordinal)) throw new ArgumentException("Use a stable kind.name identifier; paths are not content IDs.");
        ContentDefinition template = kind switch
        {
            "ability" => new AbilityDefinition { Id = id, Name = "New ability", Sprite = "sword", Effect = "damage", Power = 6 },
            "dialogue" => new DialogueDefinition { Id = id, Name = "New dialogue", Interaction = "interaction.stories", Variants = [new() { Player = "Tell me your story.", Replies = ["I hope to {motivation}."] }] },
            "interaction" => new InteractionDefinition { Id = id, Name = "New conversation", Action = "converse" },
            "item" => new ItemDefinition { Id = id, Name = "New item", Sprite = "chest", Stack = id },
            "trait" => new TraitDefinition { Id = id, Name = "New trait" },
            "names" => new NamesDefinition { Id = id, Name = "New profiles", First = ["Aster"], Last = ["Vale"], Backgrounds = ["traveller"], Motivations = ["explore"], Traits = [c.All<TraitDefinition>().First().Id] },
            "character" => new CharacterDefinition { Id = id, Name = "New traveller", Names = c.All<NamesDefinition>().First().Id, Classes = c.Game.Classes, Interactions = ["interaction.stories"] },
            "boss" => c.Get<BossDefinition>(c.Game.Bosses[0]),
            "class" => c.All<ClassDefinition>().First(),
            "recipe" => c.All<RecipeDefinition>().First(),
            "resource" => c.All<ResourceDefinition>().First(),
            "camp" => c.All<CampDefinition>().First(),
            "affix" => c.All<AffixDefinition>().First(),
            "variant" => new VariantDefinition { Id = id, Name = "New variation" },
            "generation" => c.Get<GenerationDefinition>(c.Game.Generation),
            "simulation" => c.Get<SimulationDefinition>(c.Game.Simulation),
            "incident" => new IncidentDefinition { Id = id, Name = "A new memory", Condition = "trust", Effect = "memory", Mood = 3 },
            "discovery" => new DiscoveryDefinition { Id = id, Name = "A forgotten marker", Sprite = "sign", Action = "clue", Text = "{witness} of {town} left a route to {site}, to the {direction}." },
            _ => throw new ArgumentException("Scaffold a supported content kind; game.vagabonds is the existing runtime entry point.")
        };
        // Clone templates before changing IDs, preserving the loaded catalog's identity and references.
        var copy = JsonSerializer.Deserialize<ContentDefinition>(JsonSerializer.Serialize<ContentDefinition>(template, ContentCatalog.Json), ContentCatalog.Json)!;
        copy.Id = id;
        return copy;
    }
}
