namespace Roag.Core.Content;

/// <summary>A deliberate group of enemies, with fixed danger and readable counterplay.</summary>
public sealed class EncounterDefinition : ContentDefinition
{
    public static readonly string[] AllowedRoles = ["attacker", "support", "preparer", "attrition"];
    public static readonly string[] AllowedPurposes = ["ruin", "mine", "shrine", "fort"];
    public required string[] Members { get; set; }
    public string[] Roles { get; set; } = [];
    public string Tier { get; set; } = "patrol";
    public int Weight { get; set; } = 1;
    public string[] Purposes { get; set; } = [];
    public World.Biome[] Biomes { get; set; } = [];
    public int MinDepth { get; set; } = 1;
    public int MaxDepth { get; set; } = 3;
    public required string Tactics { get; set; }
    public required string Reward { get; set; }
    public bool Fits(string purpose, World.Biome biome, int depth) => Purposes?.Contains(purpose) == true
        && Biomes?.Contains(biome) == true && depth >= MinDepth && depth <= MaxDepth;
}

/// <summary>Victory grants. Equipment comes from a seeded thematic pool, never combat randomness.</summary>
public sealed class RewardDefinition : ContentDefinition
{
    public int Gold { get; set; }
    public int Food { get; set; }
    public int Experience { get; set; }
    public Dictionary<string, int> Supplies { get; set; } = [];
    public string[] EquipmentPool { get; set; } = [];
    public string? Signature { get; set; }
    public int EquipmentChancePercent { get; set; } = 100;
}
