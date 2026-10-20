using System.Text.Json;

namespace Ithica.Core.Expeditions;

public sealed class GenerationRules
{
    public static GenerationRules Default { get; } = Load();
    public int SchemaVersion { get; set; }
    public int GeneratorVersion { get; set; }
    public ForestRules Forest { get; set; } = new();
    public Dictionary<string, MaterialRule> Materials { get; set; } = [];
    public AbbeyRules Abbey { get; set; } = new();
    public HazardRules Hazards { get; set; } = new();
    public HearthRules Hearth { get; set; } = new();
    public IEnumerable<string> SpriteAliases => Materials.Values.SelectMany(m => m.Sprites)
        .Concat(Forest.Props).Concat(Hearth.Furniture).Concat(Abbey.Variants.SelectMany(v => v.FloorSprites.Concat(v.Props).Append(v.WallSprite)))
        .Concat([Hazards.FireSprite, Hazards.SporesSprite, Hazards.CorruptionSprite]).Distinct();
    private static GenerationRules Load()
    {
        using var stream = typeof(GenerationRules).Assembly.GetManifestResourceStream("Ithica.Generation.json")!;
        var rules = JsonSerializer.Deserialize<GenerationRules>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        if (rules.SchemaVersion != 1 || rules.Abbey.ChunkSize != 7 || rules.Abbey.Variants.Length != 3)
            throw new InvalidDataException("Unsupported generation rules.");
        return rules;
    }
}
public sealed class ForestRules
{
    public int InitialTreesPercent { get; set; }
    public int Passes { get; set; }
    public int Birth { get; set; }
    public int Survive { get; set; }
    public int PondPercent { get; set; }
    public int PondPasses { get; set; }
    public string[] Props { get; set; } = [];
}
public sealed class MaterialRule
{
    public string[] Sprites { get; set; } = [];
    public byte Fuel { get; set; }
}
public sealed class AbbeyRules
{
    public int Columns { get; set; }
    public int Rows { get; set; }
    public int ChunkSize { get; set; }
    public int MaxAttempts { get; set; }
    public RoomVariant[] Variants { get; set; } = [];
}
public sealed class RoomVariant
{
    public string Id { get; set; } = "";
    public int Weight { get; set; }
    public string Shape { get; set; } = "";
    public string[] FloorSprites { get; set; } = [];
    public string WallSprite { get; set; } = "";
    public string[] Props { get; set; } = [];
}
public sealed class HazardRules
{
    public int StepsPerTick { get; set; }
    public string FireSprite { get; set; } = "";
    public string SporesSprite { get; set; } = "";
    public string CorruptionSprite { get; set; } = "";
    public int SafeRadius { get; set; }
    public int TorchRadius { get; set; }
    public byte SuppressionTicks { get; set; }
}
public sealed class HearthRules { public string[] Furniture { get; set; } = []; }
public sealed class GenerationReport
{
    public int Version { get; set; } = 2;
    public string Algorithm { get; set; } = "authored";
    public int CellularPasses { get; set; }
    public int WfcAttempts { get; set; }
    public int Observations { get; set; }
    public int Propagations { get; set; }
    public bool UsedFallback { get; set; }
    public string FallbackReason { get; set; } = "";
    public List<RoomPlacement> Rooms { get; set; } = [];
}
public sealed record RoomPlacement(int X, int Y, int Sockets, string Variant);
public sealed record GenerationOptions(int? WfcAttempts = null);
public sealed record QuestValidation(bool Valid, string[] Errors);
