using System.Text.Json;
using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class ExpeditionPacingAuditTests
{
    [Fact]
    public void BoundedFreshRunIsDeterministicAndRecordsActualRecruitmentAndClock()
    {
        var first = ExpeditionPacingAudit.Run(ContentCatalog.Default, [42], 64);
        var second = ExpeditionPacingAudit.Run(ContentCatalog.Default, [42], 64);
        Assert.Equal(JsonSerializer.Serialize(first), JsonSerializer.Serialize(second));
        Assert.True(first.Passed);
        var world = Assert.Single(first.Worlds);
        Assert.Equal(ContentCatalog.Default.Hash, first.ContentHash);
        Assert.InRange(world.Final.Steps, 1, 64);
        Assert.True(world.Final.Tick >= world.Final.Steps * 7 / 10);
        Assert.Contains(world.Legs, l => l.Goal == "recruit" && l.Outcome == "recruited" && l.After.Party.Length > l.Before.Party.Length);
        Assert.All(world.Legs, l =>
        {
            Assert.True(l.After.Tick >= l.Before.Tick);
            Assert.Equal(l.Steps, l.After.Steps - l.Before.Steps);
            Assert.True(l.RemainingDistance >= 0);
        });
        Assert.All(world.Final.Party, p => Assert.True(p.Hunger > 0 && p.Fatigue > 0));
    }

    [Fact]
    public void InvariantSuccessDoesNotPretendAnUnreachedFightOrDungeonWasCovered()
    {
        var report = ExpeditionPacingAudit.Run(ContentCatalog.Default, [-42], 1);
        Assert.True(report.Passed);
        var world = Assert.Single(report.Worlds);
        Assert.InRange(world.Final.Steps, 0, 1);
        Assert.Empty(world.Fights);
        Assert.Contains(world.CoverageGaps, g => g.Contains("No encounter reached", StringComparison.Ordinal));
        Assert.Contains(world.CoverageGaps, g => g.Contains("No dungeon entry", StringComparison.Ordinal));
        Assert.Contains(report.Findings, f => f.StartsWith("Seed -42:", StringComparison.Ordinal));
        Assert.Contains("not human play", report.Scope, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BoundsAndSeedIdentityAreExplicit()
    {
        Assert.Throws<ArgumentException>(() => ExpeditionPacingAudit.Run(ContentCatalog.Default, []));
        Assert.Throws<ArgumentException>(() => ExpeditionPacingAudit.Run(ContentCatalog.Default, [42, 42]));
        Assert.Throws<ArgumentException>(() => ExpeditionPacingAudit.Run(ContentCatalog.Default, Enumerable.Range(0, 17).ToArray()));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExpeditionPacingAudit.Run(ContentCatalog.Default, [42], 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ExpeditionPacingAudit.Run(ContentCatalog.Default, [42], 5001));
    }
}
