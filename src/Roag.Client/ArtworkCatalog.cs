using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roag.Client;

// Viewer data only. Campaign rules, source identities and art pixels stay outside this model.
internal sealed class ArtworkEntry
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Pack { get; set; } = "";
    public string Source { get; set; } = "";
    public int[] Rect { get; set; } = [];
    public string Confidence { get; set; } = "";
    public string Basis { get; set; } = "";
    public string Role { get; set; } = "";
    public string Adaptation { get; set; } = "";
    public List<string> Ideas { get; set; } = [];
    public Dictionary<string, List<string>> Usage { get; set; } = [];
    public string GenerationNotes { get; set; } = "";
    public string IthicaStatus { get; set; } = "";
    public string LunarRole { get; set; } = "";
    public string LunarLimits { get; set; } = "";
    public List<string> LunarIdeas { get; set; } = [];
    public Dictionary<string, List<string>> IthicaUsage { get; set; } = [];
    public ArtworkDescriptor Asset { get; set; } = new();
    private string? _search, _gameSearch;
    public string Search => _search ??= string.Join(" ", new[] { Id, Source, Pack, Asset.Name, Asset.Description }
        .Concat(Asset.Tags).Concat(Asset.Evidence.Observations.Select(o => o.Description))).ToLowerInvariant();
    public string GameSearch => _gameSearch ??= string.Join(" ", new[] { Name, LunarRole, LunarLimits, Role, Adaptation, GenerationNotes }
        .Concat(LunarIdeas).Concat(Ideas).Concat(Usage.Values.SelectMany(v => v))
        .Concat(IthicaUsage.Values.SelectMany(v => v))).ToLowerInvariant();
}

internal sealed class ArtworkDescriptor
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Type { get; set; } = "";
    [JsonPropertyName("classification_confidence")] public string Confidence { get; set; } = "";
    [JsonPropertyName("suggested_layers")] public List<string> Layers { get; set; } = [];
    public List<ArtworkSubject> Subjects { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public ArtworkAppearance Appearance { get; set; } = new();
    public ArtworkComposition Composition { get; set; } = new();
    public JsonElement Relations { get; set; }
    public ArtworkEvidence Evidence { get; set; } = new();
}

internal sealed class ArtworkSubject
{
    public string Term { get; set; } = "";
    public string Confidence { get; set; } = "";
    public string Basis { get; set; } = "";
    public string Evidence { get; set; } = "";
}
internal sealed class ArtworkAppearance
{
    public int Width { get; set; }
    public int Height { get; set; }
    [JsonPropertyName("foreground_pixels")] public int ForegroundPixels { get; set; }
    [JsonPropertyName("primary_color")] public string PrimaryColor { get; set; } = "";
    [JsonPropertyName("color_coverage")] public Dictionary<string, double> ColorCoverage { get; set; } = [];
    public List<ArtworkColor> Palette { get; set; } = [];
}
internal sealed class ArtworkColor
{
    public string Hex { get; set; } = "";
    public int Alpha { get; set; }
    public string Name { get; set; } = "";
    public int Pixels { get; set; }
    public double Coverage { get; set; }
}
internal sealed class ArtworkComposition
{
    public string Kind { get; set; } = "";
    public string Animation { get; set; } = "";
    public string Directions { get; set; } = "";
    public string Sockets { get; set; } = "";
    public string Collision { get; set; } = "";
}
internal sealed class ArtworkEvidence
{
    [JsonPropertyName("review_status")] public string ReviewStatus { get; set; } = "";
    public List<ArtworkObservation> Observations { get; set; } = [];
    public List<string> Uncertainties { get; set; } = [];
    public List<string> References { get; set; } = [];
    [JsonPropertyName("inspection_refs")] public List<string> InspectionRefs { get; set; } = [];
}
internal sealed class ArtworkObservation
{
    public string Scope { get; set; } = "";
    public string Description { get; set; } = "";
}

// The two native viewers share the same neutral query rules. Free words are ANDed.
// Explicit game:true includes game notes; it does not change artwork classification.
internal sealed class ArtworkQuery
{
    public static readonly string[] Colors = ["black", "gray", "white", "brown", "red", "orange", "yellow", "green", "cyan", "blue", "purple", "pink"];
    public static readonly string[] Sorts = ["id", "name", "type", "color", "width", "height", "confidence"];
    public static readonly string[] Confidences = ["low", "medium", "high"];
    public static readonly string[] Keys = ["type", "pack", "color", "primary", "hex", "layer", "composition", "subject", "confidence", "source", "visual", "coverage", "sort", "desc", "game"];
    private readonly Dictionary<string, List<string>> _filters = [];
    private readonly List<string> _words = [];
    public string Error { get; private set; } = "";
    public string? Sort { get; }
    public bool Descending { get; }
    public bool IncludeGame { get; }
    private readonly double _coverage = .05;
    private readonly int _confidence;

