namespace Vagabonds.Core.Content;

/// <summary>A deliberate group of enemies, with fixed danger and readable counterplay.</summary>
public sealed class EncounterDefinition : ContentDefinition
{
    public required string[] Members { get; set; }
    public required string Tactics { get; set; }
    public required string Reward { get; set; }
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
