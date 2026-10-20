using System.Text.Json;
using Ithica.Core.Expeditions;

namespace Ithica.Core.Generation;

public sealed class LunarRules
{
    public static LunarRules Default { get; } = Load();
    public int Version { get; set; }
    public int NoiseOctaves { get; set; }
    public double NoiseScale { get; set; }
    public AreaBounds Surface { get; set; } = new();
    public CompartmentRules Compartments { get; set; } = new();
    public List<LunarProfile> Profiles { get; set; } = [];
    private static LunarRules Load()
    {
        using var stream = typeof(LunarRules).Assembly.GetManifestResourceStream("Ithica.LunarGeneration.json")!;
        var rules = JsonSerializer.Deserialize<LunarRules>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        static bool Bounds(AreaBounds b) => b.MinWidth >= 40 && b.MinHeight >= 28 && b.MaxWidth <= 128 && b.MaxHeight <= 128
            && b.MinWidth <= b.MaxWidth && b.MinHeight <= b.MaxHeight;
        if (rules.Version != 3 || rules.NoiseOctaves is < 1 or > 8 || !double.IsFinite(rules.NoiseScale) || rules.NoiseScale is < 2 or > 100
            || !Bounds(rules.Surface) || !Bounds(rules.Compartments) || rules.Compartments.MinPartition < 8 || rules.Compartments.MaxDepth is < 1 or > 6
            || rules.Compartments.DamageOctaves is < 1 or > 8 || !double.IsFinite(rules.Compartments.LoopChance) || rules.Compartments.LoopChance is < 0 or > 1 || rules.Profiles.Count == 0 || rules.Profiles.Select(p => p.Id).Distinct().Count() != rules.Profiles.Count
            || rules.Profiles.Any(p => string.IsNullOrWhiteSpace(p.Id) || string.IsNullOrWhiteSpace(p.Name) || !double.IsFinite(p.RockThreshold)
                || p.RockThreshold is < -.5 or > .5 || p.Craters is < 0 or > 20 || !double.IsFinite(p.Warp) || p.Warp is < 0 or > 10))
            throw new InvalidDataException("Invalid lunar generation authoring.");
        return rules;
    }
    public LunarProfile Profile(int seed) => Profiles[(int)(GenerationSeeds.Mix(unchecked((uint)seed), 301) % (uint)Profiles.Count)];
}
public class AreaBounds
{
    public int MinWidth { get; set; }
    public int MaxWidth { get; set; }
    public int MinHeight { get; set; }
    public int MaxHeight { get; set; }
}
public sealed class CompartmentRules : AreaBounds
{
    public int MinPartition { get; set; }
    public int DamageOctaves { get; set; }
    public int MaxDepth { get; set; }
    public double LoopChance { get; set; }
}
public sealed class LunarProfile
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double RockThreshold { get; set; }
    public int Craters { get; set; }
    public double Warp { get; set; }
}
