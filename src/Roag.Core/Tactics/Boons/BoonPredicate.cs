using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roag.Core.Tactics;

[JsonConverter(typeof(BoonPredicateConverter))]
public abstract record BoonPredicate;
public sealed record AllBoonPredicates(ImmutableArray<BoonPredicate> Children) : BoonPredicate;
public sealed record AnyBoonPredicates(ImmutableArray<BoonPredicate> Children) : BoonPredicate;
public sealed record NotBoonPredicate(BoonPredicate Child) : BoonPredicate;
public sealed record BoonCondition(BoonPredicateKind Op, string? Text = null, long? Number = null,
    int? Min = null, int? Max = null, int? Radius = null, int? Size = null,
    BoonEventField? Field = null, BoonNeighborhood? Neighborhood = null) : BoonPredicate;

/// <summary>Parse the authored condition tree into typed nodes; no dynamic values survive loading.</summary>
public sealed class BoonPredicateConverter : JsonConverter<BoonPredicate>
{
    private static readonly HashSet<string> LeafFields = ["op", "value", "min", "max", "radius", "size", "field", "neighborhood"];
    public override BoonPredicate Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var node = document.RootElement;
        if (node.ValueKind != JsonValueKind.Object) throw new JsonException("A boon predicate must be an object.");
        var properties = node.EnumerateObject().ToArray();
        if (properties.Select(p => p.Name).Distinct().Count() != properties.Length) throw new JsonException("Duplicate predicate field.");
        foreach (string group in new[] { "all", "any", "not_" })
        {
            if (!node.TryGetProperty(group, out var value)) continue;
            if (properties.Length != 1) throw new JsonException("Predicate groups cannot also contain leaf fields.");
            if (group == "not_") return new NotBoonPredicate(value.Deserialize<BoonPredicate>(options)
                ?? throw new JsonException("Missing negated predicate."));
            var children = value.Deserialize<ImmutableArray<BoonPredicate>>(options);
            if (children.IsDefaultOrEmpty || children.Any(c => c is null)) throw new JsonException("Empty predicate group.");
            return group == "all" ? new AllBoonPredicates(children) : new AnyBoonPredicates(children);
        }
        if (properties.Any(p => !LeafFields.Contains(p.Name))) throw new JsonException("Unknown predicate field.");
        if (!node.TryGetProperty("op", out var op)) throw new JsonException("Missing predicate operation.");
        string? text = null; long? number = null;
        if (node.TryGetProperty("value", out var scalar))
        {
            if (scalar.ValueKind == JsonValueKind.String) text = scalar.GetString();
            else if (scalar.ValueKind == JsonValueKind.Number && scalar.TryGetInt64(out long n)) number = n;
            else throw new JsonException("Predicate value must be text or an integer identity/face.");
        }
        T? Optional<T>(string name) where T : struct => node.TryGetProperty(name, out var value) ? value.Deserialize<T>(options) : null;
        return new BoonCondition(op.Deserialize<BoonPredicateKind>(options), text, number,
            Optional<int>("min"), Optional<int>("max"), Optional<int>("radius"), Optional<int>("size"),
            Optional<BoonEventField>("field"), Optional<BoonNeighborhood>("neighborhood"));
    }

    public override void Write(Utf8JsonWriter writer, BoonPredicate value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        switch (value)
        {
            case AllBoonPredicates all: Write("all", all.Children); break;
            case AnyBoonPredicates any: Write("any", any.Children); break;
            case NotBoonPredicate not: Write("not_", not.Child); break;
            case BoonCondition condition:
                Write("op", condition.Op);
                if (condition.Text != null) Write("value", condition.Text);
                else if (condition.Number != null) Write("value", condition.Number.Value);
                if (condition.Min != null) Write("min", condition.Min.Value);
                if (condition.Max != null) Write("max", condition.Max.Value);
                if (condition.Radius != null) Write("radius", condition.Radius.Value);
                if (condition.Size != null) Write("size", condition.Size.Value);
                if (condition.Field != null) Write("field", condition.Field.Value);
                if (condition.Neighborhood != null) Write("neighborhood", condition.Neighborhood.Value);
                break;
            default: throw new JsonException("Unknown native predicate node.");
        }
        writer.WriteEndObject();
        void Write<T>(string key, T child) { writer.WritePropertyName(key); JsonSerializer.Serialize(writer, child, options); }
    }
}
