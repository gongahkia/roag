using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roag.Core.World;

/// <summary>Keep constant-time live receipt lookup and deterministic saved arrays after reclamation.</summary>
public sealed class RewardReceiptSetConverter : JsonConverter<HashSet<long>>
{
    public override HashSet<long> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected reward receipt identities.");
        var ids = new HashSet<long>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) return ids;
            if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt64(out long id)) throw new JsonException("Invalid reward receipt identity.");
            ids.Add(id);
        }
        throw new JsonException("Incomplete reward receipt identities.");
    }
    public override void Write(Utf8JsonWriter writer, HashSet<long> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (long id in value.Order()) writer.WriteNumberValue(id);
        writer.WriteEndArray();
    }
}
