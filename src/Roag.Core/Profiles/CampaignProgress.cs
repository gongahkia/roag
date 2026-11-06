using Roag.Core.Tactics;

namespace Roag.Core.Profiles;

public sealed class AchievementChain
{
    public long Chain { get; set; }
    public long Owner { get; set; }
    public HashSet<string> Boons { get; set; } = [];
}

/// <summary>Only explicitly enrolled new native campaigns are eligible. Null preserves existing fixtures/saves.</summary>
public sealed class CampaignProgress
{
    public string Profile { get; set; } = "";
    public long Run { get; set; }
    public HashSet<string> Earned { get; set; } = [];
    public HashSet<string> Pending { get; set; } = [];
    public HashSet<string> StartingAchievements { get; set; } = [];
    public List<AchievementChain> Chains { get; set; } = [];

    public void Validate(TacticalWorldState state, long nextActor)
    {
        if (!Guid.TryParseExact(Profile, "N", out _) || Run <= 0 || Run >= long.MaxValue || Earned == null || Pending == null || StartingAchievements == null
            || Earned.Any(id => !AchievementCatalog.Contains(id)) || Pending.Any(id => !Earned.Contains(id))
            || StartingAchievements.Any(id => !AchievementCatalog.Contains(id)) || Chains == null || Chains.Count > 10000
            || Chains.Any(c => c == null || c.Chain <= 0 || c.Chain >= state.NextAction || c.Owner <= 0 || c.Owner >= nextActor
                || c.Boons == null || c.Boons.Count is < 1 or > 2 || c.Boons.Any(id => !BoonCatalog.Default.Contains(id)))
            || Chains.Select(c => (c.Chain, c.Owner)).Distinct().Count() != Chains.Count
            || Earned.Contains(AchievementCatalog.Prefix + "chain_reaction") && Chains.Count != 0
            || state.Chests.AvailableBoons.Where(AchievementCatalog.IsReward).Except(AchievementCatalog.Rewards(StartingAchievements)).Any())
            throw new InvalidDataException("Invalid native campaign achievement progress or frozen unlock pool.");
    }
}