    public ArtworkQuery(string text, bool includeGame = false)
    {
        foreach (string token in text.ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            int colon = token.IndexOf(':');
            if (colon <= 0 || !Keys.Contains(token[..colon])) { _words.Add(token); continue; }
            string key = token[..colon], value = token[(colon + 1)..];
            if (value.Length == 0) { Error = "Add a value after " + key + ":"; continue; }
            if (!_filters.TryGetValue(key, out var values)) _filters[key] = values = [];
            var parts = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) Error = "Add a value after " + key + ":";
            values.AddRange(parts);
        }
        string? One(string key) => _filters.GetValueOrDefault(key)?.LastOrDefault();
        if (One("confidence") is { } minimum && !Confidences.Contains(minimum)) Error = "Subject confidence: low, medium or high.";
        _confidence = Array.IndexOf(Confidences, One("confidence") ?? "low");
        foreach (string key in new[] { "source", "visual" })
            if (One(key) is { } value && !Confidences.Contains(value)) Error = key + " confidence: low, medium or high.";
        if (One("coverage") is { } coverage &&
            (!double.TryParse(coverage, NumberStyles.Float, CultureInfo.InvariantCulture, out _coverage) || !double.IsFinite(_coverage) || _coverage < 0 || _coverage > 1))
            Error = "Color coverage must be a fraction from 0 to 1.";
        foreach (string key in new[] { "color", "primary" })
            if (_filters.GetValueOrDefault(key)?.Any(c => !Colors.Contains(c)) == true) Error = "Unknown color family.";
        foreach (string value in _filters.GetValueOrDefault("hex") ?? [])
            if (value.Length != 7 || value[0] != '#' || !value[1..].All(Uri.IsHexDigit)) Error = "Exact RGB uses #rrggbb.";
        Sort = One("sort");
        if (Sort is not null && !Sorts.Contains(Sort)) Error = "Sort: id, name, type, color, width, height or confidence.";
        foreach (string key in new[] { "game", "desc" })
            if (One(key) is { } value && value is not ("true" or "false")) Error = key + " uses true or false.";
        IncludeGame = One("game") is { } game ? game == "true" : includeGame;
        Descending = One("desc") == "true";
    }

    public bool Matches(ArtworkEntry entry)
    {
        if (Error.Length > 0) return false;
        var a = entry.Asset;
        bool Any(string key, Func<string, bool> predicate) => !_filters.TryGetValue(key, out var values) || values.Any(predicate);
        bool All(string key, Func<string, bool> predicate) => !_filters.TryGetValue(key, out var values) || values.All(predicate);
        if (!Any("pack", value => entry.Pack == value) || !Any("type", value => a.Type == value)
            || !Any("layer", value => a.Layers.Contains(value)) || !Any("composition", value => a.Composition.Kind == value)
            || !Any("primary", value => a.Appearance.PrimaryColor == value)
            || !Any("source", value => entry.Confidence == value) || !Any("visual", value => a.Confidence == value)
            || !All("color", value => a.Appearance.ColorCoverage.TryGetValue(value, out double fraction) && fraction >= _coverage)
            || !All("hex", value => a.Appearance.Palette.Any(p => p.Hex == value))
            || !All("subject", value => a.Subjects.Any(s => s.Term == value && Array.IndexOf(Confidences, s.Confidence) >= _confidence))) return false;
        string text = entry.Search + (IncludeGame ? " " + entry.GameSearch : "");
        return _words.All(text.Contains);
    }

    public static List<ArtworkEntry> Order(IEnumerable<ArtworkEntry> entries, string sort, bool descending = false)
    {
        IOrderedEnumerable<ArtworkEntry> ordered = sort switch
        {
            "name" => entries.OrderBy(e => e.Asset.Name.ToLowerInvariant(), StringComparer.Ordinal),
            "type" => entries.OrderBy(e => e.Asset.Type, StringComparer.Ordinal),
            "color" => entries.OrderBy(e => Array.IndexOf(Colors, e.Asset.Appearance.PrimaryColor)),
            "width" => entries.OrderBy(e => e.Asset.Appearance.Width),
            "height" => entries.OrderBy(e => e.Asset.Appearance.Height),
            "confidence" => entries.OrderBy(e => Array.IndexOf(Confidences, e.Asset.Confidence)),
            _ => entries.OrderBy(e => e.Id, StringComparer.Ordinal)
        };
        var result = ordered.ThenBy(e => e.Id, StringComparer.Ordinal);
        return (descending ? result.Reverse() : result).ToList();
    }

    // Emitted only by the existing graphical smoke path. Python checks the actual
    // native query results against the canonical catalog, including complete order.
    public static object[] Diagnostics(IReadOnlyList<ArtworkEntry> entries)
    {
        string[] samples = ["", "type:actor color:green sort:color", "subject:ghost confidence:high",
            "type:fragment layer:object composition:fragment_candidate sort:name desc:true",
            "type:input_prompt color:white coverage:0.3", "hex:#feffff", "primary:blue sort:width",
            "subject:goblin_like confidence:high", "source:high", "interference", "interference game:true", "coverage:2",
            "color:,,,", "confidence:unknown", "hex:green"];
        return samples.Select(text =>
        {
            var query = new ArtworkQuery(text);
            return (object)new { query = text, error = query.Error,
                ids = Order(entries.Where(query.Matches), query.Sort ?? "id", query.Descending).Select(e => e.Id).ToArray() };
        }).ToArray();
    }
}
