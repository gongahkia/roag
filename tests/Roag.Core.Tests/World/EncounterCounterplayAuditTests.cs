using System.Text.Json;
using Roag.Core.Content;
using Roag.Core.World;
using Xunit;

namespace Roag.Core.Tests.World;

public sealed class EncounterCounterplayAuditTests
{
    [Theory]
    [InlineData(42)] [InlineData(1337)]
    public void MatchedExperimentsMeasureRealStunGuardAndTargetPriorityWithoutDemandingWins(int seed)
    {
        var report = EncounterCounterplayAudit.Run(ContentCatalog.Default, seed);
        Assert.True(report.Passed); Assert.NotEmpty(report.Limitations);
        foreach (var pair in report.Comparisons)
        {
            Assert.Equal(pair.Baseline.Before, pair.Counterplay.Before);
            foreach (var trial in new[] { pair.Baseline, pair.Counterplay })
            {
                Assert.Equal(trial.Commands, trial.Decisions.Length); Assert.InRange(trial.Actions, trial.Commands, 100);
                Assert.InRange(trial.After.Health, 0, trial.After.MaxHealth); Assert.InRange(trial.After.Mana, 0, trial.After.MaxMana);
                Assert.Equal(trial.After.Conscious > 0, trial.Survived);
                Assert.Equal(trial.Events.Select(e => e.Sequence).Distinct().Order(), trial.Events.Select(e => e.Sequence));
            }
        }
        Assert.True(report.Comparisons[0].Baseline.PreparedImpactDamage > 0);
        Assert.Equal(0, report.Comparisons[0].Counterplay.PreparedImpactDamage);
        Assert.True(report.Comparisons[0].Counterplay.ManaSpent > report.Comparisons[0].Baseline.ManaSpent);
        Assert.True(report.Comparisons[1].Counterplay.PreparedImpactDamage < report.Comparisons[1].Baseline.PreparedImpactDamage);
        Assert.Contains(report.Comparisons[2].Baseline.Outcome, new[] { "victory", "party-wipe", "bounded-observation" });
    }

    [Fact]
    public void RepeatingASeedProducesTheSameMeasuredContinuation()
    {
        Assert.Equal(JsonSerializer.Serialize(EncounterCounterplayAudit.Run(ContentCatalog.Default, 42)),
            JsonSerializer.Serialize(EncounterCounterplayAudit.Run(ContentCatalog.Default, 42)));
    }
}
