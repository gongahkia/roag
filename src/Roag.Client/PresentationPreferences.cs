using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roag.Client;

internal enum UiFontFace { Pixel, Sans }
internal enum UiTextSize { Small, Normal, Large }

/// <summary>Presentation-only preferences, deliberately outside the continuing run.</summary>
internal sealed record PresentationPreferences(int Format = 1, int Master = 100, int Music = 80, int Effects = 90,
    bool Shake = true, bool Flash = true, bool CombatEffects = true, bool DamageNumbers = true,
    UiFontFace Font = UiFontFace.Pixel, UiTextSize TextSize = UiTextSize.Normal, bool HighContrast = false,
    bool ReducedMotion = false)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }, WriteIndented = true
    };

    public void Validate()
    {
        if (Format != 1 || Master is < 0 or > 100 || Music is < 0 or > 100 || Effects is < 0 or > 100
            || !Enum.IsDefined(Font) || !Enum.IsDefined(TextSize)) throw new InvalidDataException("Invalid presentation preferences.");
    }

    [JsonIgnore] public float EffectsVolume => Master * Effects / 10_000f;
    [JsonIgnore] public float MusicVolume => Master * Music / 10_000f;
    [JsonIgnore] public bool Animate => !ReducedMotion;

    public static PresentationPreferences Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return new();
            var value = JsonSerializer.Deserialize<PresentationPreferences>(File.ReadAllText(path), Json)
                ?? throw new InvalidDataException("Empty presentation preferences.");
            value.Validate(); return value;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or InvalidDataException) { return new(); }
    }

    public void Save(string path)
    {
        Validate(); path = Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            string json = JsonSerializer.Serialize(this, Json); File.WriteAllText(temporary, json); File.Move(temporary, path, true);
            if (File.ReadAllText(path) != json) throw new IOException("Presentation settings verification failed.");
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
