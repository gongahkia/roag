using Vagabonds.Core.Content;
using Vagabonds.Core.World;
using Xunit;

namespace Vagabonds.Core.Tests.World;

public sealed class EcologyStressAuditTests
{
    [Fact]
    public void SevenActualDaysIncludeMigrationRepeatedHuntsAndRecoveredPopulationSaves()
    {
        var report = EcologyStressAudit.Run(ContentCatalog.Default, 42, WorldSession.TicksPerDay * 7);
        Assert.True(report.Passed); Assert.Equal(7 * WorldSession.TicksPerDay, report.Ticks);
        Assert.NotEqual(report.Origin, report.MigratedHome); Assert.True(report.Hunts >= 2); Assert.True(report.Births >= 2);
        Assert.Contains("walking", report.SavedPhases); Assert.Contains("pending-recovery", report.SavedPhases);
        Assert.Contains("recovered", report.SavedPhases); Assert.Equal("completed", report.SavedPhases[^1]);
        Assert.True(report.Checkpoints.Count > 100); Assert.All(report.Checkpoints, p => Assert.InRange(p.History, 0, WorldSession.WildlifeHistoryLimit));
    }
}
