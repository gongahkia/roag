using System.Collections.Immutable;
using Roag.Core.Tactics;

namespace Roag.Core.Profiles;

public sealed record AchievementDefinition(string Id, string Name, string Hint, string? Reward = null);

/// <summary>Stable original achievement identities, independent of active-world content compatibility.</summary>
public static class AchievementCatalog
{
    public const string Content = "roag-achievements/1";
    public const string Prefix = "core:achievement/";
    public static ImmutableArray<AchievementDefinition> All { get; } =
    [
        new(Prefix + "first_guardian", "First Guardian", "Defeat a Thorn Warden.", "core:boon/guardians_vow"),
        new(Prefix + "uncovered_truth", "Uncovered Truth", "Reveal a hidden cache through ordinary actions.", "core:boon/prospectors_heart"),
        new(Prefix + "chain_reaction", "Chain Reaction", "Activate three different boons on one member in one causal action chain.", "core:boon/circuit_chorus"),
        new(Prefix + "regents_fall", "Regent's Fall", "Defeat the Hollow Regent and survive.", "core:boon/crownfire"),
        new(Prefix + "first_blood", "First Blood", "Defeat your first enemy."),
        new(Prefix + "pathfinder", "Pathfinder", "Reach another floor."),
        new(Prefix + "treasure_hunter", "Treasure Hunter", "Claim a chest reward."),
        new(Prefix + "coin_purse", "Coin Purse", "Carry at least 50 gold."),
        new(Prefix + "first_level", "Growing Stronger", "Reach level 2."),
        new(Prefix + "veteran", "Veteran", "Reach level 5."),
        new(Prefix + "boon_bearer", "Boon Bearer", "Claim a boon."),
        new(Prefix + "stonebreaker", "Stonebreaker", "Break a barricade."),
        new(Prefix + "ore_seeker", "Ore Seeker", "Break an ore deposit."),
        new(Prefix + "gate_opener", "Gate Opener", "Open a timed gate."),
        new(Prefix + "key_bearer", "Key Bearer", "Find a vault key."),
        new(Prefix + "warded", "Warded", "Collect a ward flask."),
        new(Prefix + "torchbearer", "Torchbearer", "Light a brazier."),
        new(Prefix + "long_heal", "Second Wind", "Use a healing supply or eat to restore hunger."),
        new(Prefix + "pressure_survivor", "Under Pressure", "Face an arriving pursuer."),
        new(Prefix + "deeper_road", "Deeper Road", "Enter the second stage."),
        new(Prefix + "sanctum_reached", "Sanctum Reached", "Enter the Final Sanctum."),
        new(Prefix + "regent_awakened", "Regent Awakened", "Force the Hollow Regent into phase two.")
    ];
    private static readonly ImmutableDictionary<string, AchievementDefinition> Definitions = All.ToImmutableDictionary(a => a.Id, StringComparer.Ordinal);
    public static bool Contains(string id) => id is not null && Definitions.ContainsKey(id);
    public static AchievementDefinition Get(string id) => Definitions.TryGetValue(id, out var value) ? value : throw new InvalidDataException("Unknown achievement identity.");
    public static bool IsReward(string id) => All.Any(a => a.Reward == id);
    public static string[] Rewards(IEnumerable<string> earned)
    {
        var ids = earned.ToHashSet(StringComparer.Ordinal);
        return All.Where(a => a.Reward != null && ids.Contains(a.Id)).Select(a => a.Reward!).ToArray();
    }
    public static string[] Pool(IEnumerable<string> normalPool, IEnumerable<string> earned) => normalPool.Concat(Rewards(earned)).Distinct(StringComparer.Ordinal).ToArray();
}
