using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Roag.Core.Content;

public static class DiagnosticJson
{
    public sealed record Change(string Path, JsonNode? Before, JsonNode? After, string Kind = "changed");
    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    public static string Hash(JsonNode? node) => Hash(Encoding.UTF8.GetBytes(Canonical(node)));
    private static string Canonical(JsonNode? node) => node switch
    {
        JsonObject o => "{" + string.Join(",", o.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => JsonSerializer.Serialize(p.Key) + ":" + Canonical(p.Value))) + "}",
        JsonArray a => "[" + string.Join(",", a.Select(Canonical)) + "]",
        _ => node?.ToJsonString() ?? "null"
    };
    public static List<Change> Diff(JsonNode? before, JsonNode? after, string path = "", int limit = 100)
    {
        var changes = new List<Change>(); Walk(before, after, path); return changes;
        void Walk(JsonNode? a, JsonNode? b, string p)
        {
            if (changes.Count >= limit || JsonNode.DeepEquals(a, b)) return;
            if (a is JsonObject left && b is JsonObject right)
                foreach (string key in left.Select(v => v.Key).Union(right.Select(v => v.Key)).Order(StringComparer.Ordinal))
                {
                    if (changes.Count >= limit) break;
                    string child = p + "/" + key.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);
                    if (!left.ContainsKey(key)) changes.Add(new(child, null, Summary(right[key]), "added"));
                    else if (!right.ContainsKey(key)) changes.Add(new(child, Summary(left[key]), null, "removed"));
                    else Walk(left[key], right[key], child);
                }
            else if (a is JsonArray aa && b is JsonArray bb && aa.Count == bb.Count)
                for (int i = 0; i < aa.Count; i++) Walk(aa[i], bb[i], p + "/" + i);
            else changes.Add(new(p, Summary(a), Summary(b)));
        }
    }
    private static JsonNode? Summary(JsonNode? value) => value == null || value.ToJsonString().Length <= 1500 ? value?.DeepClone()
        : Node(new { truncated = true, type = value.GetValueKind().ToString(), hash = Hash(value), entries = value is JsonArray a ? a.Count : value is JsonObject o ? o.Count : 1 });
    public static JsonNode? At(JsonNode? node, string pointer)
    {
        if (pointer == null) throw new ArgumentException("A JSON pointer cannot be null; use an empty string for the root.");
        if (pointer.Length == 0) return node;
        if (!pointer.StartsWith('/')) throw new ArgumentException("Use a JSON pointer beginning with /.");
        foreach (string raw in pointer[1..].Split('/'))
        {
            string key = raw.Replace("~1", "/", StringComparison.Ordinal).Replace("~0", "~", StringComparison.Ordinal);
            node = node switch
            {
                JsonObject obj when obj.ContainsKey(key) => obj[key],
                JsonArray array when int.TryParse(key, out int index) && index >= 0 && index < array.Count => array[index],
                _ => throw new ArgumentException("Missing JSON pointer: " + pointer)
            };
        }
        return node;
    }
    public static JsonNode Node(object value) => JsonSerializer.SerializeToNode(value, ContentCatalog.Json)!;
}
