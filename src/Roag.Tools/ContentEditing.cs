using System.Text.Json.Nodes;
using Roag.Core.Content;

public static class ContentEditing
{
    public sealed record PatchReport(bool Valid, bool Applied, string Id, string File, string BeforeSha256, string AfterSha256,
        string GameplayHashBefore, string GameplayHashAfter, object Changes, object Issues, string[] Affected);
    public static string[] Affected(ContentCatalog catalog, IEnumerable<string> changed)
    {
        var all = changed.ToHashSet(StringComparer.Ordinal); var queue = new Queue<string>(all);
        while (queue.TryDequeue(out var id))
            foreach (var r in catalog.References.Where(r => r.Target == id)) if (all.Add(r.Source)) queue.Enqueue(r.Source);
        return all.Order(StringComparer.Ordinal).ToArray();
    }
    public static PatchReport Patch(ContentCatalog catalog, string root, string id, string patchPath, bool apply, string? expected)
    {
        if (!catalog.Entries.ContainsKey(id)) throw new ArgumentException("Unknown content ID: " + id);
        string source = catalog.Source(id), path = Path.GetFullPath(Path.Combine(root, source));
        byte[] original = File.ReadAllBytes(path); string hash = DiagnosticJson.Hash(original);
        if (apply && string.IsNullOrWhiteSpace(expected)) throw new ArgumentException("Apply requires --expect SHA256 from the dry run; this protects intervening edits.");
        if (expected != null && expected != hash) throw new InvalidDataException("Content changed since inspection. Current SHA256: " + hash);
        var before = JsonNode.Parse(original)!.AsObject(); var after = before.DeepClone().AsObject();
        var patch = JsonNode.Parse(File.ReadAllText(patchPath)) as JsonObject ?? throw new ArgumentException("Use an object merge patch. Arrays replace whole arrays; null removes a property.");
        Merge(after, patch);
        if (after["id"]?.ToString() != id || after["kind"]?.ToString() != before["kind"]?.ToString()) throw new ArgumentException("Patching cannot rename IDs or change content kinds. Update references deliberately.");
        string text = after.ToJsonString(ContentCatalog.Json) + "\n";
        var candidate = ContentCatalog.Load(Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
            .Where(p => Path.GetFileName(p) != "schema.json")
            .Select(p => (File: Path.GetRelativePath(root, p), Text: Path.GetFullPath(p) == path ? text : File.ReadAllText(p))));
        bool valid = candidate.Issues.Count == 0;
        if (apply && valid)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temporary, text);
                if (DiagnosticJson.Hash(File.ReadAllBytes(path)) != hash) throw new InvalidDataException("Content changed during validation; nothing was applied.");
                File.Move(temporary, path, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        return new(valid, apply && valid, id, source, hash, DiagnosticJson.Hash(System.Text.Encoding.UTF8.GetBytes(text)),
            catalog.Hash, candidate.Hash, DiagnosticJson.Diff(before, after), candidate.Issues, Affected(catalog, [id]));
    }
    private static void Merge(JsonObject target, JsonObject patch)
    {
        foreach (var (key, value) in patch)
        {
            if (value == null) target.Remove(key);
            else if (value is JsonObject obj)
            { if (target[key] is not JsonObject) target[key] = new JsonObject(); Merge(target[key]!.AsObject(), obj); }
            else target[key] = value.DeepClone();
        }
    }
    public static object Compare(ContentCatalog before, ContentCatalog after)
    {
        var changes = new List<object>(); var ids = new List<string>();
        foreach (string id in before.Entries.Keys.Union(after.Entries.Keys).Order(StringComparer.Ordinal))
        {
            JsonNode? Node(ContentCatalog c) => c.Entries.TryGetValue(id, out var d) ? System.Text.Json.JsonSerializer.SerializeToNode<ContentDefinition>(d, ContentCatalog.Json) : null;
            var a = Node(before); var b = Node(after); if (JsonNode.DeepEquals(a, b)) continue;
            ids.Add(id); changes.Add(new { id, kind = a == null ? "added" : b == null ? "removed" : "modified", fields = DiagnosticJson.Diff(a, b) });
        }
        return new { beforeHash = before.Hash, afterHash = after.Hash, gameplayChanged = before.Hash != after.Hash,
            changes, affected = Affected(before, ids).Union(Affected(after, ids)).Order(StringComparer.Ordinal), issues = after.Issues };
    }
}
