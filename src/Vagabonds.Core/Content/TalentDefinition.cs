namespace Vagabonds.Core.Content;

/// <summary>A generated combat disposition or a deliberately chosen class development.</summary>
public sealed class TalentDefinition : ContentDefinition
{
    public static readonly string[] Effects = ["initiative", "protect", "healing", "calm", "mana-discount", "wounded-damage", "marked-damage", "stunned-damage", "all-target-damage", "burn-damage", "cooperative-damage"];
    public required string Acquisition { get; set; }
    public string? Class { get; set; }
    public int Level { get; set; } = 1;
    public Dictionary<string, int> Bonuses { get; set; } = [];
    public static int Maximum(string effect) => effect == "calm" ? 20 : effect == "mana-discount" ? 2 : 8;
}
